using ShutterStream.Domain.DTOs.Albums;
using ShutterStream.Domain.Results;

namespace ShutterStream.Domain.Interfaces.Api.Albums
{
    public interface IAlbumsService
    {
        Task<GetAlbumsDto> GetAlbums();
        Task<Result<GetAlbumDto>> GetAlbum(int albumId, bool includePrivate);
    }
}
