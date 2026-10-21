namespace ShutterStream.Domain.DTOs.Admin.Requests
{
    public class SetAlbumCoverRequest
    {
        public required int AlbumId { get; set; }
        public required int PhotoId { get; set; }
    }
}
