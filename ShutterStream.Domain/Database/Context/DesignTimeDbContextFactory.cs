using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShutterStream.Domain.Database.Context
{
    /// <summary>
    /// Only used by the EF tooling when adding migrations. Creating a migration doesn't connect
    /// to the database, so this avoids booting the API (and its debug test container) to do it.
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql("Host=localhost;Database=shutterstream_design;Username=design;Password=design")
                .Options;

            return new AppDbContext(options);
        }
    }
}
