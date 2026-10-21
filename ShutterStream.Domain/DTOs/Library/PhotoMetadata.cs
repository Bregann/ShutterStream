namespace ShutterStream.Domain.DTOs.Library
{
    /// <summary>
    /// Metadata read from an image file header. GPS data is deliberately never read.
    /// </summary>
    public class PhotoMetadata
    {
        public required int Width { get; set; }
        public required int Height { get; set; }
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
