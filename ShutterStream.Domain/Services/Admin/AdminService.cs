using Hangfire;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ShutterStream.Domain.Database.Context;
using ShutterStream.Domain.DTOs.Admin.Requests;
using ShutterStream.Domain.DTOs.Admin.Responses;
using ShutterStream.Domain.Exceptions;
using ShutterStream.Domain.Extensions;
using ShutterStream.Domain.Interfaces.Api.Admin;
using ShutterStream.Domain.Interfaces.Api.Captions;
using ShutterStream.Domain.Interfaces.Api.Library;
using ShutterStream.Domain.Interfaces.Helpers;

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

        public void QueueLibraryScan()
        {
            if (scanStatusHelper.GetStatus().IsRunning)
            {
                throw new ConflictException("A library scan is already running");
            }

            backgroundJobClient.Enqueue<ILibraryScanService>(service => service.ScanLibrary());
            Log.Information("Library scan queued from the admin interface");
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
                    CoverPhotoId = a.CoverPhotoId,
                    CoverPhotoVersion = a.CoverPhotoId != null && coverVersions.TryGetValue(a.CoverPhotoId.Value, out var modified)
                        ? modified.ToImageVersion()
                        : null
                }).ToArray()
            };
        }

        public async Task<GetAdminAlbumDto> GetAdminAlbum(int albumId)
        {
            var album = await context.Albums.FirstOrDefaultAsync(a => a.Id == albumId) ?? throw new NotFoundException("Album not found");

            var photos = await context.Photos
                .Where(p => p.AlbumId == albumId)
                .OrderForDisplay()
                .Select(p => new { p.Id, p.FileName, p.Width, p.Height, p.FileModifiedAt, p.IsHidden, p.Title, p.Description, p.CaptionGeneratedAt })
                .ToListAsync();

            return new GetAdminAlbumDto
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
                    FileName = p.FileName,
                    Width = p.Width,
                    Height = p.Height,
                    Version = p.FileModifiedAt.ToImageVersion(),
                    IsHidden = p.IsHidden,
                    Title = p.Title,
                    Description = p.Description,
                    IsAiCaption = p.CaptionGeneratedAt != null
                }).ToArray(),
                CaptionStatus = GetCaptionStatus(album.Id)
            };
        }

        public async Task UpdateAlbumDetails(UpdateAlbumDetailsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                throw new BadRequestException("Album title cannot be empty");
            }

            var album = await context.Albums.FindAsync(request.AlbumId) ?? throw new NotFoundException("Album not found");

            album.Title = request.Title.Trim();
            album.Description = request.Description.Trim();
            await context.SaveChangesAsync();

            Log.Information($"Album {album.Id} details updated");
        }

        public async Task SetAlbumVisibility(SetAlbumVisibilityRequest request)
        {
            var album = await context.Albums.FindAsync(request.AlbumId) ?? throw new NotFoundException("Album not found");

            album.IsPublic = request.IsPublic;
            await context.SaveChangesAsync();

            Log.Information($"Album {album.Id} ({album.FolderName}) is now {(request.IsPublic ? "public" : "private")}");
        }

        public async Task SetAlbumCover(SetAlbumCoverRequest request)
        {
            var album = await context.Albums.FindAsync(request.AlbumId) ?? throw new NotFoundException("Album not found");
            var photo = await context.Photos.FindAsync(request.PhotoId) ?? throw new NotFoundException("Photo not found");

            if (photo.AlbumId != album.Id)
            {
                throw new BadRequestException("The cover photo must be in the album");
            }

            album.CoverPhotoId = photo.Id;
            await context.SaveChangesAsync();

            Log.Information($"Album {album.Id} cover set to photo {photo.Id}");
        }

        public async Task SetPhotoVisibility(SetPhotoVisibilityRequest request)
        {
            var photo = await context.Photos.FindAsync(request.PhotoId) ?? throw new NotFoundException("Photo not found");

            photo.IsHidden = request.IsHidden;
            await context.SaveChangesAsync();

            Log.Information($"Photo {photo.Id} is now {(request.IsHidden ? "hidden" : "visible")}");
        }

        public async Task QueueAlbumCaptions(GenerateAlbumCaptionsRequest request)
        {
            if (!aiCaptionHelper.IsEnabled)
            {
                throw new BadRequestException("AI captions are not set up. Add a Gemini API key to the GeminiApiKey environmental setting");
            }

            if (captionJobStatusHelper.GetStatus().RunningAlbumId != null)
            {
                throw new ConflictException("A caption job is already running");
            }

            if (!await context.Albums.AnyAsync(a => a.Id == request.AlbumId))
            {
                throw new NotFoundException("Album not found");
            }

            backgroundJobClient.Enqueue<IPhotoCaptionService>(service => service.GenerateAlbumCaptions(request.AlbumId, request.RegenerateExisting));
            Log.Information($"Caption job queued for album {request.AlbumId} (regenerate existing: {request.RegenerateExisting})");
        }

        public async Task UpdatePhotoCaption(UpdatePhotoCaptionRequest request)
        {
            var photo = await context.Photos.FindAsync(request.PhotoId) ?? throw new NotFoundException("Photo not found");

            var title = request.Title.Trim();
            var description = request.Description.Trim();

            photo.Title = title == "" ? null : title;
            photo.Description = description == "" ? null : description;

            // Marks the caption as hand written so AI runs never overwrite it
            photo.CaptionGeneratedAt = null;

            await context.SaveChangesAsync();

            Log.Information($"Photo {photo.Id} caption edited by hand");
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
