using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ShutterStream.Domain.Database.Context;
using ShutterStream.Domain.Database.Models;
using ShutterStream.Domain.Enums;

namespace ShutterStream.Domain.Helpers
{
    /// <summary>
    /// Release only. Creates the admin login and required settings on first start from environment
    /// variables, so a fresh deployment doesn't need any manual database inserts.
    /// Nothing is overwritten once it exists.
    /// </summary>
    public class DatabaseBootstrapHelper
    {
        private static readonly PasswordHasher<User> _passwordHasher = new();

        public const string DefaultGeminiModel = "gemini-3.8-flash";

        public static async Task BootstrapDatabase(AppDbContext context)
        {
            var adminUsername = GetEnvironmentVariable("AdminUsername");
            var adminPassword = GetEnvironmentVariable("AdminPassword");

            if (!await context.Users.AnyAsync())
            {
                if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminPassword))
                {
                    throw new InvalidOperationException("No admin user exists. Set the AdminUsername and AdminPassword environment variables to create one");
                }

                context.Users.Add(new User
                {
                    Username = adminUsername.ToLower().Trim(),
                    PasswordHash = _passwordHasher.HashPassword(new User(), adminPassword.Trim())
                });

                Log.Information($"Created admin user {adminUsername}");
            }

            // The Hangfire dashboard login defaults to the admin login unless set separately
            await EnsureSetting(context, EnvironmentalSettingEnum.HangfireUsername, GetEnvironmentVariable("HangfireUsername") ?? adminUsername);
            await EnsureSetting(context, EnvironmentalSettingEnum.HangfirePassword, GetEnvironmentVariable("HangfirePassword") ?? adminPassword);

            // AI captions are optional, an empty key just switches them off
            await EnsureSetting(context, EnvironmentalSettingEnum.GeminiApiKey, GetEnvironmentVariable("GEMINI_API_KEY") ?? "", allowEmpty: true);
            await EnsureSetting(context, EnvironmentalSettingEnum.GeminiModel, GetEnvironmentVariable("GeminiModel") ?? DefaultGeminiModel);

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Treats a blank variable as unset. Container UIs like Unraid pass optional fields left
        /// empty as empty variables, which would otherwise skip the fallbacks above.
        /// </summary>
        private static string? GetEnvironmentVariable(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static async Task EnsureSetting(AppDbContext context, EnvironmentalSettingEnum key, string? value, bool allowEmpty = false)
        {
            if (await context.EnvironmentalSettings.AnyAsync(s => s.Key == key.ToString()))
            {
                return;
            }

            if (value == null || (!allowEmpty && string.IsNullOrWhiteSpace(value)))
            {
                throw new InvalidOperationException($"Environmental setting {key} is missing and no environment variable was provided for it");
            }

            await context.EnvironmentalSettings.AddAsync(new EnvironmentalSetting
            {
                Key = key.ToString(),
                Value = value
            });
        }
    }
}
