using ShutterStream.Domain.DTOs.Auth.Requests;
using ShutterStream.Domain.DTOs.Auth.Responses;
using ShutterStream.Domain.Results;

namespace ShutterStream.Domain.Interfaces.Api.Auth
{
    public interface IAuthService
    {
        Task<Result<LoginUserResponse>> LoginUser(LoginUserRequest request);
        Task<Result<LoginUserResponse>> RefreshToken(string refreshToken);
        Task DeleteUserSession(string refreshToken);
    }
}
