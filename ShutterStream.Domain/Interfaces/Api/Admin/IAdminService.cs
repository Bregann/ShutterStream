using ShutterStream.Domain.DTOs.Admin.Requests;
using ShutterStream.Domain.DTOs.Admin.Responses;

namespace ShutterStream.Domain.Interfaces.Api.Admin
{
    public interface IAdminService
    {
        Task<GetLibraryStatusDto> GetLibraryStatus();
        void QueueLibraryScan();
        Task<GetAdminAlbumsDto> GetAdminAlbums();
        Task<GetAdminAlbumDto> GetAdminAlbum(int albumId);
        Task UpdateAlbumDetails(UpdateAlbumDetailsRequest request);
        Task SetAlbumVisibility(SetAlbumVisibilityRequest request);
        Task SetAlbumCover(SetAlbumCoverRequest request);
        Task SetPhotoVisibility(SetPhotoVisibilityRequest request);
        Task QueueAlbumCaptions(GenerateAlbumCaptionsRequest request);
        Task UpdatePhotoCaption(UpdatePhotoCaptionRequest request);
    }
}
