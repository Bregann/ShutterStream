using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ShutterStream.Api.Controllers.Albums
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlbumsController : ControllerBase
    {
        [HttpPost("UploadNewAlbum")]
        public void UploadNewAlbum([FromForm]IFormFile file)
        {

        }

    }
}
