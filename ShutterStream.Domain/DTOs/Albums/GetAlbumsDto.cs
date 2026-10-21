namespace ShutterStream.Domain.DTOs.Albums
{
    public class GetAlbumsDto
    {
        public required AlbumSummaryItem[] Albums { get; set; }
    }

    public class AlbumSummaryItem
    {
        public required int AlbumId { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required int PhotoCount { get; set; }
        public int? CoverPhotoId { get; set; }
        public long? CoverPhotoVersion { get; set; }
        public DateTime? AlbumDate { get; set; }
    }
}
