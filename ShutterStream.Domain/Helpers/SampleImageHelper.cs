using SkiaSharp;
using System.Buffers.Binary;
using System.Text;

namespace ShutterStream.Domain.Helpers
{
    public record SampleExif(
        string Make,
        string Model,
        string LensModel,
        uint FocalLength,
        double Aperture,
        uint ExposureDenominator,
        ushort Iso,
        DateTime DateTaken,
        ushort Orientation = 1);

    /// <summary>
    /// Generates gradient JPEGs with real EXIF data, used for the debug sample library and the tests.
    /// SkiaSharp can't write EXIF, so a minimal EXIF APP1 segment is built by hand and inserted after the JPEG SOI marker.
    /// </summary>
    public static class SampleImageHelper
    {
        private const ushort TypeAscii = 2;
        private const ushort TypeShort = 3;
        private const ushort TypeLong = 4;
        private const ushort TypeRational = 5;

        private record IfdEntry(ushort Tag, ushort Type, uint Count, byte[] Data);

        public static async Task CreateSampleJpeg(string filePath, int width, int height, SKColor fromColour, SKColor toColour, SampleExif? exif = null)
        {
            byte[] jpegBytes;

            using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
            {
                using (var paint = new SKPaint())
                {
                    paint.Shader = SKShader.CreateLinearGradient(
                        new SKPoint(0, 0),
                        new SKPoint(width, height),
                        [fromColour, toColour],
                        SKShaderTileMode.Clamp);

                    surface.Canvas.DrawRect(0, 0, width, height, paint);
                }

                using var image = surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
                jpegBytes = data.ToArray();
            }

            if (exif != null)
            {
                jpegBytes = InsertExif(jpegBytes, BuildExifTiff(exif));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllBytesAsync(filePath, jpegBytes);
        }

        private static byte[] InsertExif(byte[] jpegBytes, byte[] tiff)
        {
            var header = Encoding.ASCII.GetBytes("Exif\0\0");
            var segmentLength = 2 + header.Length + tiff.Length;

            var app1 = new List<byte> { 0xFF, 0xE1, (byte)(segmentLength >> 8), (byte)(segmentLength & 0xFF) };
            app1.AddRange(header);
            app1.AddRange(tiff);

            // Keep the SOI marker (FF D8) first, then the EXIF segment, then the rest of the file
            return [.. jpegBytes[..2], .. app1, .. jpegBytes[2..]];
        }

        private static byte[] BuildExifTiff(SampleExif exif)
        {
            var exifEntries = new List<IfdEntry>
            {
                Rational(0x829A, 1, exif.ExposureDenominator),
                Rational(0x829D, (uint)Math.Round(exif.Aperture * 10), 10),
                Short(0x8827, exif.Iso),
                Ascii(0x9003, exif.DateTaken.ToString("yyyy:MM:dd HH:mm:ss")),
                Rational(0x920A, exif.FocalLength, 1),
                Ascii(0xA434, exif.LensModel)
            };

            // The IFD0 size doesn't depend on the pointer value, so build once to find where the EXIF IFD starts
            List<IfdEntry> BuildIfd0(uint exifOffset) =>
            [
                Ascii(0x010F, exif.Make),
                Ascii(0x0110, exif.Model),
                Short(0x0112, exif.Orientation),
                Long(0x8769, exifOffset)
            ];

            const int ifd0Offset = 8;
            var exifOffset = (uint)(ifd0Offset + WriteIfd(BuildIfd0(0), ifd0Offset).Length);
            var ifd0Bytes = WriteIfd(BuildIfd0(exifOffset), ifd0Offset);
            var exifBytes = WriteIfd(exifEntries, (int)exifOffset);

            // Big endian TIFF header: "MM", 42, offset of the first IFD
            byte[] tiffHeader = [(byte)'M', (byte)'M', 0x00, 0x2A, 0x00, 0x00, 0x00, ifd0Offset];

            return [.. tiffHeader, .. ifd0Bytes, .. exifBytes];
        }

        private static byte[] WriteIfd(IReadOnlyList<IfdEntry> entries, int ifdOffset)
        {
            var dataOffset = ifdOffset + 2 + (entries.Count * 12) + 4;
            var ifd = new List<byte>();
            var data = new List<byte>();

            AddUInt16(ifd, (ushort)entries.Count);

            foreach (var entry in entries.OrderBy(e => e.Tag))
            {
                AddUInt16(ifd, entry.Tag);
                AddUInt16(ifd, entry.Type);
                AddUInt32(ifd, entry.Count);

                // Values of 4 bytes or fewer live inside the entry, anything larger goes in the data area
                if (entry.Data.Length <= 4)
                {
                    ifd.AddRange(entry.Data);
                    ifd.AddRange(new byte[4 - entry.Data.Length]);
                }
                else
                {
                    AddUInt32(ifd, (uint)(dataOffset + data.Count));
                    data.AddRange(entry.Data);

                    if (data.Count % 2 == 1)
                    {
                        data.Add(0);
                    }
                }
            }

            // No further IFDs
            AddUInt32(ifd, 0);

            return [.. ifd, .. data];
        }

        private static IfdEntry Ascii(ushort tag, string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value + "\0");
            return new IfdEntry(tag, TypeAscii, (uint)bytes.Length, bytes);
        }

        private static IfdEntry Short(ushort tag, ushort value)
        {
            var bytes = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
            return new IfdEntry(tag, TypeShort, 1, bytes);
        }

        private static IfdEntry Long(ushort tag, uint value)
        {
            var bytes = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
            return new IfdEntry(tag, TypeLong, 1, bytes);
        }

        private static IfdEntry Rational(ushort tag, uint numerator, uint denominator)
        {
            var bytes = new byte[8];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, numerator);
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(4), denominator);
            return new IfdEntry(tag, TypeRational, 1, bytes);
        }

        private static void AddUInt16(List<byte> bytes, ushort value)
        {
            bytes.Add((byte)(value >> 8));
            bytes.Add((byte)(value & 0xFF));
        }

        private static void AddUInt32(List<byte> bytes, uint value)
        {
            bytes.Add((byte)(value >> 24));
            bytes.Add((byte)((value >> 16) & 0xFF));
            bytes.Add((byte)((value >> 8) & 0xFF));
            bytes.Add((byte)(value & 0xFF));
        }
    }
}
