using Microsoft.AspNetCore.Identity;
using ShutterStream.Domain.Database.Context;
using ShutterStream.Domain.Database.Models;

namespace ShutterStream.Tests.Infrastructure
{
    public class TestDatabaseSeedHelper
    {
        private static readonly PasswordHasher<User> _passwordHasher = new();

        public static async Task<User> SeedTestUser(AppDbContext context, string username = "testuser", string password = "test123!")
        {
            var user = new User
            {
                Username = username,
                PasswordHash = _passwordHasher.HashPassword(new User(), password)
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }

        public static async Task<Album> SeedTestAlbum(AppDbContext context, string folderName, bool isPublic, int photoCount, DateTime? firstPhotoTaken = null)
        {
            var baseDate = firstPhotoTaken ?? new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

            var album = new Album
            {
                FolderName = folderName,
                Title = $"{folderName} Title",
                IsPublic = isPublic,
                AlbumDate = photoCount > 0 ? baseDate.AddHours(photoCount - 1) : null
            };

            context.Albums.Add(album);
            await context.SaveChangesAsync();

            for (var i = 0; i < photoCount; i++)
            {
                context.Photos.Add(new Photo
                {
                    AlbumId = album.Id,
                    FileName = $"IMG_{i + 1:D4}.jpg",
                    FileSizeBytes = 1000 + i,
                    FileModifiedAt = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    Width = 3000,
                    Height = 2000,
                    DateTaken = baseDate.AddHours(i),
                    CameraMake = "NIKON CORPORATION",
                    CameraModel = "NIKON Z 6_2",
                    Aperture = 5.6,
                    ExposureTime = "1/250",
                    FocalLength = 35,
                    Iso = 200
                });
            }

            await context.SaveChangesAsync();
            return album;
        }

        /// <summary>
        /// A public album with three photos (the middle one hidden), a private album with two photos
        /// and a public album with no photos
        /// </summary>
        public static async Task SeedStandardAlbums(AppDbContext context)
        {
            var publicAlbum = await SeedTestAlbum(context, "Public Album", true, 3, new DateTime(2025, 6, 1, 9, 0, 0, DateTimeKind.Utc));
            await SeedTestAlbum(context, "Private Album", false, 2, new DateTime(2025, 7, 1, 9, 0, 0, DateTimeKind.Utc));
            await SeedTestAlbum(context, "Empty Public Album", true, 0);

            var middlePhoto = context.Photos.Where(p => p.AlbumId == publicAlbum.Id).OrderBy(p => p.FileName).Skip(1).First();
            middlePhoto.IsHidden = true;
            await context.SaveChangesAsync();
        }
    }
}
