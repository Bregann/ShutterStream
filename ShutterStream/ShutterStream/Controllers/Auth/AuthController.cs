using Microsoft.AspNetCore.Mvc;
using ShutterStream.Domain.Data;
using ShutterStream.Api.Dtos;
using static ShutterStream.Domain.Dtos.AuthDto;

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
