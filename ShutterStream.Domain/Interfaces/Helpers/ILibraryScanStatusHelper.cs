using ShutterStream.Domain.DTOs.Library;

namespace ShutterStream.Domain.Interfaces.Helpers
{
    public interface ILibraryScanStatusHelper
    {
        bool TryStartScan();
        void SetProgress(int processed, int total);
        void CompleteScan(string message);
        LibraryScanStatus GetStatus();
    }
}
