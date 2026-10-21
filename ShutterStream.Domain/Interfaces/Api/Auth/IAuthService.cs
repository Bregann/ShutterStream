using ShutterStream.Domain.DTOs.Auth.Requests;
using ShutterStream.Domain.DTOs.Auth.Responses;

namespace ShutterStream.Domain.Interfaces.Api.Auth
{
    public interface IAuthService
    {
        Task<LoginUserResponse> LoginUser(LoginUserRequest request);
        Task<LoginUserResponse> RefreshToken(string refreshToken);
        Task DeleteUserSession(string refreshToken);
    }
}
