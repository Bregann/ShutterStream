using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShutterStream.Core.Extensions;
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
            return adminService.QueueLibraryScan().ToActionResult();
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
            var result = await adminService.GetAdminAlbum(albumId);
            return result.ToActionResult();
        }

        [HttpPatch]
        public async Task<ActionResult> UpdateAlbumDetails([FromBody] UpdateAlbumDetailsRequest request)
        {
            var result = await adminService.UpdateAlbumDetails(request);
            return result.ToActionResult();
        }

        [HttpPatch]
        public async Task<ActionResult> SetAlbumVisibility([FromBody] SetAlbumVisibilityRequest request)
        {
            var result = await adminService.SetAlbumVisibility(request);
            return result.ToActionResult();
        }

        [HttpPatch]
        public async Task<ActionResult> SetAlbumCover([FromBody] SetAlbumCoverRequest request)
        {
            var result = await adminService.SetAlbumCover(request);
            return result.ToActionResult();
        }

        [HttpPatch]
        public async Task<ActionResult> SetPhotoVisibility([FromBody] SetPhotoVisibilityRequest request)
        {
            var result = await adminService.SetPhotoVisibility(request);
            return result.ToActionResult();
        }

        [HttpPost]
        public async Task<ActionResult> GenerateAlbumCaptions([FromBody] GenerateAlbumCaptionsRequest request)
        {
            var result = await adminService.QueueAlbumCaptions(request);
            return result.ToActionResult();
        }

        [HttpPatch]
        public async Task<ActionResult> UpdatePhotoCaption([FromBody] UpdatePhotoCaptionRequest request)
        {
            var result = await adminService.UpdatePhotoCaption(request);
            return result.ToActionResult();
        }
    }
}
