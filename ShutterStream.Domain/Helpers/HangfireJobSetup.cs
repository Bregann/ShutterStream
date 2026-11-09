using Hangfire;
using ShutterStream.Domain.Interfaces.Api.Library;
using ShutterStream.Domain.Interfaces.Helpers;

namespace ShutterStream.Domain.Helpers
{
    public class HangfireJobSetup
    {
        public static void SetupRecurringJobs()
        {
            // An unchanged library only costs a directory listing, so scanning hourly is cheap
            RecurringJob.AddOrUpdate<IProjectMonitorClient>("ProjectMonitorHeartbeat", client => client.SendHeartbeat(), Cron.Minutely);
            RecurringJob.AddOrUpdate<ILibraryScanService>("ScanPhotoLibrary", service => service.ScanLibrary(), Cron.Hourly);
        }
    }
}
