using Microsoft.EntityFrameworkCore;
using Serilog;
using ShutterStream.Domain.Database.Context;
using ShutterStream.Domain.DTOs.Captions;
using ShutterStream.Domain.DTOs.Library;
using ShutterStream.Domain.Enums;
using ShutterStream.Domain.Extensions;
using ShutterStream.Domain.Interfaces.Api.Captions;
using ShutterStream.Domain.Interfaces.Helpers;
using System.Collections.Concurrent;

namespace ShutterStream.Domain.Services.Captions
{
    public class PhotoCaptionService(
        AppDbContext context,
        IPhotoLibraryHelper libraryHelper,
        IImageProcessingHelper imageProcessingHelper,
        IAiCaptionHelper aiCaptionHelper,
        ICaptionJobStatusHelper captionJobStatusHelper) : IPhotoCaptionService
    {
        // Captions are saved in batches so progress survives a restart part way through a big album
        private const int BatchSize = 8;

        // Kept low to stay well inside the API rate limits
        private const int MaxParallelRequests = 4;

        private record PendingPhoto(int Id, string SubFolder, string FileName, DateTime? DateTaken);

        public async Task GenerateAlbumCaptions(int albumId, bool regenerateExisting)
        {
            if (!captionJobStatusHelper.TryStartJob(albumId))
            {
                Log.Information($"Caption job for album {albumId} requested but another caption job is already running");
                return;
            }

            var processed = 0;
            var failed = 0;

            try
            {
                var album = await context.Albums.FirstOrDefaultAsync(a => a.Id == albumId)
                    ?? throw new InvalidOperationException($"Album {albumId} no longer exists");

                // A photo with a title but no generated timestamp was captioned by hand, so it is always left alone
                var pendingPhotos = await context.Photos
                    .Where(p => p.AlbumId == albumId && (p.Title == null || (regenerateExisting && p.CaptionGeneratedAt != null)))
                    .OrderForDisplay()
                    .Select(p => new PendingPhoto(p.Id, p.SubFolder, p.FileName, p.DateTaken))
                    .ToListAsync();

                captionJobStatusHelper.SetProgress(0, 0, pendingPhotos.Count);
                Log.Information($"Generating AI captions for {pendingPhotos.Count} photos in album {albumId} ({album.FolderName})");

                var captionContext = new PhotoCaptionContext { AlbumTitle = album.Title };
                var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = MaxParallelRequests };

                foreach (var batch in pendingPhotos.Chunk(BatchSize))
                {
                    var captions = new ConcurrentDictionary<int, PhotoCaption>();

                    await Parallel.ForEachAsync(batch, parallelOptions, async (photo, cancellationToken) =>
                    {
                        try
                        {
                            var imageBytes = await GetSmallImage(photo, album.FolderName);
                            var caption = await aiCaptionHelper.GenerateCaption(
                                imageBytes,
                                new PhotoCaptionContext { AlbumTitle = captionContext.AlbumTitle, DateTaken = photo.DateTaken },
                                cancellationToken);

                            if (caption == null)
                            {
                                Interlocked.Increment(ref failed);
                            }
                            else
                            {
                                captions[photo.Id] = caption;
                            }
                        }
                        catch (Exception ex)
                        {
                            // One bad photo or a transient API error shouldn't stop the rest of the album
                            Log.Warning(ex, $"Failed to caption photo {photo.Id} ({album.FolderName}/{photo.FileName})");
                            Interlocked.Increment(ref failed);
                        }

                        captionJobStatusHelper.SetProgress(Interlocked.Increment(ref processed), failed, pendingPhotos.Count);
                    });

                    await SaveCaptions(captions);
                }

                var message = $"Captioned {processed - failed} of {pendingPhotos.Count} photos" + (failed > 0 ? $", {failed} failed" : "");
                Log.Information($"Caption job for album {albumId} complete: {message}");
                captionJobStatusHelper.CompleteJob(message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Caption job for album {albumId} failed");
                captionJobStatusHelper.CompleteJob($"Caption job failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Uses the small thumbnail rather than the original: it's plenty for describing the photo,
        /// costs far fewer tokens, and has already had its EXIF and GPS data stripped.
        /// </summary>
        private async Task<byte[]> GetSmallImage(PendingPhoto photo, string folderName)
        {
            var thumbnailPath = libraryHelper.GetThumbnailPath(photo.Id, PhotoSize.Small);

            if (!File.Exists(thumbnailPath))
            {
                await imageProcessingHelper.GenerateThumbnails(
                    libraryHelper.GetPhotoFilePath(folderName, photo.SubFolder, photo.FileName),
                    [new ThumbnailTarget(thumbnailPath, libraryHelper.GetMaxDimension(PhotoSize.Small))]);
            }

            return await File.ReadAllBytesAsync(thumbnailPath);
        }

        private async Task SaveCaptions(ConcurrentDictionary<int, PhotoCaption> captions)
        {
            if (captions.IsEmpty)
            {
                return;
            }

            var ids = captions.Keys.ToList();
            var photos = await context.Photos.Where(p => ids.Contains(p.Id)).ToListAsync();
            var generatedAt = DateTime.UtcNow;

            foreach (var photo in photos)
            {
                // The admin may have edited this caption by hand while the job was running
                if (photo.Title != null && photo.CaptionGeneratedAt == null)
                {
                    continue;
                }

                photo.Title = captions[photo.Id].Title;
                photo.Description = captions[photo.Id].Description;
                photo.CaptionGeneratedAt = generatedAt;
            }

            await context.SaveChangesAsync();
        }
    }
}
