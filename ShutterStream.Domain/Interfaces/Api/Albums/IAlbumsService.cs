using ShutterStream.Domain.DTOs.Albums;

namespace ShutterStream.Domain.Interfaces.Api.Albums
{
    public interface IAlbumsService
    {
        Task<GetAlbumsDto> GetAlbums();
        Task<GetAlbumDto> GetAlbum(int albumId, bool includePrivate);
    }
}
