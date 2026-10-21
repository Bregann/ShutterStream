namespace ShutterStream.Domain.DTOs.Captions
{
    /// <summary>
    /// Hints given to the AI alongside the image. The album title usually comes from the folder name
    /// (e.g. "2025-01 Iceland") and is often the only clue to where the photo was taken.
    /// </summary>
    public class PhotoCaptionContext
    {
        public required string AlbumTitle { get; set; }
        public DateTime? DateTaken { get; set; }
    }
}
