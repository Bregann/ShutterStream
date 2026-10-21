using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShutterStream.Domain.Database.Models
{
    /// <summary>
    /// An album maps 1:1 to a top level folder inside the photo library.
    /// </summary>
    [Index(nameof(FolderName), IsUnique = true)]
    public class Album
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// The folder name relative to the library root. This is the link between the
        /// database and the disk, so it is never edited from the admin interface.
        /// </summary>
        [Required]
        public required string FolderName { get; set; }

        [Required]
        public required string Title { get; set; }

        [Required]
        public string Description { get; set; } = "";

        /// <summary>
        /// Albums are private when first discovered and only become visible once published by the admin.
        /// </summary>
        [Required]
        public bool IsPublic { get; set; } = false;

        /// <summary>
        /// The most recent date a photo in the album was taken, used to order the albums.
        /// </summary>
        public DateTime? AlbumDate { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CoverPhotoId { get; set; }

        [ForeignKey(nameof(CoverPhotoId))]
        [DeleteBehavior(DeleteBehavior.SetNull)]
        public virtual Photo? CoverPhoto { get; set; }

        [InverseProperty(nameof(Photo.Album))]
        public virtual ICollection<Photo> Photos { get; set; } = null!;
    }
}
