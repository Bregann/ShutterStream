namespace ShutterStream.Api.Dtos
{
    public class RegisterNewUserRequestDto
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string Email { get; set; }
    }

    public class LoginUserRequestDto
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}
