using ShutterStream.Domain.DTOs.Photos;
using ShutterStream.Domain.Enums;
using ShutterStream.Domain.Results;

namespace ShutterStream.Domain.Interfaces.Api.Photos
{
    public interface IPhotosService
    {
        Task<Result<GetPhotoDto>> GetPhoto(int photoId, bool includePrivate);
        Task<Result<PhotoImageResult>> GetPhotoImage(int photoId, PhotoSize size, bool includePrivate);
        Task<Result> RecordPhotoView(int photoId);
    }
}
