namespace ShutterStream.Domain.DTOs.Photos
{
    public class PhotoImageResult
    {
        public required string FilePath { get; set; }
        public required DateTime LastModified { get; set; }

        /// <summary>
        /// Whether anonymous visitors can see this photo. Used to decide how aggressively it can be cached.
        /// </summary>
        public required bool IsPubliclyVisible { get; set; }
    }
}
