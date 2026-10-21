using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using ShutterStream.Domain.DTOs.Library;
using ShutterStream.Domain.Interfaces.Helpers;
using SkiaSharp;
using System.Globalization;

namespace ShutterStream.Domain.Helpers
{
    public class ImageProcessingHelper : IImageProcessingHelper
    {
        private const int JpegQuality = 85;

        public Task<PhotoMetadata> ReadMetadata(string filePath)
        {
            // SKCodec only reads the file header here, so this is cheap even for large photos
            using var codec = SKCodec.Create(filePath) ?? throw new InvalidDataException($"Unsupported or corrupt image {filePath}");

            var width = codec.Info.Width;
            var height = codec.Info.Height;

            // These orientations are rotated 90 degrees, so the displayed dimensions are swapped
            if (IsRotated90(codec.EncodedOrigin))
            {
                (width, height) = (height, width);
            }

            var metadata = new PhotoMetadata
            {
                Width = width,
                Height = height
            };

            // GPS directories are deliberately never read
            var directories = ImageMetadataReader.ReadMetadata(filePath);
            var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();

            if (ifd0 != null)
            {
                metadata.CameraMake = CleanString(ifd0.GetString(ExifDirectoryBase.TagMake));
                metadata.CameraModel = CleanString(ifd0.GetString(ExifDirectoryBase.TagModel));
            }

            if (subIfd == null)
            {
                return Task.FromResult(metadata);
            }

            metadata.LensModel = CleanString(subIfd.GetString(ExifDirectoryBase.TagLensModel));

            if (subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dateTaken))
            {
                // Wall clock time with no timezone, flagged as UTC so Postgres accepts it
                metadata.DateTaken = DateTime.SpecifyKind(dateTaken, DateTimeKind.Utc);
            }

            if (subIfd.TryGetRational(ExifDirectoryBase.TagFocalLength, out var focalLength) && focalLength.Denominator != 0)
            {
                metadata.FocalLength = Math.Round(focalLength.ToDouble(), 1);
            }

            if (subIfd.TryGetRational(ExifDirectoryBase.TagFNumber, out var fNumber) && fNumber.Denominator != 0)
            {
                metadata.Aperture = Math.Round(fNumber.ToDouble(), 1);
            }

            if (subIfd.TryGetRational(ExifDirectoryBase.TagExposureTime, out var exposureTime) && exposureTime.Denominator != 0)
            {
                metadata.ExposureTime = FormatExposureTime(exposureTime.ToDouble());
            }

            if (subIfd.TryGetInt32(ExifDirectoryBase.TagIsoEquivalent, out var iso))
            {
                metadata.Iso = iso;
            }

            return Task.FromResult(metadata);
        }

        public async Task GenerateThumbnails(string sourcePath, IReadOnlyList<ThumbnailTarget> targets)
        {
            if (targets.Count == 0)
            {
                return;
            }

            using var codec = SKCodec.Create(sourcePath) ?? throw new InvalidDataException($"Unsupported or corrupt image {sourcePath}");

            var largestDimension = targets.Max(t => t.MaxDimension);

            // Encoded images are re-encoded from raw pixels, so EXIF (camera serials, GPS) is
            // never copied into the generated files
            using var decoded = DecodeAtLeast(codec, largestDimension);
            using var oriented = ApplyOrigin(decoded, codec.EncodedOrigin);

            foreach (var target in targets)
            {
                var scale = Math.Min(1f, (float)target.MaxDimension / Math.Max(oriented.Width, oriented.Height));
                var resizedInfo = new SKImageInfo(
                    Math.Max(1, (int)Math.Round(oriented.Width * scale)),
                    Math.Max(1, (int)Math.Round(oriented.Height * scale)));

                using var resized = scale < 1f
                    ? oriented.Resize(resizedInfo, new SKSamplingOptions(SKCubicResampler.Mitchell))
                    : oriented.Copy();

                using var image = SKImage.FromBitmap(resized);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);

                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(target.DestinationPath)!);

                // Write to a temp file then move it into place so a half written
                // thumbnail is never served if two requests generate it at once
                var tempPath = $"{target.DestinationPath}.{Guid.NewGuid():N}.tmp";

                await using (var fileStream = File.Create(tempPath))
                {
                    data.SaveTo(fileStream);
                }

                File.Move(tempPath, target.DestinationPath, true);
            }
        }

        /// <summary>
        /// Lets the JPEG decoder downscale (by 1/2, 1/4 or 1/8) while decoding, which is far quicker and
        /// uses far less memory than decoding at full resolution, as long as the result is still big enough.
        /// </summary>
        private static SKBitmap DecodeAtLeast(SKCodec codec, int minimumLongEdge)
        {
            var info = codec.Info;
            var desiredScale = Math.Min(1f, (float)minimumLongEdge / Math.Max(info.Width, info.Height));
            var scaledSize = codec.GetScaledDimensions(desiredScale);

            var decodeSize = Math.Max(scaledSize.Width, scaledSize.Height) >= Math.Min(minimumLongEdge, Math.Max(info.Width, info.Height))
                ? scaledSize
                : new SKSizeI(info.Width, info.Height);

            var decodeInfo = new SKImageInfo(decodeSize.Width, decodeSize.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            var bitmap = new SKBitmap(decodeInfo);
            var result = codec.GetPixels(decodeInfo, bitmap.GetPixels());

            if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput)
            {
                bitmap.Dispose();
                throw new InvalidDataException($"Failed to decode image: {result}");
            }

            return bitmap;
        }

        /// <summary>
        /// Rotates/flips the pixels to match the EXIF orientation so every browser displays them the right way up.
        /// </summary>
        private static SKBitmap ApplyOrigin(SKBitmap bitmap, SKEncodedOrigin origin)
        {
            if (origin == SKEncodedOrigin.TopLeft)
            {
                return bitmap.Copy();
            }

            var width = bitmap.Width;
            var height = bitmap.Height;
            var rotated = IsRotated90(origin);

            var result = new SKBitmap(rotated ? height : width, rotated ? width : height, bitmap.ColorType, bitmap.AlphaType);

            using (var canvas = new SKCanvas(result))
            {
                switch (origin)
                {
                    case SKEncodedOrigin.TopRight:
                        canvas.Translate(width, 0);
                        canvas.Scale(-1, 1);
                        break;
                    case SKEncodedOrigin.BottomRight:
                        canvas.Translate(width, height);
                        canvas.RotateDegrees(180);
                        break;
                    case SKEncodedOrigin.BottomLeft:
                        canvas.Translate(0, height);
                        canvas.Scale(1, -1);
                        break;
                    case SKEncodedOrigin.LeftTop:
                        canvas.RotateDegrees(90);
                        canvas.Scale(1, -1);
                        break;
                    case SKEncodedOrigin.RightTop:
                        canvas.Translate(height, 0);
                        canvas.RotateDegrees(90);
                        break;
                    case SKEncodedOrigin.RightBottom:
                        canvas.Translate(height, width);
                        canvas.RotateDegrees(90);
                        canvas.Scale(-1, 1);
                        break;
                    case SKEncodedOrigin.LeftBottom:
                        canvas.Translate(0, width);
                        canvas.RotateDegrees(-90);
                        break;
                }

                using var image = SKImage.FromBitmap(bitmap);
                canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest));
            }

            return result;
        }

        private static bool IsRotated90(SKEncodedOrigin origin)
        {
            return origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        }

        private static string? CleanString(string? value)
        {
            if (value == null)
            {
                return null;
            }

            var trimmed = value.Trim('\0', ' ');
            return trimmed == "" ? null : trimmed;
        }

        private static string FormatExposureTime(double seconds)
        {
            if (seconds <= 0)
            {
                return "0";
            }

            if (seconds < 1)
            {
                return $"1/{Math.Round(1 / seconds)}";
            }

            return Math.Round(seconds, 1).ToString(CultureInfo.InvariantCulture);
        }
    }
}
