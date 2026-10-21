namespace ShutterStream.Domain.DTOs.Auth.Requests
{
    public class DeleteUserSessionRequest
    {
        public string? RefreshToken { get; set; }
    }
}
