using ShutterStream.Domain.Results;
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
            var result = (await _albumsService.GetAlbum(album.Id, false)).Value;

            // Assert
            Assert.That(result.Photos.Select(p => p.PhotoId), Is.EqualTo(new[] { photos[0].Id, photos[2].Id }));
        }

        [Test]
        public async Task GetAlbum_ShouldReturnTheChosenCover()
        {
            // Arrange
            var album = DbContext.Albums.First(a => a.FolderName == "Public Album");
            var photos = DbContext.Photos.Where(p => p.AlbumId == album.Id).OrderBy(p => p.FileName).ToList();
            album.CoverPhotoId = photos[2].Id;
            await DbContext.SaveChangesAsync();

            // Act
            var result = (await _albumsService.GetAlbum(album.Id, false)).Value;

            // Assert
            Assert.That(result.CoverPhotoId, Is.EqualTo(photos[2].Id));
        }

        [Test]
        public async Task GetAlbum_WhenCoverIsHidden_ShouldFallBackToTheFirstPhoto()
        {
            // Arrange - the second photo in the seeded public album is hidden
            var album = DbContext.Albums.First(a => a.FolderName == "Public Album");
            var photos = DbContext.Photos.Where(p => p.AlbumId == album.Id).OrderBy(p => p.FileName).ToList();
            album.CoverPhotoId = photos[1].Id;
            await DbContext.SaveChangesAsync();

            // Act
            var result = (await _albumsService.GetAlbum(album.Id, false)).Value;

            // Assert
            Assert.That(result.CoverPhotoId, Is.EqualTo(photos[0].Id));
        }

        [Test]
        public async Task GetAlbum_WhenPrivateAndNotAdmin_ShouldReturnNotFound()
        {
            // Arrange
            var album = DbContext.Albums.First(a => a.FolderName == "Private Album");

            // Act & Assert
            var result = await _albumsService.GetAlbum(album.Id, false);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
            Assert.That(result.Error, Is.EqualTo("Album not found"));
        }

        [Test]
        public async Task GetAlbum_WhenPrivateAndAdmin_ShouldReturnAlbum()
        {
            // Arrange
            var album = DbContext.Albums.First(a => a.FolderName == "Private Album");

            // Act
            var result = (await _albumsService.GetAlbum(album.Id, true)).Value;

            // Assert
            Assert.That(result.IsPublic, Is.False);
            Assert.That(result.Photos, Has.Length.EqualTo(2));
        }

        [Test]
        public async Task GetAlbum_WhenNotFound_ShouldReturnNotFound()
        {
            // Act & Assert
            var result = await _albumsService.GetAlbum(99999, true);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
        }
    }
}
