using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShutterStream.Infrastructure.Database.Models
{
    public class Images
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string ImageName { get; set; }
        public required string FilePath { get; set; }
        public required long Likes { get; set; }
        public required long Views { get; set; }
        public required DateTime UploadDate { get; set; }
        public required Albums Album { get; set; }
    }
}
