using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.EntityFrameworkCore;
using Moq;
using ShutterStream.Domain.DTOs.Admin.Requests;
using ShutterStream.Domain.Exceptions;
using ShutterStream.Domain.Helpers;
using ShutterStream.Domain.Interfaces.Api.Captions;
using ShutterStream.Domain.Interfaces.Api.Library;
using ShutterStream.Domain.Interfaces.Helpers;
using ShutterStream.Domain.Services.Admin;
using ShutterStream.Tests.Infrastructure;
using MockFactory = ShutterStream.Tests.Infrastructure.MockFactory;

namespace ShutterStream.Tests.Services.Admin
{
    [TestFixture]
    public class AdminServiceIntegrationTests : DatabaseIntegrationTestBase
    {
        private AdminService _adminService = null!;
        private Mock<IBackgroundJobClient> _mockBackgroundJobClient = null!;
        private LibraryScanStatusHelper _scanStatusHelper = null!;
        private CaptionJobStatusHelper _captionJobStatusHelper = null!;
        private Mock<IAiCaptionHelper> _mockAiCaptionHelper = null!;
        private TestFileSystem _fileSystem = null!;

        protected override async Task CustomSetUp()
        {
            await TestDatabaseSeedHelper.SeedStandardAlbums(DbContext);

            _fileSystem = new TestFileSystem();
            _mockBackgroundJobClient = MockFactory.CreateBackgroundJobClient();
            _scanStatusHelper = new LibraryScanStatusHelper();
            _captionJobStatusHelper = new CaptionJobStatusHelper();
            _mockAiCaptionHelper = MockFactory.CreateAiCaptionHelper();

            _adminService = new AdminService(
                DbContext,
                new PhotoLibraryHelper(_fileSystem.LibraryPath, _fileSystem.CachePath),
                _scanStatusHelper,
                _mockAiCaptionHelper.Object,
                _captionJobStatusHelper,
                _mockBackgroundJobClient.Object);
        }

        protected override Task CustomTearDown()
        {
            _fileSystem.Delete();
            return Task.CompletedTask;
        }

        [Test]
        public async Task GetLibraryStatus_ShouldReturnCounts()
        {
            // Act
            var result = await _adminService.GetLibraryStatus();

            // Assert
            Assert.That(result.TotalAlbums, Is.EqualTo(3));
            Assert.That(result.PublicAlbums, Is.EqualTo(2));
            Assert.That(result.TotalPhotos, Is.EqualTo(5));
            Assert.That(result.PendingThumbnails, Is.EqualTo(5));
            Assert.That(result.LibraryPathExists, Is.True);
            Assert.That(result.IsScanRunning, Is.False);
        }

        [Test]
        public void QueueLibraryScan_ShouldEnqueueScanJob()
        {
            // Act
            _adminService.QueueLibraryScan();

            // Assert
            _mockBackgroundJobClient.Verify(x => x.Create(
                It.Is<Job>(job => job.Type == typeof(ILibraryScanService) && job.Method.Name == nameof(ILibraryScanService.ScanLibrary)),
                It.IsAny<EnqueuedState>()), Times.Once);
        }

        [Test]
        public void QueueLibraryScan_WhenScanAlreadyRunning_ShouldThrowConflictException()
        {
            // Arrange
            _scanStatusHelper.TryStartScan();

            // Act & Assert
            Assert.Throws<ConflictException>(() => _adminService.QueueLibraryScan());
            _mockBackgroundJobClient.Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
        }

        [Test]
        public async Task GetAdminAlbums_ShouldIncludePrivateAndEmptyAlbums()
        {
            // Act
            var result = await _adminService.GetAdminAlbums();

            // Assert
            Assert.That(result.Albums, Has.Length.EqualTo(3));

            var publicAlbum = result.Albums.First(a => a.FolderName == "Public Album");
            Assert.That(publicAlbum.PhotoCount, Is.EqualTo(3));
            Assert.That(publicAlbum.HiddenPhotoCount, Is.EqualTo(1));
        }

        [Test]
        public async Task GetAdminAlbum_ShouldIncludeHiddenPhotos()
        {
            // Arrange
            var album = DbContext.Albums.First(a => a.FolderName == "Public Album");

            // Act
            var result = await _adminService.GetAdminAlbum(album.Id);

            // Assert
            Assert.That(result.Photos, Has.Length.EqualTo(3));
            Assert.That(result.Photos.Count(p => p.IsHidden), Is.EqualTo(1));
        }

        [Test]
        public async Task UpdateAlbumDetails_ShouldTrimAndSave()
        {
            // Arrange
            var album = DbContext.Albums.First(a => a.FolderName == "Public Album");

            // Act
            await _adminService.UpdateAlbumDetails(new UpdateAlbumDetailsRequest
            {
                AlbumId = album.Id,
                Title = "  Iceland 2025 ",
                Description = " Northern lights trip "
            });

            // Assert
            var updated = await DbContext.Albums.AsNoTracking().FirstAsync(a => a.Id == album.Id);
            Assert.That(updated.Title, Is.EqualTo("Iceland 2025"));
            Assert.That(updated.Description, Is.EqualTo("Northern lights trip"));
            Assert.That(updated.FolderName, Is.EqualTo("Public Album"));
        }

        [Test]
        public async Task UpdateAlbumDetails_WithEmptyTitle_ShouldThrowBadRequestException()
        {
            // Arrange
            var album = DbContext.Albums.First(a => a.FolderName == "Public Album");

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(async () => await _adminService.UpdateAlbumDetails(new UpdateAlbumDetailsRequest
            {
                AlbumId = album.Id,
                Title = "   ",
                Description = ""
            }));
        }

        [Test]
        public async Task SetAlbumVisibility_ShouldPublishAlbum()
        {
            // Arrange
            var album = DbContext.Albums.First(a => a.FolderName == "Private Album");

            // Act
            await _adminService.SetAlbumVisibility(new SetAlbumVisibilityRequest { AlbumId = album.Id, IsPublic = true });

            // Assert
            var updated = await DbContext.Albums.AsNoTracking().FirstAsync(a => a.Id == album.Id);
            Assert.That(updated.IsPublic, Is.True);
        }

        [Test]
        public async Task SetAlbumVisibility_WhenAlbumNotFound_ShouldThrowNotFoundException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(async () =>
                await _adminService.SetAlbumVisibility(new SetAlbumVisibilityRequest { AlbumId = 99999, IsPublic = true }));
        }

        [Test]
        public async Task SetAlbumCover_ShouldSetCoverPhoto()
        {
            // Arrange
            var album = DbContext.Albums.First(a => a.FolderName == "Public Album");
            var photo = DbContext.Photos.First(p => p.AlbumId == album.Id);

            // Act
            await _adminService.SetAlbumCover(new SetAlbumCoverRequest { AlbumId = album.Id, PhotoId = photo.Id });

            // Assert
            var updated = await DbContext.Albums.AsNoTracking().FirstAsync(a => a.Id == album.Id);
            Assert.That(updated.CoverPhotoId, Is.EqualTo(photo.Id));
        }

        [Test]
        public async Task SetAlbumCover_WithPhotoFromAnotherAlbum_ShouldThrowBadRequestException()
        {
            // Arrange
            var album = DbContext.Albums.First(a => a.FolderName == "Public Album");
            var otherPhoto = DbContext.Photos.First(p => p.Album.FolderName == "Private Album");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadRequestException>(async () =>
                await _adminService.SetAlbumCover(new SetAlbumCoverRequest { AlbumId = album.Id, PhotoId = otherPhoto.Id }));

            Assert.That(exception.Message, Is.EqualTo("The cover photo must be in the album"));
        }

        [Test]
        public async Task SetPhotoVisibility_ShouldHidePhoto()
        {
            // Arrange
            var photo = DbContext.Photos.First(p => !p.IsHidden);

            // Act
            await _adminService.SetPhotoVisibility(new SetPhotoVisibilityRequest { PhotoId = photo.Id, IsHidden = true });

            // Assert
            var updated = await DbContext.Photos.AsNoTracking().FirstAsync(p => p.Id == photo.Id);
            Assert.That(updated.IsHidden, Is.True);
        }

        [Test]
        public async Task QueueAlbumCaptions_ShouldEnqueueCaptionJob()
        {
            // Arrange
            var album = DbContext.Albums.First(a => a.FolderName == "Public Album");

            // Act
            await _adminService.QueueAlbumCaptions(new GenerateAlbumCaptionsRequest { AlbumId = album.Id });

            // Assert
            _mockBackgroundJobClient.Verify(x => x.Create(
                It.Is<Job>(job => job.Type == typeof(IPhotoCaptionService) && (int)job.Args[0] == album.Id && (bool)job.Args[1] == false),
                It.IsAny<EnqueuedState>()), Times.Once);
        }

        [Test]
        public async Task QueueAlbumCaptions_WhenNoApiKey_ShouldThrowBadRequestException()
        {
            // Arrange
            _mockAiCaptionHelper.Setup(x => x.IsEnabled).Returns(false);
            var album = DbContext.Albums.First(a => a.FolderName == "Public Album");

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(async () =>
                await _adminService.QueueAlbumCaptions(new GenerateAlbumCaptionsRequest { AlbumId = album.Id }));
            _mockBackgroundJobClient.Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
        }

        [Test]
        public async Task QueueAlbumCaptions_WhenJobAlreadyRunning_ShouldThrowConflictException()
        {
            // Arrange
            var album = DbContext.Albums.First(a => a.FolderName == "Public Album");
            _captionJobStatusHelper.TryStartJob(album.Id);

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(async () =>
                await _adminService.QueueAlbumCaptions(new GenerateAlbumCaptionsRequest { AlbumId = album.Id }));
        }

        [Test]
        public async Task QueueAlbumCaptions_WhenAlbumNotFound_ShouldThrowNotFoundException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(async () =>
                await _adminService.QueueAlbumCaptions(new GenerateAlbumCaptionsRequest { AlbumId = 99999 }));
        }

        [Test]
        public async Task GetAdminAlbum_ShouldReportCaptionJobProgressForThatAlbumOnly()
        {
            // Arrange
            var album = DbContext.Albums.First(a => a.FolderName == "Public Album");
            var otherAlbum = DbContext.Albums.First(a => a.FolderName == "Private Album");
            _captionJobStatusHelper.TryStartJob(album.Id);
            _captionJobStatusHelper.SetProgress(1, 0, 3);

            // Act
            var result = await _adminService.GetAdminAlbum(album.Id);
            var otherResult = await _adminService.GetAdminAlbum(otherAlbum.Id);

            // Assert
            Assert.That(result.CaptionStatus.IsRunning, Is.True);
            Assert.That(result.CaptionStatus.Processed, Is.EqualTo(1));
            Assert.That(result.CaptionStatus.Total, Is.EqualTo(3));
            Assert.That(otherResult.CaptionStatus.IsRunning, Is.False);
            Assert.That(otherResult.CaptionStatus.IsOtherAlbumRunning, Is.True);
        }

        [Test]
        public async Task UpdatePhotoCaption_ShouldSaveAndMarkAsHandWritten()
        {
            // Arrange
            var photo = DbContext.Photos.First();
            photo.Title = "AI title";
            photo.CaptionGeneratedAt = DateTime.UtcNow;
            await DbContext.SaveChangesAsync();

            // Act
            await _adminService.UpdatePhotoCaption(new UpdatePhotoCaptionRequest { PhotoId = photo.Id, Title = " My title ", Description = " " });

            // Assert
            var updated = await DbContext.Photos.AsNoTracking().FirstAsync(p => p.Id == photo.Id);
            Assert.That(updated.Title, Is.EqualTo("My title"));
            Assert.That(updated.Description, Is.Null);
            Assert.That(updated.CaptionGeneratedAt, Is.Null);
        }
    }
}
