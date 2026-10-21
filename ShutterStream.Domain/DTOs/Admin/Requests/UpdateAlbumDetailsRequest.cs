using System.ComponentModel.DataAnnotations;

namespace ShutterStream.Domain.DTOs.Admin.Requests
{
    public class UpdateAlbumDetailsRequest
    {
        public required int AlbumId { get; set; }
        [MaxLength(200)]
        public required string Title { get; set; }

        [MaxLength(5000)]
        public required string Description { get; set; }
    }
}
