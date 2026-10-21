using ShutterStream.Domain.DTOs.Admin.Requests;
using ShutterStream.Domain.DTOs.Admin.Responses;
using ShutterStream.Domain.Results;

namespace ShutterStream.Domain.Interfaces.Api.Admin
{
    public interface IAdminService
    {
        Task<GetLibraryStatusDto> GetLibraryStatus();
        Result QueueLibraryScan();
        Task<GetAdminAlbumsDto> GetAdminAlbums();
        Task<Result<GetAdminAlbumDto>> GetAdminAlbum(int albumId);
        Task<Result> UpdateAlbumDetails(UpdateAlbumDetailsRequest request);
        Task<Result> SetAlbumVisibility(SetAlbumVisibilityRequest request);
        Task<Result> SetAlbumCover(SetAlbumCoverRequest request);
        Task<Result> SetPhotoVisibility(SetPhotoVisibilityRequest request);
        Task<Result> QueueAlbumCaptions(GenerateAlbumCaptionsRequest request);
        Task<Result> UpdatePhotoCaption(UpdatePhotoCaptionRequest request);
    }
}
