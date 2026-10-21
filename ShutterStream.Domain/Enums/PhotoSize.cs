namespace ShutterStream.Domain.Enums
{
    /// <summary>
    /// The resized variants generated for every photo. Originals are never served
    /// so full resolution files (and any EXIF/GPS they contain) stay on disk only.
    /// </summary>
    public enum PhotoSize
    {
        Small = 0,
        Large = 1
    }
}
