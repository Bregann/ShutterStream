namespace ShutterStream.Domain.DTOs.Admin.Requests
{
    public class SetPhotoVisibilityRequest
    {
        public required int PhotoId { get; set; }
        public required bool IsHidden { get; set; }
    }
}
