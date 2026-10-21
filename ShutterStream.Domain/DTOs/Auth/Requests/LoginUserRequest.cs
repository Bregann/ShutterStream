using System.ComponentModel.DataAnnotations;

namespace ShutterStream.Domain.DTOs.Auth.Requests
{
    public class LoginUserRequest
    {
        [MaxLength(200)]
        public required string Username { get; set; }

        [MaxLength(200)]
        public required string Password { get; set; }
    }
}
