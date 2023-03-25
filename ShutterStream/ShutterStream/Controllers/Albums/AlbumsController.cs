using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShutterStream.Api.Dtos.Albums;
using ShutterStream.Domain.ControllerData.Album;
using ShutterStream.Domain.Helpers;

namespace ShutterStream.Api.Controllers.Albums
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlbumsController : ControllerBase
    {
        [HttpPost("CreateNewAlbum")] 
        public async Task<ActionResult<int>> CreateNewAlbum([FromBody] CreateNewAlbumDto dto)
        {
            if (dto.AlbumName.Length > 50)
            {
                return BadRequest();
            }

            if (dto.Location != null && dto.Location.Length > 100)
            {
                return BadRequest();
            }

            var username = AuthHelper.ValidateSessionIdAndReturnUsername(Request.Headers);

            if (username == null)
            {
                return Unauthorized();
            }

            var newAlbumId = await AlbumData.CreateNewAlbumForUser(username, dto.AlbumName, dto.Location);

            if (newAlbumId == null)
            {
                return Unauthorized();
            }

            return newAlbumId;
        }
    }
}
