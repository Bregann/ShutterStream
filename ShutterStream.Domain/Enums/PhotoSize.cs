namespace ShutterStream.Domain.Enums
{
    /// <summary>
    /// The resized variants generated for every photo. Originals are never served, every variant is
    /// re-encoded from the pixels, so any EXIF/GPS the camera recorded stays on disk only.
    /// </summary>
    public enum PhotoSize
    {
        Small = 0,
        Large = 1,

        /// <summary>
        /// The photo at its original resolution, for the full screen viewer and downloads. Too big to
        /// make for every photo, so it is only generated the first time someone asks for it.
        /// </summary>
        Full = 2
    }

    public static class PhotoSizes
    {
        /// <summary>
        /// The sizes the library scan generates up front. Full is left to be made on demand.
        /// </summary>
        public static readonly PhotoSize[] Pregenerated = [PhotoSize.Small, PhotoSize.Large];
    }
}
