using Microsoft.AspNetCore.Identity;
using ShutterStream.Domain.Database.Context;
using ShutterStream.Domain.Database.Models;
using ShutterStream.Domain.Enums;
using SkiaSharp;

namespace ShutterStream.Domain.Helpers
{
    /// <summary>
    /// Debug only seed data. Creates the admin login and, if the library folder doesn't exist yet,
    /// a small sample library of generated images so the site has something to show.
    /// </summary>
    public class DatabaseSeedHelper
    {
        private static readonly PasswordHasher<User> _passwordHasher = new();

        public static async Task SeedDatabase(AppDbContext context, string libraryRootPath)
        {
            context.Users.Add(new User
            {
                Username = "admin",
                PasswordHash = _passwordHasher.HashPassword(new User(), "test123!")
            });

            await context.EnvironmentalSettings.AddAsync(new EnvironmentalSetting
            {
                Key = EnvironmentalSettingEnum.HangfireUsername.ToString(),
                Value = "admin"
            });

            await context.EnvironmentalSettings.AddAsync(new EnvironmentalSetting
            {
                Key = EnvironmentalSettingEnum.HangfirePassword.ToString(),
                Value = "password"
            });

            // Set GEMINI_API_KEY before starting the API to try AI captions locally
            await context.EnvironmentalSettings.AddAsync(new EnvironmentalSetting
            {
                Key = EnvironmentalSettingEnum.GeminiApiKey.ToString(),
                Value = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? ""
            });

            await context.EnvironmentalSettings.AddAsync(new EnvironmentalSetting
            {
                Key = EnvironmentalSettingEnum.GeminiModel.ToString(),
                Value = DatabaseBootstrapHelper.DefaultGeminiModel
            });

            await context.SaveChangesAsync();

            // Only ever create the sample library in a folder that doesn't exist, so a real
            // photo library pointed at in launchSettings is never written to
            if (!Directory.Exists(libraryRootPath))
            {
                await CreateSampleLibrary(libraryRootPath);
            }
        }

        private static async Task CreateSampleLibrary(string libraryRootPath)
        {
            var albums = new (string FolderName, SKColor From, SKColor To, int Count)[]
            {
                ("2024-06 Lake District", new SKColor(30, 90, 140), new SKColor(150, 200, 120), 14),
                ("2025-01 Iceland", new SKColor(10, 20, 60), new SKColor(90, 200, 180), 10),
                ("2025-09 Street Photography", new SKColor(40, 40, 40), new SKColor(220, 180, 120), 8)
            };

            // A mix of landscape, portrait and panoramic shapes to exercise the justified grid
            var sizes = new (int Width, int Height)[] { (1800, 1200), (1200, 1800), (1800, 1200), (2400, 1000), (1500, 1500), (1800, 1350) };

            for (var albumIndex = 0; albumIndex < albums.Length; albumIndex++)
            {
                var album = albums[albumIndex];

                for (var i = 0; i < album.Count; i++)
                {
                    var (width, height) = sizes[i % sizes.Length];
                    var shift = (byte)(i * 12);

                    await SampleImageHelper.CreateSampleJpeg(
                        Path.Combine(libraryRootPath, album.FolderName, $"DSC_{i + 1:D4}.jpg"),
                        width,
                        height,
                        album.From.WithRed((byte)Math.Min(255, album.From.Red + shift)),
                        album.To,
                        new SampleExif(
                            Make: "NIKON CORPORATION",
                            Model: "NIKON Z 6_2",
                            LensModel: "NIKKOR Z 24-70mm f/4 S",
                            FocalLength: 35,
                            Aperture: 5.6,
                            ExposureDenominator: 250,
                            Iso: 200,
                            DateTaken: new DateTime(2024 + albumIndex, 6, 1, 10, 0, 0).AddHours(i * 3)));
                }
            }
        }
    }
}
