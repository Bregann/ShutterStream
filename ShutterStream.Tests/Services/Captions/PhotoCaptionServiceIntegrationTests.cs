using Microsoft.EntityFrameworkCore;
using Moq;
using ShutterStream.Domain.DTOs.Captions;
using ShutterStream.Domain.Helpers;
using ShutterStream.Domain.Interfaces.Helpers;
using ShutterStream.Domain.Services.Captions;
using ShutterStream.Tests.Infrastructure;
using SkiaSharp;
using MockFactory = ShutterStream.Tests.Infrastructure.MockFactory;

namespace ShutterStream.Tests.Services.Captions
{
    [TestFixture]
    public class PhotoCaptionServiceIntegrationTests : DatabaseIntegrationTestBase
    {
        private PhotoCaptionService _captionService = null!;
        private Mock<IAiCaptionHelper> _mockAiCaptionHelper = null!;
        private CaptionJobStatusHelper _captionJobStatusHelper = null!;
        private TestFileSystem _fileSystem = null!;
        private int _albumId;

        protected override async Task CustomSetUp()
        {
            _fileSystem = new TestFileSystem();

            var album = await TestDatabaseSeedHelper.SeedTestAlbum(DbContext, "Iceland", false, 3);
            _albumId = album.Id;

            // The seed helper only creates database rows, so create the matching files on disk
            foreach (var photo in DbContext.Photos.Where(p => p.AlbumId == _albumId).ToList())
            {
                await SampleImageHelper.CreateSampleJpeg(Path.Combine(_fileSystem.LibraryPath, "Iceland", photo.FileName), 1600, 1200, SKColors.Teal, SKColors.Navy);
            }

            _mockAiCaptionHelper = MockFactory.CreateAiCaptionHelper();
            _captionJobStatusHelper = new CaptionJobStatusHelper();

            _captionService = new PhotoCaptionService(
                DbContext,
                new PhotoLibraryHelper(_fileSystem.LibraryPath, _fileSystem.CachePath),
                new ImageProcessingHelper(),
                _mockAiCaptionHelper.Object,
                _captionJobStatusHelper);
        }

        protected override Task CustomTearDown()
        {
            _fileSystem.Delete();
            return Task.CompletedTask;
        }

        [Test]
        public async Task GenerateAlbumCaptions_ShouldCaptionEveryPhoto()
        {
            // Act
            await _captionService.GenerateAlbumCaptions(_albumId, false);

            // Assert
            var photos = await DbContext.Photos.AsNoTracking().Where(p => p.AlbumId == _albumId).ToListAsync();
            Assert.That(photos.All(p => p.Title != null && p.Description != null && p.CaptionGeneratedAt != null), Is.True);
            Assert.That(_captionJobStatusHelper.GetStatus().LastMessage, Is.EqualTo("Captioned 3 of 3 photos"));
        }

        [Test]
        public async Task GenerateAlbumCaptions_ShouldSendSmallThumbnailAndAlbumContext()
        {
            // Arrange
            var album = await DbContext.Albums.FirstAsync(a => a.Id == _albumId);
            album.Title = "Iceland 2025";
            await DbContext.SaveChangesAsync();

            // Act
            await _captionService.GenerateAlbumCaptions(_albumId, false);

            // Assert - the 1024px thumbnail is sent rather than the full size file
            _mockAiCaptionHelper.Verify(x => x.GenerateCaption(
                It.Is<byte[]>(bytes => IsLongEdge(bytes, 1024)),
                It.Is<PhotoCaptionContext>(c => c.AlbumTitle == "Iceland 2025" && c.DateTaken != null),
                It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Test]
        public async Task GenerateAlbumCaptions_ShouldSkipPhotosThatAlreadyHaveCaptions()
        {
            // Arrange
            var photo = await DbContext.Photos.FirstAsync(p => p.AlbumId == _albumId);
            photo.Title = "Existing";
            photo.CaptionGeneratedAt = DateTime.UtcNow;
            await DbContext.SaveChangesAsync();

            // Act
            await _captionService.GenerateAlbumCaptions(_albumId, false);

            // Assert
            _mockAiCaptionHelper.Verify(x => x.GenerateCaption(It.IsAny<byte[]>(), It.IsAny<PhotoCaptionContext>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            var unchanged = await DbContext.Photos.AsNoTracking().FirstAsync(p => p.Id == photo.Id);
            Assert.That(unchanged.Title, Is.EqualTo("Existing"));
        }

        [Test]
        public async Task GenerateAlbumCaptions_WhenRegenerating_ShouldReplaceAiCaptionsButKeepHandWrittenOnes()
        {
            // Arrange
            var photos = await DbContext.Photos.Where(p => p.AlbumId == _albumId).OrderBy(p => p.FileName).ToListAsync();
            photos[0].Title = "Old AI caption";
            photos[0].CaptionGeneratedAt = DateTime.UtcNow.AddDays(-1);
            photos[1].Title = "Hand written";
            photos[1].CaptionGeneratedAt = null;
            await DbContext.SaveChangesAsync();

            // Act
            await _captionService.GenerateAlbumCaptions(_albumId, true);

            // Assert
            var updated = await DbContext.Photos.AsNoTracking().Where(p => p.AlbumId == _albumId).OrderBy(p => p.FileName).ToListAsync();
            Assert.That(updated[0].Title, Does.StartWith("Caption"));
            Assert.That(updated[1].Title, Is.EqualTo("Hand written"));
            Assert.That(updated[2].Title, Does.StartWith("Caption"));
        }

        [Test]
        public async Task GenerateAlbumCaptions_WhenOnePhotoFails_ShouldCaptionTheRest()
        {
            // Arrange - the first call throws, the rest succeed
            var calls = 0;
            _mockAiCaptionHelper
                .Setup(x => x.GenerateCaption(It.IsAny<byte[]>(), It.IsAny<PhotoCaptionContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Interlocked.Increment(ref calls) == 1
                    ? throw new HttpRequestException("Gemini caption request failed: TooManyRequests")
                    : new PhotoCaption { Title = "Caption", Description = "Description" });

            // Act
            await _captionService.GenerateAlbumCaptions(_albumId, false);

            // Assert
            Assert.That(await DbContext.Photos.CountAsync(p => p.AlbumId == _albumId && p.Title != null), Is.EqualTo(2));
            Assert.That(_captionJobStatusHelper.GetStatus().LastMessage, Is.EqualTo("Captioned 2 of 3 photos, 1 failed"));
        }

        [Test]
        public async Task GenerateAlbumCaptions_WhenAiDeclines_ShouldCountAsFailed()
        {
            // Arrange
            _mockAiCaptionHelper
                .Setup(x => x.GenerateCaption(It.IsAny<byte[]>(), It.IsAny<PhotoCaptionContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((PhotoCaption?)null);

            // Act
            await _captionService.GenerateAlbumCaptions(_albumId, false);

            // Assert
            Assert.That(await DbContext.Photos.CountAsync(p => p.Title != null), Is.Zero);
            Assert.That(_captionJobStatusHelper.GetStatus().LastMessage, Is.EqualTo("Captioned 0 of 3 photos, 3 failed"));
        }

        [Test]
        public async Task GenerateAlbumCaptions_WhenAnotherJobRunning_ShouldDoNothing()
        {
            // Arrange
            _captionJobStatusHelper.TryStartJob(999);

            // Act
            await _captionService.GenerateAlbumCaptions(_albumId, false);

            // Assert
            _mockAiCaptionHelper.Verify(x => x.GenerateCaption(It.IsAny<byte[]>(), It.IsAny<PhotoCaptionContext>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task GenerateAlbumCaptions_ShouldReleaseTheLockWhenFinished()
        {
            // Act
            await _captionService.GenerateAlbumCaptions(_albumId, false);

            // Assert
            var status = _captionJobStatusHelper.GetStatus();
            Assert.That(status.RunningAlbumId, Is.Null);
            Assert.That(status.LastAlbumId, Is.EqualTo(_albumId));
        }

        private static bool IsLongEdge(byte[] jpegBytes, int expected)
        {
            using var codec = SKCodec.Create(new MemoryStream(jpegBytes));
            return Math.Max(codec.Info.Width, codec.Info.Height) == expected;
        }
    }
}
