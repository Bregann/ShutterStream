using Hangfire;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ShutterStream.Domain.Database.Context;
using ShutterStream.Domain.DTOs.Admin.Requests;
using ShutterStream.Domain.DTOs.Admin.Responses;
using ShutterStream.Domain.Extensions;
using ShutterStream.Domain.Interfaces.Api.Admin;
using ShutterStream.Domain.Interfaces.Api.Captions;
using ShutterStream.Domain.Interfaces.Api.Library;
using ShutterStream.Domain.Interfaces.Helpers;
using ShutterStream.Domain.Results;

namespace ShutterStream.Domain.Services.Admin
{
    public class AdminService(
        AppDbContext context,
        IPhotoLibraryHelper libraryHelper,
        ILibraryScanStatusHelper scanStatusHelper,
        IAiCaptionHelper aiCaptionHelper,
        ICaptionJobStatusHelper captionJobStatusHelper,
        IBackgroundJobClient backgroundJobClient) : IAdminService
    {
        public async Task<GetLibraryStatusDto> GetLibraryStatus()
        {
            var scanStatus = scanStatusHelper.GetStatus();

            return new GetLibraryStatusDto
            {
                LibraryPath = libraryHelper.LibraryRootPath,
                LibraryPathExists = Directory.Exists(libraryHelper.LibraryRootPath),
                TotalAlbums = await context.Albums.CountAsync(),
                PublicAlbums = await context.Albums.CountAsync(a => a.IsPublic),
                TotalPhotos = await context.Photos.CountAsync(),
                PendingThumbnails = await context.Photos.CountAsync(p => !p.ThumbnailsGenerated),
                IsScanRunning = scanStatus.IsRunning,
                ScanProcessed = scanStatus.Processed,
                ScanTotal = scanStatus.Total,
                LastScanStartedAt = scanStatus.LastStartedAt,
                LastScanCompletedAt = scanStatus.LastCompletedAt,
                LastScanMessage = scanStatus.LastMessage
            };
        }

        public Result QueueLibraryScan()
        {
            if (scanStatusHelper.GetStatus().IsRunning)
            {
                return Result.ConflictFail("A library scan is already running");
            }

            backgroundJobClient.Enqueue<ILibraryScanService>(service => service.ScanLibrary());
            Log.Information("Library scan queued from the admin interface");

            return Result.Ok();
        }

        public async Task<GetAdminAlbumsDto> GetAdminAlbums()
        {
            var albums = await context.Albums
                .OrderByDescending(a => a.AlbumDate.HasValue)
                .ThenByDescending(a => a.AlbumDate)
                .ThenBy(a => a.Title)
                .Select(a => new
                {
                    a.Id,
                    a.FolderName,
                    a.Title,
                    a.IsPublic,
                    a.AlbumDate,
                    PhotoCount = a.Photos.Count,
                    HiddenPhotoCount = a.Photos.Count(p => p.IsHidden),
                    ViewCount = a.Photos.Sum(p => p.ViewCount),
                    CoverPhotoId = a.CoverPhotoId ?? a.Photos
                        .OrderBy(p => p.DateTaken ?? p.FileModifiedAt)
                        .ThenBy(p => p.FileName)
                        .Select(p => (int?)p.Id)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var coverPhotoIds = albums.Where(a => a.CoverPhotoId != null).Select(a => a.CoverPhotoId!.Value).ToList();
            var coverVersions = await context.Photos
                .Where(p => coverPhotoIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.FileModifiedAt);

            return new GetAdminAlbumsDto
            {
                Albums = albums.Select(a => new AdminAlbumItem
                {
                    AlbumId = a.Id,
                    FolderName = a.FolderName,
                    Title = a.Title,
                    IsPublic = a.IsPublic,
                    AlbumDate = a.AlbumDate,
                    PhotoCount = a.PhotoCount,
                    HiddenPhotoCount = a.HiddenPhotoCount,
                    ViewCount = a.ViewCount,
                    CoverPhotoId = a.CoverPhotoId,
                    CoverPhotoVersion = a.CoverPhotoId != null && coverVersions.TryGetValue(a.CoverPhotoId.Value, out var modified)
                        ? modified.ToImageVersion()
                        : null
                }).ToArray()
            };
        }

        public async Task<Result<GetAdminAlbumDto>> GetAdminAlbum(int albumId)
        {
            var album = await context.Albums.FirstOrDefaultAsync(a => a.Id == albumId);

            if (album == null)
            {
                return Result.NotFoundFail<GetAdminAlbumDto>("Album not found");
            }

            var photos = await context.Photos
                .Where(p => p.AlbumId == albumId)
                .OrderForDisplay()
                .Select(p => new { p.Id, p.SubFolder, p.FileName, p.Width, p.Height, p.FileModifiedAt, p.IsHidden, p.ViewCount, p.Title, p.Description, p.CaptionGeneratedAt })
                .ToListAsync();

            return Result.Ok(new GetAdminAlbumDto
            {
                AlbumId = album.Id,
                FolderName = album.FolderName,
                Title = album.Title,
                Description = album.Description,
                IsPublic = album.IsPublic,
                CoverPhotoId = album.CoverPhotoId,
                Photos = photos.Select(p => new AdminPhotoItem
                {
                    PhotoId = p.Id,
                    SubFolder = p.SubFolder,
                    FileName = p.FileName,
                    Width = p.Width,
                    Height = p.Height,
                    Version = p.FileModifiedAt.ToImageVersion(),
                    IsHidden = p.IsHidden,
                    ViewCount = p.ViewCount,
                    Title = p.Title,
                    Description = p.Description,
                    IsAiCaption = p.CaptionGeneratedAt != null
                }).ToArray(),
                CaptionStatus = GetCaptionStatus(album.Id)
            });
        }

        public async Task<Result> UpdateAlbumDetails(UpdateAlbumDetailsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Result.Fail("Album title cannot be empty");
            }

            var album = await context.Albums.FindAsync(request.AlbumId);

            if (album == null)
            {
                return Result.NotFoundFail("Album not found");
            }

            album.Title = request.Title.Trim();
            album.Description = request.Description.Trim();
            await context.SaveChangesAsync();

            Log.Information($"Album {album.Id} details updated");

            return Result.Ok();
        }

        public async Task<Result> SetAlbumVisibility(SetAlbumVisibilityRequest request)
        {
            var album = await context.Albums.FindAsync(request.AlbumId);

            if (album == null)
            {
                return Result.NotFoundFail("Album not found");
            }

            album.IsPublic = request.IsPublic;
            await context.SaveChangesAsync();

            Log.Information($"Album {album.Id} ({album.FolderName}) is now {(request.IsPublic ? "public" : "private")}");

            return Result.Ok();
        }

        public async Task<Result> SetAlbumCover(SetAlbumCoverRequest request)
        {
            var album = await context.Albums.FindAsync(request.AlbumId);

            if (album == null)
            {
                return Result.NotFoundFail("Album not found");
            }

            var photo = await context.Photos.FindAsync(request.PhotoId);

            if (photo == null)
            {
                return Result.NotFoundFail("Photo not found");
            }

            if (photo.AlbumId != album.Id)
            {
                return Result.Fail("The cover photo must be in the album");
            }

            album.CoverPhotoId = photo.Id;
            await context.SaveChangesAsync();

            Log.Information($"Album {album.Id} cover set to photo {photo.Id}");

            return Result.Ok();
        }

        public async Task<Result> SetPhotoVisibility(SetPhotoVisibilityRequest request)
        {
            var photo = await context.Photos.FindAsync(request.PhotoId);

            if (photo == null)
            {
                return Result.NotFoundFail("Photo not found");
            }

            photo.IsHidden = request.IsHidden;
            await context.SaveChangesAsync();

            Log.Information($"Photo {photo.Id} is now {(request.IsHidden ? "hidden" : "visible")}");

            return Result.Ok();
        }

        public async Task<Result> QueueAlbumCaptions(GenerateAlbumCaptionsRequest request)
        {
            if (!aiCaptionHelper.IsEnabled)
            {
                return Result.Fail("AI captions are not set up. Add a Gemini API key to the GeminiApiKey environmental setting");
            }

            if (captionJobStatusHelper.GetStatus().RunningAlbumId != null)
            {
                return Result.ConflictFail("A caption job is already running");
            }

            if (!await context.Albums.AnyAsync(a => a.Id == request.AlbumId))
            {
                return Result.NotFoundFail("Album not found");
            }

            backgroundJobClient.Enqueue<IPhotoCaptionService>(service => service.GenerateAlbumCaptions(request.AlbumId, request.RegenerateExisting));
            Log.Information($"Caption job queued for album {request.AlbumId} (regenerate existing: {request.RegenerateExisting})");

            return Result.Ok();
        }

        public async Task<Result> UpdatePhotoCaption(UpdatePhotoCaptionRequest request)
        {
            var photo = await context.Photos.FindAsync(request.PhotoId);

            if (photo == null)
            {
                return Result.NotFoundFail("Photo not found");
            }

            var title = request.Title.Trim();
            var description = request.Description.Trim();

            photo.Title = title == "" ? null : title;
            photo.Description = description == "" ? null : description;

            // Marks the caption as hand written so AI runs never overwrite it
            photo.CaptionGeneratedAt = null;

            await context.SaveChangesAsync();

            Log.Information($"Photo {photo.Id} caption edited by hand");

            return Result.Ok();
        }

        private AlbumCaptionStatus GetCaptionStatus(int albumId)
        {
            var status = captionJobStatusHelper.GetStatus();
            var isRunning = status.RunningAlbumId == albumId;

            return new AlbumCaptionStatus
            {
                AiCaptionsEnabled = aiCaptionHelper.IsEnabled,
                IsRunning = isRunning,
                IsOtherAlbumRunning = status.RunningAlbumId != null && !isRunning,
                Processed = isRunning ? status.Processed : 0,
                Failed = isRunning ? status.Failed : 0,
                Total = isRunning ? status.Total : 0,
                LastCompletedAt = status.LastAlbumId == albumId ? status.LastCompletedAt : null,
                LastMessage = status.LastAlbumId == albumId ? status.LastMessage : null
            };
        }
    }
}
