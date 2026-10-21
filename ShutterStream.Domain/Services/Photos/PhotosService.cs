using Microsoft.EntityFrameworkCore;
using Serilog;
using ShutterStream.Domain.Database.Context;
using ShutterStream.Domain.DTOs.Library;
using ShutterStream.Domain.DTOs.Photos;
using ShutterStream.Domain.Enums;
using ShutterStream.Domain.Exceptions;
using ShutterStream.Domain.Extensions;
using ShutterStream.Domain.Interfaces.Api.Photos;
using ShutterStream.Domain.Interfaces.Helpers;

namespace ShutterStream.Domain.Services.Photos
{
    public class PhotosService(AppDbContext context, IPhotoLibraryHelper libraryHelper, IImageProcessingHelper imageProcessingHelper) : IPhotosService
    {
        public async Task<GetPhotoDto> GetPhoto(int photoId, bool includePrivate)
        {
            var photo = await context.Photos.FirstOrDefaultAsync(p => p.Id == photoId);

            if (photo == null || (!IsPubliclyVisible(photo.Album.IsPublic, photo.IsHidden) && !includePrivate))
            {
                throw new NotFoundException("Photo not found");
            }

            // Hidden photos are left out of the sequence, apart from this one when the admin is previewing it
            var albumPhotoIds = await context.Photos
                .Where(p => p.AlbumId == photo.AlbumId && (!p.IsHidden || p.Id == photoId))
                .OrderForDisplay()
                .Select(p => p.Id)
                .ToListAsync();

            var index = albumPhotoIds.IndexOf(photoId);

            return new GetPhotoDto
            {
                PhotoId = photo.Id,
                Title = photo.Title,
                Description = photo.Description,
                AlbumId = photo.AlbumId,
                AlbumTitle = photo.Album.Title,
                Width = photo.Width,
                Height = photo.Height,
                Version = photo.FileModifiedAt.ToImageVersion(),
                PositionInAlbum = index + 1,
                AlbumPhotoCount = albumPhotoIds.Count,
                PreviousPhotoId = index > 0 ? albumPhotoIds[index - 1] : null,
                NextPhotoId = index < albumPhotoIds.Count - 1 ? albumPhotoIds[index + 1] : null,
                DateTaken = photo.DateTaken,
                CameraMake = photo.CameraMake,
                CameraModel = photo.CameraModel,
                LensModel = photo.LensModel,
                FocalLength = photo.FocalLength,
                Aperture = photo.Aperture,
                ExposureTime = photo.ExposureTime,
                Iso = photo.Iso
            };
        }

        public async Task<PhotoImageResult> GetPhotoImage(int photoId, PhotoSize size, bool includePrivate)
        {
            var photo = await context.Photos
                .Where(p => p.Id == photoId)
                .Select(p => new { p.Id, p.FileName, p.FileModifiedAt, p.IsHidden, p.Album.FolderName, AlbumIsPublic = p.Album.IsPublic })
                .FirstOrDefaultAsync();

            if (photo == null)
            {
                throw new NotFoundException("Photo not found");
            }

            var isPubliclyVisible = IsPubliclyVisible(photo.AlbumIsPublic, photo.IsHidden);

            if (!isPubliclyVisible && !includePrivate)
            {
                throw new NotFoundException("Photo not found");
            }

            var thumbnailPath = libraryHelper.GetThumbnailPath(photo.Id, size);

            // The library scan pre-generates thumbnails, but generate on demand if a request
            // arrives first (e.g. straight after a scan finds new photos, or the cache was cleared)
            if (!File.Exists(thumbnailPath))
            {
                var sourcePath = libraryHelper.GetPhotoFilePath(photo.FolderName, photo.FileName);

                if (!File.Exists(sourcePath))
                {
                    Log.Warning($"Source file missing for photo {photo.Id} at {sourcePath}");
                    throw new NotFoundException("Photo file not found");
                }

                var targets = Enum.GetValues<PhotoSize>()
                    .Select(s => new ThumbnailTarget(libraryHelper.GetThumbnailPath(photo.Id, s), libraryHelper.GetMaxDimension(s)))
                    .ToList();

                await imageProcessingHelper.GenerateThumbnails(sourcePath, targets);

                await context.Photos
                    .Where(p => p.Id == photo.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.ThumbnailsGenerated, true));
            }

            return new PhotoImageResult
            {
                FilePath = thumbnailPath,
                LastModified = photo.FileModifiedAt,
                IsPubliclyVisible = isPubliclyVisible
            };
        }

        private static bool IsPubliclyVisible(bool albumIsPublic, bool photoIsHidden)
        {
            return albumIsPublic && !photoIsHidden;
        }
    }
}
