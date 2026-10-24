using Microsoft.EntityFrameworkCore;
using ShutterStream.Domain.Database.Context;
using ShutterStream.Domain.DTOs.Albums;
using ShutterStream.Domain.Extensions;
using ShutterStream.Domain.Interfaces.Api.Albums;
using ShutterStream.Domain.Results;

namespace ShutterStream.Domain.Services.Albums
{
    public class AlbumsService(AppDbContext context) : IAlbumsService
    {
        public async Task<GetAlbumsDto> GetAlbums()
        {
            var albums = await context.Albums
                .Where(a => a.IsPublic)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Description,
                    a.AlbumDate,
                    PhotoCount = a.Photos.Count(p => !p.IsHidden),
                    // Fall back to the first visible photo if no cover is set or the cover has been hidden
                    CoverPhotoId = a.CoverPhoto != null && !a.CoverPhoto.IsHidden
                        ? a.CoverPhotoId
                        : a.Photos
                            .Where(p => !p.IsHidden)
                            .OrderBy(p => p.DateTaken ?? p.FileModifiedAt)
                            .ThenBy(p => p.FileName)
                            .Select(p => (int?)p.Id)
                            .FirstOrDefault()
                })
                .Where(a => a.PhotoCount > 0)
                .OrderByDescending(a => a.AlbumDate.HasValue)
                .ThenByDescending(a => a.AlbumDate)
                .ThenBy(a => a.Title)
                .ToListAsync();

            var coverPhotoIds = albums.Where(a => a.CoverPhotoId != null).Select(a => a.CoverPhotoId!.Value).ToList();
            var coverVersions = await context.Photos
                .Where(p => coverPhotoIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.FileModifiedAt);

            return new GetAlbumsDto
            {
                Albums = albums.Select(a => new AlbumSummaryItem
                {
                    AlbumId = a.Id,
                    Title = a.Title,
                    Description = a.Description,
                    PhotoCount = a.PhotoCount,
                    AlbumDate = a.AlbumDate,
                    CoverPhotoId = a.CoverPhotoId,
                    CoverPhotoVersion = a.CoverPhotoId != null && coverVersions.TryGetValue(a.CoverPhotoId.Value, out var modified)
                        ? modified.ToImageVersion()
                        : null
                }).ToArray()
            };
        }

        public async Task<Result<GetAlbumDto>> GetAlbum(int albumId, bool includePrivate)
        {
            var album = await context.Albums.FirstOrDefaultAsync(a => a.Id == albumId);

            // Private albums return the same error as missing ones so their existence isn't leaked
            if (album == null || (!album.IsPublic && !includePrivate))
            {
                return Result.NotFoundFail<GetAlbumDto>("Album not found");
            }

            var photos = await context.Photos
                .Where(p => p.AlbumId == albumId && !p.IsHidden)
                .OrderForDisplay()
                .Select(p => new { p.Id, p.Width, p.Height, p.FileModifiedAt, p.Title, p.SubFolder })
                .ToListAsync();

            return Result.Ok(new GetAlbumDto
            {
                AlbumId = album.Id,
                Title = album.Title,
                Description = album.Description,
                IsPublic = album.IsPublic,
                AlbumDate = album.AlbumDate,
                CoverPhotoId = photos.Any(p => p.Id == album.CoverPhotoId) ? album.CoverPhotoId : photos.FirstOrDefault()?.Id,
                Photos = photos.Select(p => new AlbumPhotoItem
                {
                    PhotoId = p.Id,
                    Width = p.Width,
                    Height = p.Height,
                    Version = p.FileModifiedAt.ToImageVersion(),
                    Title = p.Title,
                    SubFolder = p.SubFolder
                }).ToArray()
            });
        }
    }
}
