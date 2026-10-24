using Microsoft.EntityFrameworkCore;
using Serilog;
using ShutterStream.Domain.Database.Context;
using ShutterStream.Domain.Database.Models;
using ShutterStream.Domain.DTOs.Library;
using ShutterStream.Domain.Enums;
using ShutterStream.Domain.Interfaces.Api.Library;
using ShutterStream.Domain.Interfaces.Helpers;
using System.Collections.Concurrent;

namespace ShutterStream.Domain.Services.Library
{
    public class LibraryScanService(
        AppDbContext context,
        IPhotoLibraryHelper libraryHelper,
        IImageProcessingHelper imageProcessingHelper,
        ILibraryScanStatusHelper scanStatusHelper) : ILibraryScanService
    {
        // Thumbnails are generated and saved in batches so a restart mid scan doesn't lose all the work
        private const int ThumbnailBatchSize = 50;

        public async Task<LibraryScanResult?> ScanLibrary()
        {
            if (!scanStatusHelper.TryStartScan())
            {
                Log.Information("Library scan requested but one is already running");
                return null;
            }

            var result = new LibraryScanResult();

            try
            {
                Log.Information($"Starting library scan of {libraryHelper.LibraryRootPath}");

                await SyncAlbums(result);
                await GenerateMissingThumbnails(result);

                var message = $"Albums +{result.AlbumsAdded}/-{result.AlbumsRemoved}, " +
                    $"photos +{result.PhotosAdded}/~{result.PhotosUpdated}/-{result.PhotosRemoved}, " +
                    $"{result.ThumbnailsGenerated} thumbnails generated" +
                    (result.PhotosFailed > 0 ? $", {result.PhotosFailed} files could not be read" : "");

                Log.Information($"Library scan complete: {message}");
                scanStatusHelper.CompleteScan(message);

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Library scan failed");
                scanStatusHelper.CompleteScan($"Scan failed: {ex.Message}");
                throw;
            }
        }

        private async Task SyncAlbums(LibraryScanResult result)
        {
            var rootPath = libraryHelper.LibraryRootPath;

            if (!Directory.Exists(rootPath))
            {
                throw new DirectoryNotFoundException($"Photo library folder {rootPath} does not exist");
            }

            var folderNames = Directory.GetDirectories(rootPath)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name) && !name.StartsWith('.'))
                .Select(name => name!)
                .ToHashSet(StringComparer.Ordinal);

            var albums = await context.Albums.ToListAsync();

            // An empty library almost always means a network drive has dropped rather than every
            // album being deleted, so refuse to wipe the albums and their published settings
            if (folderNames.Count == 0 && albums.Count > 0)
            {
                throw new InvalidOperationException("The photo library folder is empty. Refusing to remove every album - check the library drive is mounted");
            }

            foreach (var album in albums.Where(a => !folderNames.Contains(a.FolderName)).ToList())
            {
                await RemoveAlbum(album);
                albums.Remove(album);
                result.AlbumsRemoved++;
            }

            var existingFolderNames = albums.Select(a => a.FolderName).ToHashSet(StringComparer.Ordinal);

            foreach (var folderName in folderNames.Where(f => !existingFolderNames.Contains(f)))
            {
                var album = new Album
                {
                    FolderName = folderName,
                    Title = folderName,
                    IsPublic = false
                };

                context.Albums.Add(album);
                albums.Add(album);
                result.AlbumsAdded++;

                Log.Information($"New album found: {folderName}");
            }

            await context.SaveChangesAsync();

            foreach (var album in albums)
            {
                await SyncAlbumPhotos(album, result);
            }
        }

        private async Task RemoveAlbum(Album album)
        {
            var photoIds = await context.Photos.Where(p => p.AlbumId == album.Id).Select(p => p.Id).ToListAsync();

            // Clear the cover first so EF doesn't trip over the album <-> photo cycle when deleting
            album.CoverPhotoId = null;
            await context.SaveChangesAsync();

            context.Albums.Remove(album);
            await context.SaveChangesAsync();

            foreach (var photoId in photoIds)
            {
                libraryHelper.DeleteThumbnails(photoId);
            }

            Log.Information($"Album folder {album.FolderName} no longer exists, removed album {album.Id} and {photoIds.Count} photos");
        }

        private async Task SyncAlbumPhotos(Album album, LibraryScanResult result)
        {
            var albumFolderPath = libraryHelper.GetAlbumFolderPath(album.FolderName);

            // Sub folders (e.g. one per day of a holiday) are part of the same album. Hidden folders
            // are skipped the same way hidden files are
            var files = new DirectoryInfo(albumFolderPath)
                .EnumerateFiles("*", new EnumerationOptions { RecurseSubdirectories = true })
                .Select(f => (File: f, SubFolder: GetSubFolder(albumFolderPath, f)))
                .Where(f => libraryHelper.IsSupportedImageFile(f.File.Name) && !f.SubFolder.Split('/').Any(part => part.StartsWith('.')))
                .ToDictionary(f => PhotoKey(f.SubFolder, f.File.Name), f => f.File, StringComparer.Ordinal);

            var existingPhotos = await context.Photos.Where(p => p.AlbumId == album.Id).ToListAsync();
            var currentPhotos = new List<Photo>();

            foreach (var photo in existingPhotos)
            {
                if (!files.TryGetValue(PhotoKey(photo.SubFolder, photo.FileName), out var file))
                {
                    if (album.CoverPhotoId == photo.Id)
                    {
                        album.CoverPhotoId = null;
                    }

                    context.Photos.Remove(photo);
                    libraryHelper.DeleteThumbnails(photo.Id);
                    result.PhotosRemoved++;
                    continue;
                }

                currentPhotos.Add(photo);

                // Postgres stores timestamps to the microsecond, so allow some slack rather than an exact match
                var hasChanged = file.Length != photo.FileSizeBytes
                    || Math.Abs((file.LastWriteTimeUtc - photo.FileModifiedAt).TotalSeconds) > 1;

                if (!hasChanged)
                {
                    continue;
                }

                var metadata = await TryReadMetadata(file.FullName, result);

                if (metadata == null)
                {
                    continue;
                }

                ApplyMetadata(photo, metadata);
                photo.FileSizeBytes = file.Length;
                photo.FileModifiedAt = file.LastWriteTimeUtc;
                photo.ThumbnailsGenerated = false;
                libraryHelper.DeleteThumbnails(photo.Id);
                result.PhotosUpdated++;
            }

            var existingKeys = existingPhotos.Select(p => PhotoKey(p.SubFolder, p.FileName)).ToHashSet(StringComparer.Ordinal);

            foreach (var file in files.Where(f => !existingKeys.Contains(f.Key)).Select(f => f.Value))
            {
                var metadata = await TryReadMetadata(file.FullName, result);

                if (metadata == null)
                {
                    continue;
                }

                var photo = new Photo
                {
                    AlbumId = album.Id,
                    SubFolder = GetSubFolder(albumFolderPath, file),
                    FileName = file.Name,
                    FileSizeBytes = file.Length,
                    FileModifiedAt = file.LastWriteTimeUtc,
                    Width = metadata.Width,
                    Height = metadata.Height
                };

                ApplyMetadata(photo, metadata);
                context.Photos.Add(photo);
                currentPhotos.Add(photo);
                result.PhotosAdded++;
            }

            album.AlbumDate = currentPhotos.Count > 0
                ? currentPhotos.Max(p => p.DateTaken ?? p.FileModifiedAt)
                : null;

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// The file's folder relative to the album folder, always "/" separated so it is the same on
        /// every OS. Empty for files directly in the album folder.
        /// </summary>
        private static string GetSubFolder(string albumFolderPath, FileInfo file)
        {
            var relative = Path.GetRelativePath(albumFolderPath, file.DirectoryName!);
            return relative == "." ? "" : relative.Replace('\\', '/');
        }

        private static string PhotoKey(string subFolder, string fileName) => $"{subFolder}/{fileName}";

        private async Task<PhotoMetadata?> TryReadMetadata(string filePath, LibraryScanResult result)
        {
            try
            {
                return await imageProcessingHelper.ReadMetadata(filePath);
            }
            catch (Exception ex)
            {
                // A single corrupt file shouldn't stop the rest of the library from scanning
                Log.Warning(ex, $"Could not read image {filePath}, skipping");
                result.PhotosFailed++;
                return null;
            }
        }

        private static void ApplyMetadata(Photo photo, PhotoMetadata metadata)
        {
            photo.Width = metadata.Width;
            photo.Height = metadata.Height;
            photo.DateTaken = metadata.DateTaken;
            photo.CameraMake = metadata.CameraMake;
            photo.CameraModel = metadata.CameraModel;
            photo.LensModel = metadata.LensModel;
            photo.FocalLength = metadata.FocalLength;
            photo.Aperture = metadata.Aperture;
            photo.ExposureTime = metadata.ExposureTime;
            photo.Iso = metadata.Iso;
        }

        private async Task GenerateMissingThumbnails(LibraryScanResult result)
        {
            var pendingPhotos = await context.Photos
                .Where(p => !p.ThumbnailsGenerated)
                .OrderBy(p => p.Id)
                .Select(p => new { p.Id, p.SubFolder, p.FileName, p.Album.FolderName })
                .ToListAsync();

            if (pendingPhotos.Count == 0)
            {
                return;
            }

            Log.Information($"Generating thumbnails for {pendingPhotos.Count} photos");

            var processed = 0;
            scanStatusHelper.SetProgress(0, pendingPhotos.Count);

            // Image decoding is CPU and memory heavy, so leave headroom for serving requests
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) };

            foreach (var batch in pendingPhotos.Chunk(ThumbnailBatchSize))
            {
                var generatedIds = new ConcurrentBag<int>();

                await Parallel.ForEachAsync(batch, parallelOptions, async (photo, _) =>
                {
                    try
                    {
                        var targets = Enum.GetValues<PhotoSize>()
                            .Select(size => new ThumbnailTarget(libraryHelper.GetThumbnailPath(photo.Id, size), libraryHelper.GetMaxDimension(size)))
                            .ToList();

                        await imageProcessingHelper.GenerateThumbnails(libraryHelper.GetPhotoFilePath(photo.FolderName, photo.SubFolder, photo.FileName), targets);
                        generatedIds.Add(photo.Id);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, $"Failed to generate thumbnails for photo {photo.Id} ({photo.FolderName}/{photo.FileName})");
                    }

                    scanStatusHelper.SetProgress(Interlocked.Increment(ref processed), pendingPhotos.Count);
                });

                var ids = generatedIds.ToList();

                await context.Photos
                    .Where(p => ids.Contains(p.Id))
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.ThumbnailsGenerated, true));

                // ExecuteUpdate bypasses the change tracker, so any tracked photos now hold a stale
                // ThumbnailsGenerated value. Clear them so a later scan on this context reloads from the database
                context.ChangeTracker.Clear();

                result.ThumbnailsGenerated += ids.Count;
            }
        }
    }
}
