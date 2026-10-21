using ShutterStream.Domain.Enums;

namespace ShutterStream.Domain.Interfaces.Helpers
{
    public interface IPhotoLibraryHelper
    {
        string LibraryRootPath { get; }
        string GetAlbumFolderPath(string folderName);
        string GetPhotoFilePath(string folderName, string fileName);
        string GetThumbnailPath(int photoId, PhotoSize size);
        int GetMaxDimension(PhotoSize size);
        bool IsSupportedImageFile(string fileName);
        void DeleteThumbnails(int photoId);
    }
}
