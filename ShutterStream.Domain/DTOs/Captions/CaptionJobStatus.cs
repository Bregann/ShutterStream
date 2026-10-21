namespace ShutterStream.Domain.DTOs.Captions
{
    public record CaptionJobStatus(
        int? RunningAlbumId,
        int Processed,
        int Failed,
        int Total,
        int? LastAlbumId,
        DateTime? LastCompletedAt,
        string? LastMessage);
}
