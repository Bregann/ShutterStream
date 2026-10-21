using ShutterStream.Domain.DTOs.Captions;

namespace ShutterStream.Domain.Interfaces.Helpers
{
    public interface ICaptionJobStatusHelper
    {
        bool TryStartJob(int albumId);
        void SetProgress(int processed, int failed, int total);
        void CompleteJob(string message);
        CaptionJobStatus GetStatus();
    }
}
