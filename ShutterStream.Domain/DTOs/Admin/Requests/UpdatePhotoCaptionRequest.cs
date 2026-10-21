using System.ComponentModel.DataAnnotations;

namespace ShutterStream.Domain.DTOs.Admin.Requests
{
    public class UpdatePhotoCaptionRequest
    {
        public required int PhotoId { get; set; }
        [MaxLength(200)]
        public required string Title { get; set; }

        [MaxLength(2000)]
        public required string Description { get; set; }
    }
}
