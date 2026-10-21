using ShutterStream.Domain.Exceptions;
using ShutterStream.Domain.Services.Albums;
using ShutterStream.Tests.Infrastructure;

namespace ShutterStream.Tests.Services.Albums
{
    [TestFixture]
    public class AlbumsServiceIntegrationTests : DatabaseIntegrationTestBase
    {
        private AlbumsService _albumsService = null!;

        protected override async Task CustomSetUp()
        {
            await TestDatabaseSeedHelper.SeedStandardAlbums(DbContext);
            _albumsService = new AlbumsService(DbContext);
        }

        [Test]
        public async Task GetAlbums_ShouldOnlyReturnPublicAlbumsWithVisiblePhotos()
        {
            // Act
            var result = await _albumsService.GetAlbums();

            // Assert - the private album and the empty public album are both left out
            Assert.That(result.Albums.Select(a => a.Title), Is.EqualTo(new[] { "Public Album Title" }));
        }

        [Test]
        public async Task GetAlbums_ShouldNotCountHiddenPhotos()
        {
            // Act
            var result = await _albumsService.GetAlbums();

            // Assert
            Assert.That(result.Albums[0].PhotoCount, Is.EqualTo(2));
        }

        [Test]
        public async Task GetAlbums_ShouldOrderByMostRecentAlbumDate()
        {
            // Arrange
            await TestDatabaseSeedHelper.SeedTestAlbum(DbContext, "Newer Album", true, 1, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            // Act
            var result = await _albumsService.GetAlbums();

            // Assert
            Assert.That(result.Albums.Select(a => a.Title), Is.EqualTo(new[] { "Newer Album Title", "Public Album Title" }));
        }

        [Test]
        public async Task GetAlbums_WhenNoCoverSet_ShouldUseFirstVisiblePhoto()
        {
            // Arrange
            var firstPhoto = DbContext.Photos.Where(p => p.Album.FolderName == "Public Album").OrderBy(p => p.FileName).First();

            // Act
            var result = await _albumsService.GetAlbums();

            // Assert
            Assert.That(result.Albums[0].CoverPhotoId, Is.EqualTo(firstPhoto.Id));
            Assert.That(result.Albums[0].CoverPhotoVersion, Is.Not.Null);
        }

        [Test]
        public async Task GetAlbums_WhenCoverIsHidden_ShouldFallBackToFirstVisiblePhoto()
        {
            // Arrange
            var album = DbContext.Albums.First(a => a.FolderName == "Public Album");
            var photos = DbContext.Photos.Where(p => p.AlbumId == album.Id).OrderBy(p => p.FileName).ToList();
            album.CoverPhotoId = photos[1].Id; // the hidden one
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _albumsService.GetAlbums();

            // Assert
            Assert.That(result.Albums[0].CoverPhotoId, Is.EqualTo(photos[0].Id));
        }

        [Test]
        public async Task GetAlbum_ShouldExcludeHiddenPhotosAndOrderByDateTaken()
        {
            // Arrange
            var album = DbContext.Albums.First(a => a.FolderName == "Public Album");
            var photos = DbContext.Photos.Where(p => p.AlbumId == album.Id).OrderBy(p => p.FileName).ToList();

            // Act
            var result = await _albumsService.GetAlbum(album.Id, false);

            // Assert
            Assert.That(result.Photos.Select(p => p.PhotoId), Is.EqualTo(new[] { photos[0].Id, photos[2].Id }));
        }

        [Test]
        public async Task GetAlbum_WhenPrivateAndNotAdmin_ShouldThrowNotFoundException()
        {
            // Arrange
            var album = DbContext.Albums.First(a => a.FolderName == "Private Album");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(async () => await _albumsService.GetAlbum(album.Id, false));
            Assert.That(exception.Message, Is.EqualTo("Album not found"));
        }

        [Test]
        public async Task GetAlbum_WhenPrivateAndAdmin_ShouldReturnAlbum()
        {
            // Arrange
            var album = DbContext.Albums.First(a => a.FolderName == "Private Album");

            // Act
            var result = await _albumsService.GetAlbum(album.Id, true);

            // Assert
            Assert.That(result.IsPublic, Is.False);
            Assert.That(result.Photos, Has.Length.EqualTo(2));
        }

        [Test]
        public async Task GetAlbum_WhenNotFound_ShouldThrowNotFoundException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(async () => await _albumsService.GetAlbum(99999, true));
        }
    }
}
