using Microsoft.AspNetCore.Mvc;
using static ShutterStream.Domain.Dtos.AuthDto;
using ShutterStream.Domain.Data.Auth;
using ShutterStream.Api.Dtos.Auth;

namespace ShutterStream.Api.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        [HttpPost("LoginUser")]
        public ActionResult<LoginUserDto> LoginUser([FromBody] LoginUserRequestDto dto)
        {
            var loginData = AuthData.ValidateUserLogin(dto.Username.ToLower(), dto.Password);

            if (!loginData.Successful)
            {
                return Unauthorized();
            }

            return Ok(loginData);
        }

        [HttpPost("RegisterNewUser")]
        public RegisterUserDto RegisterNewUserAsync([FromBody] RegisterNewUserRequestDto dto)
        {
            return AuthData.RegisterNewUser(dto.Username.ToLower(), dto.Password, dto.Email.ToLower());
        }

        [HttpPost("UpdateSessionExpireTime")]
        public ActionResult UpdateSessionExpireTime([FromBody] long exp)
        {
            if (Request.Headers["Authorization"].Count == 0)
            {
                return BadRequest();
            }

            var success = AuthData.UpdateSessionExpireTime(Request.Headers["Authorization"]!, exp);

            if (!success)
            {
                return BadRequest();
            }

            return Ok();
        }

        [HttpDelete("DeleteUserSession")]
        public ActionResult DeleteUserSession()
        {
            if (Request.Headers["Authorization"].Count == 0)
            {
                return BadRequest();
            }

            AuthData.DeleteUserSession(Request.Headers["Authorization"]!);
            return Ok();
        }
    }
}
