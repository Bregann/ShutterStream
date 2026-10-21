namespace ShutterStream.Domain.DTOs.Admin.Responses
{
    public class GetLibraryStatusDto
    {
        public required string LibraryPath { get; set; }
        public required bool LibraryPathExists { get; set; }
        public required int TotalAlbums { get; set; }
        public required int PublicAlbums { get; set; }
        public required int TotalPhotos { get; set; }
        public required int PendingThumbnails { get; set; }
        public required bool IsScanRunning { get; set; }
        public required int ScanProcessed { get; set; }
        public required int ScanTotal { get; set; }
        public DateTime? LastScanStartedAt { get; set; }
        public DateTime? LastScanCompletedAt { get; set; }
        public string? LastScanMessage { get; set; }
    }
}
