namespace ShutterStream.Domain.DTOs.Admin.Requests
{
    public class GenerateAlbumCaptionsRequest
    {
        public required int AlbumId { get; set; }

        /// <summary>
        /// Re-caption photos the AI has already captioned. Hand edited captions are never overwritten.
        /// </summary>
        public bool RegenerateExisting { get; set; } = false;
    }
}
