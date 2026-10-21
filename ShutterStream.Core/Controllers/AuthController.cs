using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShutterStream.Domain.DTOs.Auth.Requests;
using ShutterStream.Domain.Exceptions;
using ShutterStream.Domain.Interfaces.Api.Auth;

namespace ShutterStream.Core.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost]
        [EnableRateLimiting(RateLimitPolicies.Login)]
        public async Task<ActionResult> LoginUser([FromBody] LoginUserRequest request)
        {
            var loginData = await authService.LoginUser(request);

            SetAuthCookies(loginData.AccessToken, loginData.RefreshToken);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
            {
                ClearAuthCookies();
                throw new UnauthorizedException("No refresh token provided");
            }

            try
            {
                var newTokens = await authService.RefreshToken(refreshToken);
                SetAuthCookies(newTokens.AccessToken, newTokens.RefreshToken);
                return Ok();
            }
            catch (UnauthorizedException)
            {
                ClearAuthCookies();
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUserSession([FromBody] DeleteUserSessionRequest request)
        {
            // The web proxy moves the HttpOnly refresh token cookie into the body for this request
            var refreshToken = request.RefreshToken ?? Request.Cookies["refreshToken"];

            if (!string.IsNullOrEmpty(refreshToken))
            {
                await authService.DeleteUserSession(refreshToken);
            }

            ClearAuthCookies();
            return Ok();
        }

        // Lax stops other sites sending these cookies with their requests (CSRF). The site and its
        // API proxy share one origin, so nothing here needs SameSite=None
        private void SetAuthCookies(string accessToken, string refreshToken)
        {
            Response.Cookies.Append("accessToken", accessToken, new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });

            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
        }

        private void ClearAuthCookies()
        {
            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");
        }
    }
}
