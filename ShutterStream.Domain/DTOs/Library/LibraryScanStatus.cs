namespace ShutterStream.Domain.DTOs.Library
{
    public record LibraryScanStatus(
        bool IsRunning,
        int Processed,
        int Total,
        DateTime? LastStartedAt,
        DateTime? LastCompletedAt,
        string? LastMessage);
}
