namespace ShutterStream.Domain.DTOs.Library
{
    public class LibraryScanResult
    {
        public int AlbumsAdded { get; set; }
        public int AlbumsRemoved { get; set; }
        public int PhotosAdded { get; set; }
        public int PhotosUpdated { get; set; }
        public int PhotosRemoved { get; set; }
        public int PhotosFailed { get; set; }
        public int ThumbnailsGenerated { get; set; }
    }
}
