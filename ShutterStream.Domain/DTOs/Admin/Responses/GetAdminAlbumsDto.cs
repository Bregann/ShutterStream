namespace ShutterStream.Domain.DTOs.Admin.Responses
{
    public class GetAdminAlbumsDto
    {
        public required AdminAlbumItem[] Albums { get; set; }
    }

    public class AdminAlbumItem
    {
        public required int AlbumId { get; set; }
        public required string FolderName { get; set; }
        public required string Title { get; set; }
        public required bool IsPublic { get; set; }
        public required int PhotoCount { get; set; }
        public required int HiddenPhotoCount { get; set; }
        public int? CoverPhotoId { get; set; }
        public long? CoverPhotoVersion { get; set; }
        public DateTime? AlbumDate { get; set; }
    }
}
