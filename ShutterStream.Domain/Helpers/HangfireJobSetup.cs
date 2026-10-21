using Hangfire;
using ShutterStream.Domain.Interfaces.Api.Library;

namespace ShutterStream.Domain.Helpers
{
    public class HangfireJobSetup
    {
        public static void SetupRecurringJobs()
        {
            // An unchanged library only costs a directory listing, so scanning hourly is cheap
            RecurringJob.AddOrUpdate<ILibraryScanService>("ScanPhotoLibrary", service => service.ScanLibrary(), Cron.Hourly);
        }
    }
}
