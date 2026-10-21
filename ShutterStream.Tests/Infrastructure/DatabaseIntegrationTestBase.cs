using Microsoft.EntityFrameworkCore;
using ShutterStream.Domain.Database.Context;

namespace ShutterStream.Tests.Infrastructure
{
    /// <summary>
    /// Base class for integration tests that need a real database
    /// Automatically creates and tears down the database for each test
    /// </summary>
    public abstract class DatabaseIntegrationTestBase
    {
        protected AppDbContext DbContext { get; private set; } = null!;

        [SetUp]
        public async Task SetUp()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(TestContainerSetup.ConnectionString)
                .UseLazyLoadingProxies()
                .EnableSensitiveDataLogging()
                .Options;

            DbContext = new AppDbContext(options);

            // Ensure database is created fresh for each test
            await DbContext.Database.EnsureDeletedAsync();

            // Use MigrateAsync instead of EnsureCreatedAsync to properly configure lazy loading proxies
            await DbContext.Database.MigrateAsync();

            // Custom setup for derived classes
            await CustomSetUp();
        }

        [TearDown]
        public async Task TearDown()
        {
            // Custom teardown for derived classes
            await CustomTearDown();

            await DbContext.DisposeAsync();
        }

        /// <summary>
        /// Override this method to add custom setup logic
        /// </summary>
        protected virtual Task CustomSetUp()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Override this method to add custom teardown logic
        /// </summary>
        protected virtual Task CustomTearDown()
        {
            return Task.CompletedTask;
        }
    }
}
