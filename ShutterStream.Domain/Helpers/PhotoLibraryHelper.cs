using Serilog;
using ShutterStream.Domain.Enums;
using ShutterStream.Domain.Interfaces.Helpers;

namespace ShutterStream.Domain.Helpers
{
    /// <summary>
    /// Resolves every path used for photos. The library root is treated as read only,
    /// all generated images live under the cache root.
    /// </summary>
    public class PhotoLibraryHelper(string libraryRootPath, string cacheRootPath) : IPhotoLibraryHelper
    {
        // Formats SkiaSharp can decode. RAW, HEIC and TIFF files are skipped by the scanner
        private static readonly HashSet<string> _supportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp"
        };

        public string LibraryRootPath { get; } = libraryRootPath;

        public string GetAlbumFolderPath(string folderName)
        {
            return Path.Combine(LibraryRootPath, folderName);
        }

        public string GetPhotoFilePath(string folderName, string fileName)
        {
            return Path.Combine(LibraryRootPath, folderName, fileName);
        }

        public string GetThumbnailPath(int photoId, PhotoSize size)
        {
            return Path.Combine(cacheRootPath, photoId.ToString(), $"{size.ToString().ToLowerInvariant()}.jpg");
        }

        public int GetMaxDimension(PhotoSize size)
        {
            return size switch
            {
                PhotoSize.Small => 1024,
                PhotoSize.Large => 2048,
                _ => throw new ArgumentOutOfRangeException(nameof(size), size, "Unknown photo size")
            };
        }

        public bool IsSupportedImageFile(string fileName)
        {
            return !fileName.StartsWith('.') && _supportedExtensions.Contains(Path.GetExtension(fileName));
        }

        public void DeleteThumbnails(int photoId)
        {
            var thumbnailFolder = Path.Combine(cacheRootPath, photoId.ToString());

            if (!Directory.Exists(thumbnailFolder))
            {
                return;
            }

            try
            {
                Directory.Delete(thumbnailFolder, true);
            }
            catch (IOException ex)
            {
                Log.Warning(ex, $"Failed to delete thumbnails for photo {photoId}");
            }
        }
    }
}
