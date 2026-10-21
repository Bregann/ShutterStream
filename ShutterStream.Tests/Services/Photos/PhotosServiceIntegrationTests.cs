using Microsoft.EntityFrameworkCore;
using ShutterStream.Domain.Enums;
using ShutterStream.Domain.Exceptions;
using ShutterStream.Domain.Helpers;
using ShutterStream.Domain.Services.Photos;
using ShutterStream.Tests.Infrastructure;
using SkiaSharp;

namespace ShutterStream.Tests.Services.Photos
{
    [TestFixture]
    public class PhotosServiceIntegrationTests : DatabaseIntegrationTestBase
    {
        private PhotosService _photosService = null!;
        private TestFileSystem _fileSystem = null!;
        private PhotoLibraryHelper _libraryHelper = null!;
        private List<int> _publicPhotoIds = null!;

        protected override async Task CustomSetUp()
        {
            await TestDatabaseSeedHelper.SeedStandardAlbums(DbContext);

            _fileSystem = new TestFileSystem();
            _libraryHelper = new PhotoLibraryHelper(_fileSystem.LibraryPath, _fileSystem.CachePath);
            _photosService = new PhotosService(DbContext, _libraryHelper, new ImageProcessingHelper());

            _publicPhotoIds = DbContext.Photos
                .Where(p => p.Album.FolderName == "Public Album")
                .OrderBy(p => p.FileName)
                .Select(p => p.Id)
                .ToList();
        }

        protected override Task CustomTearDown()
        {
            _fileSystem.Delete();
            return Task.CompletedTask;
        }

        [Test]
        public async Task GetPhoto_ShouldSkipHiddenPhotosForPreviousAndNext()
        {
            // Act - photo 0 and photo 2 are visible, photo 1 is hidden
            var result = await _photosService.GetPhoto(_publicPhotoIds[0], false);

            // Assert
            Assert.That(result.PreviousPhotoId, Is.Null);
            Assert.That(result.NextPhotoId, Is.EqualTo(_publicPhotoIds[2]));
            Assert.That(result.PositionInAlbum, Is.EqualTo(1));
            Assert.That(result.AlbumPhotoCount, Is.EqualTo(2));
        }

        [Test]
        public async Task GetPhoto_ShouldReturnExifData()
        {
            // Act
            var result = await _photosService.GetPhoto(_publicPhotoIds[0], false);

            // Assert
            Assert.That(result.CameraModel, Is.EqualTo("NIKON Z 6_2"));
            Assert.That(result.Aperture, Is.EqualTo(5.6));
            Assert.That(result.ExposureTime, Is.EqualTo("1/250"));
            Assert.That(result.AlbumTitle, Is.EqualTo("Public Album Title"));
        }

        [Test]
        public async Task GetPhoto_WhenHiddenAndNotAdmin_ShouldThrowNotFoundException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(async () => await _photosService.GetPhoto(_publicPhotoIds[1], false));
        }

        [Test]
        public async Task GetPhoto_WhenHiddenAndAdmin_ShouldIncludeItInTheSequence()
        {
            // Act
            var result = await _photosService.GetPhoto(_publicPhotoIds[1], true);

            // Assert
            Assert.That(result.PreviousPhotoId, Is.EqualTo(_publicPhotoIds[0]));
            Assert.That(result.NextPhotoId, Is.EqualTo(_publicPhotoIds[2]));
            Assert.That(result.PositionInAlbum, Is.EqualTo(2));
        }

        [Test]
        public async Task GetPhoto_WhenAlbumIsPrivateAndNotAdmin_ShouldThrowNotFoundException()
        {
            // Arrange
            var privatePhotoId = DbContext.Photos.First(p => p.Album.FolderName == "Private Album").Id;

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(async () => await _photosService.GetPhoto(privatePhotoId, false));
        }

        [Test]
        public async Task GetPhotoImage_WhenThumbnailMissing_ShouldGenerateItOnDemand()
        {
            // Arrange
            await SampleImageHelper.CreateSampleJpeg(
                Path.Combine(_fileSystem.LibraryPath, "Public Album", "IMG_0001.jpg"), 3000, 2000, SKColors.Red, SKColors.Blue);

            // Act
            var result = await _photosService.GetPhotoImage(_publicPhotoIds[0], PhotoSize.Small, false);

            // Assert
            Assert.That(File.Exists(result.FilePath), Is.True);
            Assert.That(result.IsPubliclyVisible, Is.True);

            using var codec = SKCodec.Create(result.FilePath);
            Assert.That(codec.Info.Width, Is.EqualTo(1024));

            var photo = await DbContext.Photos.AsNoTracking().FirstAsync(p => p.Id == _publicPhotoIds[0]);
            Assert.That(photo.ThumbnailsGenerated, Is.True);
        }

        [Test]
        public async Task GetPhotoImage_WhenSourceFileMissing_ShouldThrowNotFoundException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(async () => await _photosService.GetPhotoImage(_publicPhotoIds[0], PhotoSize.Small, false));
            Assert.That(exception.Message, Is.EqualTo("Photo file not found"));
        }

        [Test]
        public async Task GetPhotoImage_WhenHiddenAndNotAdmin_ShouldThrowNotFoundException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(async () => await _photosService.GetPhotoImage(_publicPhotoIds[1], PhotoSize.Large, false));
        }

        [Test]
        public async Task GetPhotoImage_WhenHiddenAndAdmin_ShouldNotBeMarkedPublic()
        {
            // Arrange
            await SampleImageHelper.CreateSampleJpeg(
                Path.Combine(_fileSystem.LibraryPath, "Public Album", "IMG_0002.jpg"), 400, 300, SKColors.Red, SKColors.Blue);

            // Act
            var result = await _photosService.GetPhotoImage(_publicPhotoIds[1], PhotoSize.Large, true);

            // Assert - so the controller sends no-store rather than a public cache header
            Assert.That(result.IsPubliclyVisible, Is.False);
        }
    }
}
