using Microsoft.EntityFrameworkCore;
using ShutterStream.Infrastructure.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShutterStream.Infrastructure.Database.Context
{
    public class DatabaseContext : DbContext
    {
        private static readonly string _connectionString = Environment.GetEnvironmentVariable("ShutterStreamConnString")!;

        public DbSet<Albums> Albums { get; set; }
        public DbSet<Config> Config { get; set; }
        public DbSet<Images> Images { get; set; }
        public DbSet<Sessions> Sessions { get; set; }
        public DbSet<Users> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql(_connectionString);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Seed in the data
            modelBuilder.Entity<Config>().HasData(new Config
            {
                Id = 1,
                AzureBlobConnectionString = "",
                ProjectMonitorApiKey = ""
            });
        }

    }
}
