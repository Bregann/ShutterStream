namespace ShutterStream.Domain.DTOs.Admin.Responses
{
    public class GetAdminAlbumDto
    {
        public required int AlbumId { get; set; }
        public required string FolderName { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required bool IsPublic { get; set; }
        public int? CoverPhotoId { get; set; }
        public required AdminPhotoItem[] Photos { get; set; }
        public required AlbumCaptionStatus CaptionStatus { get; set; }
    }

    public class AlbumCaptionStatus
    {
        /// <summary>
        /// False when no Gemini API key is configured
        /// </summary>
        public required bool AiCaptionsEnabled { get; set; }
        public required bool IsRunning { get; set; }

        /// <summary>
        /// True when a caption job is running for a different album, which blocks starting one here
        /// </summary>
        public required bool IsOtherAlbumRunning { get; set; }
        public required int Processed { get; set; }
        public required int Failed { get; set; }
        public required int Total { get; set; }
        public DateTime? LastCompletedAt { get; set; }
        public string? LastMessage { get; set; }
    }

    public class AdminPhotoItem
    {
        public required int PhotoId { get; set; }
        public required string FileName { get; set; }
        public required int Width { get; set; }
        public required int Height { get; set; }
        public required long Version { get; set; }
        public required bool IsHidden { get; set; }
        public required int ViewCount { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }

        /// <summary>
        /// True when the current caption was written by the AI rather than by hand
        /// </summary>
        public required bool IsAiCaption { get; set; }
    }
}
