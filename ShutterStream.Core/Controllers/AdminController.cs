using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShutterStream.Domain.DTOs.Admin.Requests;
using ShutterStream.Domain.DTOs.Admin.Responses;
using ShutterStream.Domain.Interfaces.Api.Admin;

namespace ShutterStream.Core.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class AdminController(IAdminService adminService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<GetLibraryStatusDto>> GetLibraryStatus()
        {
            var status = await adminService.GetLibraryStatus();
            return Ok(status);
        }

        [HttpPost]
        public ActionResult ScanLibrary()
        {
            adminService.QueueLibraryScan();
            return Ok();
        }

        [HttpGet]
        public async Task<ActionResult<GetAdminAlbumsDto>> GetAdminAlbums()
        {
            var albums = await adminService.GetAdminAlbums();
            return Ok(albums);
        }

        [HttpGet]
        public async Task<ActionResult<GetAdminAlbumDto>> GetAdminAlbum([FromQuery] int albumId)
        {
            var album = await adminService.GetAdminAlbum(albumId);
            return Ok(album);
        }

        [HttpPatch]
        public async Task<ActionResult> UpdateAlbumDetails([FromBody] UpdateAlbumDetailsRequest request)
        {
            await adminService.UpdateAlbumDetails(request);
            return Ok();
        }

        [HttpPatch]
        public async Task<ActionResult> SetAlbumVisibility([FromBody] SetAlbumVisibilityRequest request)
        {
            await adminService.SetAlbumVisibility(request);
            return Ok();
        }

        [HttpPatch]
        public async Task<ActionResult> SetAlbumCover([FromBody] SetAlbumCoverRequest request)
        {
            await adminService.SetAlbumCover(request);
            return Ok();
        }

        [HttpPatch]
        public async Task<ActionResult> SetPhotoVisibility([FromBody] SetPhotoVisibilityRequest request)
        {
            await adminService.SetPhotoVisibility(request);
            return Ok();
        }

        [HttpPost]
        public async Task<ActionResult> GenerateAlbumCaptions([FromBody] GenerateAlbumCaptionsRequest request)
        {
            await adminService.QueueAlbumCaptions(request);
            return Ok();
        }

        [HttpPatch]
        public async Task<ActionResult> UpdatePhotoCaption([FromBody] UpdatePhotoCaptionRequest request)
        {
            await adminService.UpdatePhotoCaption(request);
            return Ok();
        }
    }
}
