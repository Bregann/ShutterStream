using ShutterStream.Domain.DTOs.Library;
using ShutterStream.Domain.Helpers;
using ShutterStream.Tests.Infrastructure;
using SkiaSharp;

namespace ShutterStream.Tests.Helpers
{
    [TestFixture]
    public class ImageProcessingHelperTests
    {
        private ImageProcessingHelper _helper = null!;
        private TestFileSystem _fileSystem = null!;

        [SetUp]
        public void SetUp()
        {
            _helper = new ImageProcessingHelper();
            _fileSystem = new TestFileSystem();
        }

        [TearDown]
        public void TearDown()
        {
            _fileSystem.Delete();
        }

        [Test]
        public async Task ReadMetadata_WithoutExif_ShouldReturnDimensionsOnly()
        {
            // Arrange
            var path = Path.Combine(_fileSystem.LibraryPath, "plain.jpg");
            await SampleImageHelper.CreateSampleJpeg(path, 640, 480, SKColors.Red, SKColors.Blue);

            // Act
            var metadata = await _helper.ReadMetadata(path);

            // Assert
            Assert.That(metadata.Width, Is.EqualTo(640));
            Assert.That(metadata.Height, Is.EqualTo(480));
            Assert.That(metadata.CameraModel, Is.Null);
            Assert.That(metadata.DateTaken, Is.Null);
        }

        [TestCase(2u, "1/2")]
        [TestCase(4000u, "1/4000")]
        public async Task ReadMetadata_ShouldFormatExposureTimeAsFraction(uint denominator, string expected)
        {
            // Arrange
            var path = Path.Combine(_fileSystem.LibraryPath, "exposure.jpg");
            await SampleImageHelper.CreateSampleJpeg(path, 64, 64, SKColors.Red, SKColors.Blue, new SampleExif(
                "Make", "Model", "Lens", 24, 2.8, denominator, 100, new DateTime(2025, 1, 1)));

            // Act
            var metadata = await _helper.ReadMetadata(path);

            // Assert
            Assert.That(metadata.ExposureTime, Is.EqualTo(expected));
        }

        [Test]
        public async Task GenerateThumbnails_ShouldNotUpscaleSmallImages()
        {
            // Arrange
            var source = Path.Combine(_fileSystem.LibraryPath, "small.jpg");
            var destination = Path.Combine(_fileSystem.CachePath, "1", "large.jpg");
            await SampleImageHelper.CreateSampleJpeg(source, 800, 600, SKColors.Red, SKColors.Blue);

            // Act
            await _helper.GenerateThumbnails(source, [new ThumbnailTarget(destination, 2048)]);

            // Assert
            using var codec = SKCodec.Create(destination);
            Assert.That(codec.Info.Width, Is.EqualTo(800));
            Assert.That(codec.Info.Height, Is.EqualTo(600));
        }

        [Test]
        public async Task GenerateThumbnails_ShouldStripExif()
        {
            // Arrange
            var source = Path.Combine(_fileSystem.LibraryPath, "exif.jpg");
            var destination = Path.Combine(_fileSystem.CachePath, "1", "small.jpg");
            await SampleImageHelper.CreateSampleJpeg(source, 800, 600, SKColors.Red, SKColors.Blue, new SampleExif(
                "Make", "Model", "Lens", 24, 2.8, 100, 100, new DateTime(2025, 1, 1)));

            // Act
            await _helper.GenerateThumbnails(source, [new ThumbnailTarget(destination, 400)]);

            // Assert
            var metadata = await _helper.ReadMetadata(destination);
            Assert.That(metadata.CameraModel, Is.Null);
            Assert.That(metadata.DateTaken, Is.Null);
        }

        [TestCase((ushort)3, 300, 200)]
        [TestCase((ushort)6, 200, 300)]
        [TestCase((ushort)8, 200, 300)]
        public async Task GenerateThumbnails_ShouldApplyOrientation(ushort orientation, int expectedWidth, int expectedHeight)
        {
            // Arrange
            var source = Path.Combine(_fileSystem.LibraryPath, "rotated.jpg");
            var destination = Path.Combine(_fileSystem.CachePath, "1", "small.jpg");
            await SampleImageHelper.CreateSampleJpeg(source, 300, 200, SKColors.Red, SKColors.Blue, new SampleExif(
                "Make", "Model", "Lens", 24, 2.8, 100, 100, new DateTime(2025, 1, 1), orientation));

            // Act
            await _helper.GenerateThumbnails(source, [new ThumbnailTarget(destination, 1024)]);

            // Assert
            using var codec = SKCodec.Create(destination);
            Assert.That(codec.Info.Width, Is.EqualTo(expectedWidth));
            Assert.That(codec.Info.Height, Is.EqualTo(expectedHeight));
        }

        [Test]
        public async Task ReadMetadata_WithCorruptFile_ShouldThrow()
        {
            // Arrange
            var path = Path.Combine(_fileSystem.LibraryPath, "corrupt.jpg");
            File.WriteAllText(path, "nope");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidDataException>(async () => await _helper.ReadMetadata(path));
        }
    }
}
