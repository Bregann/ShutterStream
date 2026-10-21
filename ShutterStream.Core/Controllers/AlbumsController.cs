using Microsoft.AspNetCore.Mvc;
using ShutterStream.Core.Extensions;
using ShutterStream.Domain.DTOs.Albums;
using ShutterStream.Domain.Interfaces.Api.Albums;

namespace ShutterStream.Core.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AlbumsController(IAlbumsService albumsService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<GetAlbumsDto>> GetAlbums()
        {
            var albums = await albumsService.GetAlbums();
            return Ok(albums);
        }

        [HttpGet]
        public async Task<ActionResult<GetAlbumDto>> GetAlbum([FromQuery] int albumId)
        {
            // The admin can preview private albums, everyone else only sees public ones
            var result = await albumsService.GetAlbum(albumId, User.Identity?.IsAuthenticated == true);
            return result.ToActionResult();
        }
    }
}
