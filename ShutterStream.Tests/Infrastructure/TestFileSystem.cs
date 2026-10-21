namespace ShutterStream.Tests.Infrastructure
{
    /// <summary>
    /// A throwaway library and cache folder for tests that touch the disk. Call Delete in teardown.
    /// </summary>
    public sealed class TestFileSystem
    {
        public string RootPath { get; }
        public string LibraryPath { get; }
        public string CachePath { get; }

        public TestFileSystem()
        {
            RootPath = Path.Combine(Path.GetTempPath(), "shutterstream-tests", Guid.NewGuid().ToString("N"));
            LibraryPath = Path.Combine(RootPath, "library");
            CachePath = Path.Combine(RootPath, "cache");

            Directory.CreateDirectory(LibraryPath);
            Directory.CreateDirectory(CachePath);
        }

        public void Delete()
        {
            try
            {
                Directory.Delete(RootPath, true);
            }
            catch (IOException)
            {
                // Ignore cleanup errors
            }
        }
    }
}
