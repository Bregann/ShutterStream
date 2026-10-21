namespace ShutterStream.Domain.DTOs.Admin.Requests
{
    public class SetAlbumVisibilityRequest
    {
        public required int AlbumId { get; set; }
        public required bool IsPublic { get; set; }
    }
}
