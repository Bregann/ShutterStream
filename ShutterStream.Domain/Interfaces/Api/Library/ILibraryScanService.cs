using Hangfire;
using ShutterStream.Domain.DTOs.Library;

namespace ShutterStream.Domain.Interfaces.Api.Library
{
    public interface ILibraryScanService
    {
        /// <summary>
        /// Syncs the database with the folders on disk and generates any missing thumbnails.
        /// Returns null if a scan was already running. Not retried, the next hourly run picks up anything missed.
        /// </summary>
        [AutomaticRetry(Attempts = 0)]
        Task<LibraryScanResult?> ScanLibrary();
    }
}
