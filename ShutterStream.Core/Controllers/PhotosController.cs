using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using ShutterStream.Core.Extensions;
using ShutterStream.Domain.DTOs.Photos;
using ShutterStream.Domain.Enums;
using ShutterStream.Domain.Interfaces.Api.Photos;
using ShutterStream.Domain.Results;

namespace ShutterStream.Core.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PhotosController(IPhotosService photosService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<GetPhotoDto>> GetPhoto([FromQuery] int photoId)
        {
            var result = await photosService.GetPhoto(photoId, User.Identity?.IsAuthenticated == true);
            return result.ToActionResult();
        }

        [HttpGet]
        public async Task<ActionResult> GetPhotoImage([FromQuery] int photoId, [FromQuery] PhotoSize size)
        {
            if (!Enum.IsDefined(size))
            {
                return Result.Fail("Unknown photo size").ToErrorResponse();
            }

            var result = await photosService.GetPhotoImage(photoId, size, User.Identity?.IsAuthenticated == true);

            if (!result.Success)
            {
                return result.ToErrorResponse();
            }

            var image = result.Value;

            // Image urls carry a version from the file's modified time, so public images can be
            // cached forever. Images only the admin can see must never be held by a cache
            Response.Headers[HeaderNames.CacheControl] = image.IsPubliclyVisible
                ? "public, max-age=31536000, immutable"
                : "private, no-store";

            var lastModified = new DateTimeOffset(DateTime.SpecifyKind(image.LastModified, DateTimeKind.Utc));
            var entityTag = new EntityTagHeaderValue($"\"{photoId}-{(int)size}-{lastModified.ToUnixTimeSeconds()}\"");

            return PhysicalFile(image.FilePath, "image/jpeg", lastModified, entityTag);
        }

        [HttpPost]
        public async Task<ActionResult> RecordPhotoView([FromBody] RecordPhotoViewRequest request)
        {
            // The admin browsing their own photos shouldn't inflate the counts
            if (User.Identity?.IsAuthenticated == true)
            {
                return Ok();
            }

            var result = await photosService.RecordPhotoView(request.PhotoId);
            return result.ToActionResult();
        }
    }
}
