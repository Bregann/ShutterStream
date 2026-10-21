namespace ShutterStream.Domain.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Converts a photo's file modified time into the version number appended to image urls,
        /// so browsers can cache images forever and still pick up a changed file.
        /// </summary>
        public static long ToImageVersion(this DateTime fileModifiedAt)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(fileModifiedAt, DateTimeKind.Utc)).ToUnixTimeSeconds();
        }
    }
}
