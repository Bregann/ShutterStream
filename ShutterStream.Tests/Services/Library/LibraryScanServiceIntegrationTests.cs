using Microsoft.EntityFrameworkCore;
using ShutterStream.Domain.Enums;
using ShutterStream.Domain.Extensions;
using ShutterStream.Domain.Helpers;
using ShutterStream.Domain.Services.Library;
using ShutterStream.Tests.Infrastructure;
using SkiaSharp;

namespace ShutterStream.Tests.Services.Library
{
    [TestFixture]
    public class LibraryScanServiceIntegrationTests : DatabaseIntegrationTestBase
    {
        private LibraryScanService _scanService = null!;
        private LibraryScanStatusHelper _scanStatusHelper = null!;
        private PhotoLibraryHelper _libraryHelper = null!;
        private TestFileSystem _fileSystem = null!;

        private static readonly SampleExif _sampleExif = new(
            Make: "NIKON CORPORATION",
            Model: "NIKON Z 6_2",
            LensModel: "NIKKOR Z 24-70mm f/4 S",
            FocalLength: 50,
            Aperture: 4,
            ExposureDenominator: 500,
            Iso: 400,
            DateTaken: new DateTime(2025, 3, 14, 15, 9, 26));

        protected override Task CustomSetUp()
        {
            _fileSystem = new TestFileSystem();
            _libraryHelper = new PhotoLibraryHelper(_fileSystem.LibraryPath, _fileSystem.CachePath);
            _scanStatusHelper = new LibraryScanStatusHelper();
            _scanService = new LibraryScanService(DbContext, _libraryHelper, new ImageProcessingHelper(), _scanStatusHelper);

            return Task.CompletedTask;
        }

        protected override Task CustomTearDown()
        {
            _fileSystem.Delete();
            return Task.CompletedTask;
        }

        private Task CreatePhoto(string folderName, string fileName, int width = 1600, int height = 1200, SampleExif? exif = null)
        {
            return SampleImageHelper.CreateSampleJpeg(
                Path.Combine(_fileSystem.LibraryPath, folderName, fileName), width, height, SKColors.Orange, SKColors.Purple, exif);
        }

        [Test]
        public async Task ScanLibrary_ShouldCreatePrivateAlbumsForEachFolder()
        {
            // Arrange
            await CreatePhoto("Iceland", "DSC_0001.jpg");
            await CreatePhoto("Lake District", "DSC_0001.jpg");

            // Act
            var result = await _scanService.ScanLibrary();

            // Assert
            Assert.That(result!.AlbumsAdded, Is.EqualTo(2));

            var albums = await DbContext.Albums.AsNoTracking().OrderBy(a => a.FolderName).ToListAsync();
            Assert.That(albums.Select(a => a.Title), Is.EqualTo(new[] { "Iceland", "Lake District" }));
            Assert.That(albums.All(a => !a.IsPublic), Is.True, "New albums must stay private until published");
        }

        [Test]
        public async Task ScanLibrary_ShouldReadExifData()
        {
            // Arrange
            await CreatePhoto("Iceland", "DSC_0001.jpg", exif: _sampleExif);

            // Act
            await _scanService.ScanLibrary();

            // Assert
            var photo = await DbContext.Photos.AsNoTracking().SingleAsync();
            Assert.That(photo.CameraMake, Is.EqualTo("NIKON CORPORATION"));
            Assert.That(photo.CameraModel, Is.EqualTo("NIKON Z 6_2"));
            Assert.That(photo.LensModel, Is.EqualTo("NIKKOR Z 24-70mm f/4 S"));
            Assert.That(photo.FocalLength, Is.EqualTo(50));
            Assert.That(photo.Aperture, Is.EqualTo(4));
            Assert.That(photo.ExposureTime, Is.EqualTo("1/500"));
            Assert.That(photo.Iso, Is.EqualTo(400));
            Assert.That(photo.DateTaken, Is.EqualTo(new DateTime(2025, 3, 14, 15, 9, 26, DateTimeKind.Utc)));

            var album = await DbContext.Albums.AsNoTracking().SingleAsync();
            Assert.That(album.AlbumDate, Is.EqualTo(photo.DateTaken));
        }

        [Test]
        public async Task ScanLibrary_ShouldGenerateThumbnailsForEverySize()
        {
            // Arrange
            await CreatePhoto("Iceland", "DSC_0001.jpg", 3000, 2000);

            // Act
            var result = await _scanService.ScanLibrary();

            // Assert
            var photo = await DbContext.Photos.AsNoTracking().SingleAsync();
            Assert.That(photo.ThumbnailsGenerated, Is.True);
            Assert.That(result!.ThumbnailsGenerated, Is.EqualTo(1));

            foreach (var size in PhotoSizes.Pregenerated)
            {
                using var codec = SKCodec.Create(_libraryHelper.GetThumbnailPath(photo.Id, size));
                Assert.That(Math.Max(codec.Info.Width, codec.Info.Height), Is.EqualTo(_libraryHelper.GetMaxDimension(size)));
            }

            // Full size takes too much disk to make for every photo, it's only generated when opened
            Assert.That(File.Exists(_libraryHelper.GetThumbnailPath(photo.Id, PhotoSize.Full)), Is.False);
        }

        [Test]
        public async Task ScanLibrary_WithRotatedPhoto_ShouldStoreDisplayDimensions()
        {
            // Arrange - orientation 6 means the camera was held portrait
            await CreatePhoto("Iceland", "DSC_0001.jpg", 3000, 2000, _sampleExif with { Orientation = 6 });

            // Act
            await _scanService.ScanLibrary();

            // Assert
            var photo = await DbContext.Photos.AsNoTracking().SingleAsync();
            Assert.That(photo.Width, Is.EqualTo(2000));
            Assert.That(photo.Height, Is.EqualTo(3000));

            using var codec = SKCodec.Create(_libraryHelper.GetThumbnailPath(photo.Id, PhotoSize.Small));
            Assert.That(codec.Info.Height, Is.GreaterThan(codec.Info.Width), "Thumbnail should be rotated upright");
        }

        [Test]
        public async Task ScanLibrary_ShouldIgnoreUnsupportedAndHiddenFiles()
        {
            // Arrange
            await CreatePhoto("Iceland", "DSC_0001.jpg");
            await File.WriteAllTextAsync(Path.Combine(_fileSystem.LibraryPath, "Iceland", "DSC_0001.NEF"), "raw");
            await File.WriteAllTextAsync(Path.Combine(_fileSystem.LibraryPath, "Iceland", ".DS_Store"), "junk");
            Directory.CreateDirectory(Path.Combine(_fileSystem.LibraryPath, ".thumbnails"));

            // Act
            await _scanService.ScanLibrary();

            // Assert
            Assert.That(await DbContext.Photos.CountAsync(), Is.EqualTo(1));
            Assert.That(await DbContext.Albums.CountAsync(), Is.EqualTo(1));
        }

        [Test]
        public async Task ScanLibrary_WithSubFolders_ShouldAddThemToTheSameAlbum()
        {
            // Arrange - the same file name in two day folders must not clash
            await CreatePhoto("Japan", "Overview.jpg");
            await CreatePhoto(Path.Combine("Japan", "Day 1"), "DSC_0001.jpg");
            await CreatePhoto(Path.Combine("Japan", "Day 2"), "DSC_0001.jpg");
            await CreatePhoto(Path.Combine("Japan", "Day 2", "Evening"), "DSC_0002.jpg");
            await CreatePhoto(Path.Combine("Japan", ".edits"), "DSC_0003.jpg");

            // Act
            var result = await _scanService.ScanLibrary();

            // Assert
            Assert.That(result!.AlbumsAdded, Is.EqualTo(1));

            var photos = await DbContext.Photos.AsNoTracking().OrderBy(p => p.SubFolder).ToListAsync();
            Assert.That(photos.Select(p => p.SubFolder), Is.EqualTo(new[] { "", "Day 1", "Day 2", "Day 2/Evening" }));
            Assert.That(photos.All(p => p.ThumbnailsGenerated), Is.True, "Thumbnails must be found from the sub folder path");
        }

        [Test]
        public async Task ScanLibrary_WhenSubFolderPhotoDeleted_ShouldOnlyRemoveThatPhoto()
        {
            // Arrange
            await CreatePhoto(Path.Combine("Japan", "Day 1"), "DSC_0001.jpg");
            await CreatePhoto(Path.Combine("Japan", "Day 2"), "DSC_0001.jpg");
            await _scanService.ScanLibrary();

            File.Delete(Path.Combine(_fileSystem.LibraryPath, "Japan", "Day 2", "DSC_0001.jpg"));

            // Act
            var result = await _scanService.ScanLibrary();

            // Assert
            Assert.That(result!.PhotosRemoved, Is.EqualTo(1));
            var remaining = await DbContext.Photos.AsNoTracking().SingleAsync();
            Assert.That(remaining.SubFolder, Is.EqualTo("Day 1"));
        }

        [Test]
        public async Task OrderForDisplay_ShouldPutLoosePhotosFirstThenSubFoldersByEarliestPhoto()
        {
            // Arrange - "Day 10" sorts before "Day 2" by name, but was taken later
            await CreatePhoto("Japan", "Overview.jpg", exif: _sampleExif with { DateTaken = new DateTime(2025, 3, 20, 12, 0, 0) });
            await CreatePhoto(Path.Combine("Japan", "Day 10"), "A.jpg", exif: _sampleExif with { DateTaken = new DateTime(2025, 3, 10, 9, 0, 0) });
            await CreatePhoto(Path.Combine("Japan", "Day 2"), "B.jpg", exif: _sampleExif with { DateTaken = new DateTime(2025, 3, 2, 18, 0, 0) });
            await CreatePhoto(Path.Combine("Japan", "Day 2"), "A.jpg", exif: _sampleExif with { DateTaken = new DateTime(2025, 3, 2, 9, 0, 0) });
            await _scanService.ScanLibrary();

            // Act
            var ordered = await DbContext.Photos.AsNoTracking()
                .OrderForDisplay()
                .Select(p => p.SubFolder + "/" + p.FileName)
                .ToListAsync();

            // Assert
            Assert.That(ordered, Is.EqualTo(new[] { "/Overview.jpg", "Day 2/A.jpg", "Day 2/B.jpg", "Day 10/A.jpg" }));
        }

        [Test]
        public async Task ScanLibrary_WithCorruptFile_ShouldSkipItAndCarryOn()
        {
            // Arrange
            await CreatePhoto("Iceland", "DSC_0001.jpg");
            await File.WriteAllTextAsync(Path.Combine(_fileSystem.LibraryPath, "Iceland", "DSC_0002.jpg"), "not really a jpeg");

            // Act
            var result = await _scanService.ScanLibrary();

            // Assert
            Assert.That(result!.PhotosFailed, Is.EqualTo(1));
            Assert.That(result.PhotosAdded, Is.EqualTo(1));
        }

        [Test]
        public async Task ScanLibrary_WhenFileDeleted_ShouldRemovePhotoAndThumbnails()
        {
            // Arrange
            await CreatePhoto("Iceland", "DSC_0001.jpg");
            await CreatePhoto("Iceland", "DSC_0002.jpg");
            await _scanService.ScanLibrary();

            var removedPhoto = await DbContext.Photos.AsNoTracking().FirstAsync(p => p.FileName == "DSC_0002.jpg");
            File.Delete(Path.Combine(_fileSystem.LibraryPath, "Iceland", "DSC_0002.jpg"));

            // Act
            var result = await _scanService.ScanLibrary();

            // Assert
            Assert.That(result!.PhotosRemoved, Is.EqualTo(1));
            Assert.That(await DbContext.Photos.AnyAsync(p => p.Id == removedPhoto.Id), Is.False);
            Assert.That(File.Exists(_libraryHelper.GetThumbnailPath(removedPhoto.Id, PhotoSize.Small)), Is.False);
        }

        [Test]
        public async Task ScanLibrary_WhenCoverPhotoDeleted_ShouldClearCover()
        {
            // Arrange
            await CreatePhoto("Iceland", "DSC_0001.jpg");
            await CreatePhoto("Iceland", "DSC_0002.jpg");
            await _scanService.ScanLibrary();

            var album = await DbContext.Albums.SingleAsync();
            album.CoverPhotoId = (await DbContext.Photos.FirstAsync(p => p.FileName == "DSC_0002.jpg")).Id;
            await DbContext.SaveChangesAsync();

            File.Delete(Path.Combine(_fileSystem.LibraryPath, "Iceland", "DSC_0002.jpg"));

            // Act
            await _scanService.ScanLibrary();

            // Assert
            var updatedAlbum = await DbContext.Albums.AsNoTracking().SingleAsync();
            Assert.That(updatedAlbum.CoverPhotoId, Is.Null);
        }

        [Test]
        public async Task ScanLibrary_WhenFileChanged_ShouldUpdateMetadataAndRegenerateThumbnails()
        {
            // Arrange
            await CreatePhoto("Iceland", "DSC_0001.jpg", 1600, 1200);
            await _scanService.ScanLibrary();

            var filePath = Path.Combine(_fileSystem.LibraryPath, "Iceland", "DSC_0001.jpg");
            await CreatePhoto("Iceland", "DSC_0001.jpg", 1200, 1600);
            File.SetLastWriteTimeUtc(filePath, DateTime.UtcNow.AddMinutes(5));

            // Act
            var result = await _scanService.ScanLibrary();

            // Assert
            Assert.That(result!.PhotosUpdated, Is.EqualTo(1));
            Assert.That(result.ThumbnailsGenerated, Is.EqualTo(1));

            var photo = await DbContext.Photos.AsNoTracking().SingleAsync();
            Assert.That(photo.Width, Is.EqualTo(1200));
            Assert.That(photo.Height, Is.EqualTo(1600));
        }

        [Test]
        public async Task ScanLibrary_WhenNothingChanged_ShouldNotUpdateAnything()
        {
            // Arrange
            await CreatePhoto("Iceland", "DSC_0001.jpg");
            await _scanService.ScanLibrary();

            // Act
            var result = await _scanService.ScanLibrary();

            // Assert
            Assert.That(result!.PhotosAdded + result.PhotosUpdated + result.PhotosRemoved + result.ThumbnailsGenerated, Is.Zero);
        }

        [Test]
        public async Task ScanLibrary_WhenFolderDeleted_ShouldRemoveAlbum()
        {
            // Arrange
            await CreatePhoto("Iceland", "DSC_0001.jpg");
            await CreatePhoto("Lake District", "DSC_0001.jpg");
            await _scanService.ScanLibrary();

            var album = await DbContext.Albums.SingleAsync(a => a.FolderName == "Lake District");
            album.CoverPhotoId = (await DbContext.Photos.FirstAsync(p => p.AlbumId == album.Id)).Id;
            await DbContext.SaveChangesAsync();

            Directory.Delete(Path.Combine(_fileSystem.LibraryPath, "Lake District"), true);

            // Act
            var result = await _scanService.ScanLibrary();

            // Assert
            Assert.That(result!.AlbumsRemoved, Is.EqualTo(1));
            Assert.That(await DbContext.Albums.AsNoTracking().Select(a => a.FolderName).ToListAsync(), Is.EqualTo(new[] { "Iceland" }));
            Assert.That(await DbContext.Photos.CountAsync(), Is.EqualTo(1));
        }

        [Test]
        public async Task ScanLibrary_ShouldKeepAdminChangesAcrossScans()
        {
            // Arrange
            await CreatePhoto("Iceland", "DSC_0001.jpg");
            await _scanService.ScanLibrary();

            var album = await DbContext.Albums.SingleAsync();
            album.IsPublic = true;
            album.Title = "Iceland 2025";
            await DbContext.SaveChangesAsync();

            // Act
            await CreatePhoto("Iceland", "DSC_0002.jpg");
            await _scanService.ScanLibrary();

            // Assert
            var updated = await DbContext.Albums.AsNoTracking().SingleAsync();
            Assert.That(updated.IsPublic, Is.True);
            Assert.That(updated.Title, Is.EqualTo("Iceland 2025"));
        }

        [Test]
        public async Task ScanLibrary_WhenLibraryEmptiesOut_ShouldRefuseToRemoveEveryAlbum()
        {
            // Arrange - simulates a network drive dropping
            await CreatePhoto("Iceland", "DSC_0001.jpg");
            await _scanService.ScanLibrary();
            Directory.Delete(Path.Combine(_fileSystem.LibraryPath, "Iceland"), true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await _scanService.ScanLibrary());
            Assert.That(await DbContext.Albums.CountAsync(), Is.EqualTo(1));
            Assert.That(_scanStatusHelper.GetStatus().IsRunning, Is.False, "A failed scan must release the lock");
        }

        [Test]
        public async Task ScanLibrary_WhenLibraryFolderMissing_ShouldThrow()
        {
            // Arrange
            Directory.Delete(_fileSystem.LibraryPath, true);

            // Act & Assert
            await Assert.ThrowsAsync<DirectoryNotFoundException>(async () => await _scanService.ScanLibrary());
        }

        [Test]
        public async Task ScanLibrary_WhenScanAlreadyRunning_ShouldReturnNull()
        {
            // Arrange
            _scanStatusHelper.TryStartScan();

            // Act
            var result = await _scanService.ScanLibrary();

            // Assert
            Assert.That(result, Is.Null);
        }
    }
}
