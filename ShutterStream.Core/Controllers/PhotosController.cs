using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using ShutterStream.Domain.DTOs.Photos;
using ShutterStream.Domain.Enums;
using ShutterStream.Domain.Exceptions;
using ShutterStream.Domain.Interfaces.Api.Photos;

namespace ShutterStream.Core.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PhotosController(IPhotosService photosService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<GetPhotoDto>> GetPhoto([FromQuery] int photoId)
        {
            var photo = await photosService.GetPhoto(photoId, User.Identity?.IsAuthenticated == true);
            return Ok(photo);
        }

        [HttpGet]
        public async Task<ActionResult> GetPhotoImage([FromQuery] int photoId, [FromQuery] PhotoSize size)
        {
            if (!Enum.IsDefined(size))
            {
                throw new BadRequestException("Unknown photo size");
            }

            var image = await photosService.GetPhotoImage(photoId, size, User.Identity?.IsAuthenticated == true);

            // Image urls carry a version from the file's modified time, so public images can be
            // cached forever. Images only the admin can see must never be held by a cache
            Response.Headers[HeaderNames.CacheControl] = image.IsPubliclyVisible
                ? "public, max-age=31536000, immutable"
                : "private, no-store";

            var lastModified = new DateTimeOffset(DateTime.SpecifyKind(image.LastModified, DateTimeKind.Utc));
            var entityTag = new EntityTagHeaderValue($"\"{photoId}-{(int)size}-{lastModified.ToUnixTimeSeconds()}\"");

            return PhysicalFile(image.FilePath, "image/jpeg", lastModified, entityTag);
        }
    }
}
