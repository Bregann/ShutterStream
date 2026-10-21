namespace ShutterStream.Domain.DTOs.Photos
{
    public class GetPhotoDto
    {
        public required int PhotoId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public required int AlbumId { get; set; }
        public required string AlbumTitle { get; set; }
        public required int Width { get; set; }
        public required int Height { get; set; }
        public required long Version { get; set; }
        public required int PositionInAlbum { get; set; }
        public required int AlbumPhotoCount { get; set; }
        public int? PreviousPhotoId { get; set; }
        public int? NextPhotoId { get; set; }
        public DateTime? DateTaken { get; set; }
        public string? CameraMake { get; set; }
        public string? CameraModel { get; set; }
        public string? LensModel { get; set; }
        public double? FocalLength { get; set; }
        public double? Aperture { get; set; }
        public string? ExposureTime { get; set; }
        public int? Iso { get; set; }
    }
}
