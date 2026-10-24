namespace ShutterStream.Domain.DTOs.Albums
{
    public class GetAlbumDto
    {
        public required int AlbumId { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required bool IsPublic { get; set; }
        public DateTime? AlbumDate { get; set; }
        public required AlbumPhotoItem[] Photos { get; set; }
    }

    public class AlbumPhotoItem
    {
        public required int PhotoId { get; set; }
        public required int Width { get; set; }
        public required int Height { get; set; }
        public required long Version { get; set; }
        public string? Title { get; set; }

        /// <summary>
        /// The section the photo is shown under, "" for photos directly in the album folder
        /// </summary>
        public required string SubFolder { get; set; }
    }
}
