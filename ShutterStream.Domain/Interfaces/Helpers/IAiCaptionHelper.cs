using ShutterStream.Domain.DTOs.Captions;

namespace ShutterStream.Domain.Interfaces.Helpers
{
    public interface IAiCaptionHelper
    {
        /// <summary>
        /// False when no API key is configured, in which case the admin interface hides the AI features.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Returns null if the model declined to caption the photo.
        /// </summary>
        Task<PhotoCaption?> GenerateCaption(byte[] jpegBytes, PhotoCaptionContext context, CancellationToken cancellationToken = default);
    }
}
