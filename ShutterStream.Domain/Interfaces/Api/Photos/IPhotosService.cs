using ShutterStream.Domain.DTOs.Photos;
using ShutterStream.Domain.Enums;

namespace ShutterStream.Domain.Interfaces.Api.Photos
{
    public interface IPhotosService
    {
        Task<GetPhotoDto> GetPhoto(int photoId, bool includePrivate);
        Task<PhotoImageResult> GetPhotoImage(int photoId, PhotoSize size, bool includePrivate);
    }
}
