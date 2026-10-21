using ShutterStream.Domain.DTOs.Library;

namespace ShutterStream.Domain.Interfaces.Helpers
{
    public interface IImageProcessingHelper
    {
        Task<PhotoMetadata> ReadMetadata(string filePath);
        Task GenerateThumbnails(string sourcePath, IReadOnlyList<ThumbnailTarget> targets);
    }
}
