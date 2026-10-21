using Hangfire;

namespace ShutterStream.Domain.Interfaces.Api.Captions
{
    public interface IPhotoCaptionService
    {
        /// <summary>
        /// Captions every photo in the album that needs one. Hand edited captions are never overwritten.
        /// Not retried automatically, as every retry would be charged again.
        /// </summary>
        [AutomaticRetry(Attempts = 0)]
        Task GenerateAlbumCaptions(int albumId, bool regenerateExisting);
    }
}
