using Hangfire;
using Hangfire.Dashboard.BasicAuthorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using ShutterStream.Core;
using ShutterStream.Domain.Database.Context;
using ShutterStream.Domain.Enums;
using ShutterStream.Domain.Helpers;
using ShutterStream.Domain.Interfaces.Api.Admin;
using ShutterStream.Domain.Interfaces.Api.Albums;
using ShutterStream.Domain.Interfaces.Api.Auth;
using ShutterStream.Domain.Interfaces.Api.Captions;
using ShutterStream.Domain.Interfaces.Api.Library;
using ShutterStream.Domain.Interfaces.Api.Photos;
using ShutterStream.Domain.Interfaces.Helpers;
using ShutterStream.Domain.Services.Admin;
using ShutterStream.Domain.Services.Albums;
using ShutterStream.Domain.Services.Auth;
using ShutterStream.Domain.Services.Captions;
using ShutterStream.Domain.Services.Library;
using ShutterStream.Domain.Services.Photos;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

#if DEBUG
using Hangfire.MemoryStorage;
using Testcontainers.PostgreSql;
#else
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Identity;
using ShutterStream.Domain.Database.Models;
#endif

Log.Logger = new LoggerConfiguration()
    .WriteTo.Async(x => x.File("/app/Logs/log.log", retainedFileCountLimit: 7, rollingInterval: RollingInterval.Day))
    .WriteTo.Console()
    .Enrich.WithProperty("Application", "ShutterStream-Api" + (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" ? "-Test" : ""))
    .WriteTo.Seq("http://192.168.1.20:5341")
    .CreateLogger();

Log.Information("Logger Setup");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();

// The photo library is mounted read only, generated thumbnails go in the cache folder.
// Made absolute because PhysicalFile refuses relative paths, which the dev launch profile uses
var photoLibraryPath = Path.GetFullPath(Environment.GetEnvironmentVariable("PhotoLibraryPath") ?? "/photos");
var photoCachePath = Path.GetFullPath(Environment.GetEnvironmentVariable("PhotoCachePath") ?? "/app/PhotoCache");

// Add in our own services
builder.Services.AddSingleton<IEnvironmentalSettingHelper, EnvironmentalSettingHelper>();
builder.Services.AddSingleton<IPhotoLibraryHelper>(new PhotoLibraryHelper(photoLibraryPath, photoCachePath));
builder.Services.AddSingleton<IImageProcessingHelper, ImageProcessingHelper>();
builder.Services.AddSingleton<ILibraryScanStatusHelper, LibraryScanStatusHelper>();
builder.Services.AddSingleton<ICaptionJobStatusHelper, CaptionJobStatusHelper>();
builder.Services.AddHttpClient<IAiCaptionHelper, GeminiCaptionHelper>();

// Add in the auth
builder.Services.AddAuthorization();

// Expected failures come back from the services as Results, so only unhandled exceptions reach
// the exception handler. ProblemDetails turns those into a generic 500 with no internals exposed
builder.Services.AddProblemDetails();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAlbumsService, AlbumsService>();
builder.Services.AddScoped<IPhotosService, PhotosService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ILibraryScanService, LibraryScanService>();
builder.Services.AddScoped<IPhotoCaptionService, PhotoCaptionService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Environment.GetEnvironmentVariable("JwtValidIssuer"),
            ValidAudience = Environment.GetEnvironmentVariable("JwtValidAudience"),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JwtKey")!))
        };
    });

// Every request arrives through the Next.js proxy, so there is no trustworthy client IP to limit by.
// A global limit on login attempts is fine with a single admin and stops password guessing
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(RateLimitPolicies.Login, limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(5);
        limiter.QueueLimit = 0;
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClients", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "https://shutterstream.bregan.me"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

#if DEBUG
GlobalConfiguration.Configuration.UseMemoryStorage();

var postgresContainer = new PostgreSqlBuilder("postgres:16")
    .WithDatabase("shutterstreamcontainer")
    .WithUsername("testuser")
    .WithPassword("testpass")
    .WithPortBinding(5432, true)
    .Build();

await postgresContainer.StartAsync();

var connectionString = postgresContainer.GetConnectionString();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseLazyLoadingProxies()
           .UseNpgsql(connectionString));

builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseMemoryStorage()
        );

#else
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseLazyLoadingProxies()
           .UseNpgsql(Environment.GetEnvironmentVariable("ShutterStreamConnString")));

GlobalConfiguration.Configuration.UsePostgreSqlStorage(c => c.UseNpgsqlConnection(Environment.GetEnvironmentVariable("ShutterStreamConnString")));

builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(Environment.GetEnvironmentVariable("ShutterStreamConnString")))
        );
#endif

// hangfire
builder.Services.AddHangfireServer(options => options.SchedulePollingInterval = TimeSpan.FromSeconds(10));

var app = builder.Build();

app.UseCors("WebClients");
app.UseRateLimiter();

#if DEBUG
// Seed the database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetService<AppDbContext>();

    if (dbContext == null)
    {
        throw new Exception("DbContext is null");
    }

    // protection to only run when the connection string is to the test container
    var dbConnectionString = dbContext.Database.GetConnectionString();
    if (!string.IsNullOrEmpty(dbConnectionString) &&
        (dbConnectionString.Contains("127.0.0.1") ||
        dbConnectionString.Contains("shutterstreamcontainer")))
    {
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();

        await DatabaseSeedHelper.SeedDatabase(dbContext, photoLibraryPath);
    }
}

// The debug database is recreated on every start, so scan straight away to populate it
BackgroundJob.Enqueue<ILibraryScanService>(service => service.ScanLibrary());
#else
// Apply any pending migrations and create the admin login on first start.
// The environmental settings are added to the database by hand
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    if (!await dbContext.Users.AnyAsync())
    {
        var adminUsername = Environment.GetEnvironmentVariable("AdminUsername");
        var adminPassword = Environment.GetEnvironmentVariable("AdminPassword");

        if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException("No admin user exists. Set the AdminUsername and AdminPassword environment variables to create one");
        }

        // Usernames are matched lowercased and trimmed at login, so store them the same way
        dbContext.Users.Add(new User
        {
            Username = adminUsername.ToLower().Trim(),
            PasswordHash = new PasswordHasher<User>().HashPassword(new User(), adminPassword)
        });

        await dbContext.SaveChangesAsync();
        Log.Information($"Created admin user {adminUsername}");
    }
}
#endif

var environmentalSettingHelper = app.Services.GetService<IEnvironmentalSettingHelper>()!;
await environmentalSettingHelper.LoadEnvironmentalSettings();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

#if DEBUG
// Not exposed in production, there's no reason to publish a map of every endpoint
app.UseSwagger();
app.UseSwaggerUI();
#endif

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var auth = new[] { new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
{
    RequireSsl = false,
    SslRedirect = false,
    LoginCaseSensitive = true,
    Users = new []
    {
        new BasicAuthAuthorizationUser
        {
            Login = environmentalSettingHelper.GetEnvironmentalSettingValue(EnvironmentalSettingEnum.HangfireUsername),
            PasswordClear = environmentalSettingHelper.GetEnvironmentalSettingValue(EnvironmentalSettingEnum.HangfirePassword)
        }
    }
})};

app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = auth
}, JobStorage.Current);

#if !DEBUG
HangfireJobSetup.SetupRecurringJobs();
#endif

app.Run();
