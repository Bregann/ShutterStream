using ShutterStream.Domain.DTOs.Captions;
using ShutterStream.Domain.Interfaces.Helpers;

namespace ShutterStream.Domain.Helpers
{
    /// <summary>
    /// Registered as a singleton. Tracks the running caption job in memory for the admin page's progress bar,
    /// and doubles as the lock that allows one caption job at a time so API usage stays predictable.
    /// </summary>
    public class CaptionJobStatusHelper : ICaptionJobStatusHelper
    {
        private readonly Lock _lock = new();
        private int? _runningAlbumId;
        private int _processed;
        private int _failed;
        private int _total;
        private int? _lastAlbumId;
        private DateTime? _lastCompletedAt;
        private string? _lastMessage;

        public bool TryStartJob(int albumId)
        {
            lock (_lock)
            {
                if (_runningAlbumId != null)
                {
                    return false;
                }

                _runningAlbumId = albumId;
                _processed = 0;
                _failed = 0;
                _total = 0;
                return true;
            }
        }

        public void SetProgress(int processed, int failed, int total)
        {
            lock (_lock)
            {
                _processed = processed;
                _failed = failed;
                _total = total;
            }
        }

        public void CompleteJob(string message)
        {
            lock (_lock)
            {
                _lastAlbumId = _runningAlbumId;
                _runningAlbumId = null;
                _lastCompletedAt = DateTime.UtcNow;
                _lastMessage = message;
            }
        }

        public CaptionJobStatus GetStatus()
        {
            lock (_lock)
            {
                return new CaptionJobStatus(_runningAlbumId, _processed, _failed, _total, _lastAlbumId, _lastCompletedAt, _lastMessage);
            }
        }
    }
}
