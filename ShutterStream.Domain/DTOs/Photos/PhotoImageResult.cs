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

        /// <summary>
        /// The name to save the file as when it is downloaded
        /// </summary>
        public required string DownloadFileName { get; set; }
    }
}
