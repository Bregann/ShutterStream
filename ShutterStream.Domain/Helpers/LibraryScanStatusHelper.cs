using ShutterStream.Domain.DTOs.Library;
using ShutterStream.Domain.Interfaces.Helpers;

namespace ShutterStream.Domain.Helpers
{
    /// <summary>
    /// Registered as a singleton. Tracks the running scan in memory so the admin page can show
    /// progress, and doubles as the lock that stops two scans running at the same time.
    /// </summary>
    public class LibraryScanStatusHelper : ILibraryScanStatusHelper
    {
        private readonly Lock _lock = new();
        private bool _isRunning;
        private int _processed;
        private int _total;
        private DateTime? _lastStartedAt;
        private DateTime? _lastCompletedAt;
        private string? _lastMessage;

        public bool TryStartScan()
        {
            lock (_lock)
            {
                if (_isRunning)
                {
                    return false;
                }

                _isRunning = true;
                _processed = 0;
                _total = 0;
                _lastStartedAt = DateTime.UtcNow;
                return true;
            }
        }

        public void SetProgress(int processed, int total)
        {
            lock (_lock)
            {
                _processed = processed;
                _total = total;
            }
        }

        public void CompleteScan(string message)
        {
            lock (_lock)
            {
                _isRunning = false;
                _lastCompletedAt = DateTime.UtcNow;
                _lastMessage = message;
            }
        }

        public LibraryScanStatus GetStatus()
        {
            lock (_lock)
            {
                return new LibraryScanStatus(_isRunning, _processed, _total, _lastStartedAt, _lastCompletedAt, _lastMessage);
            }
        }
    }
}
