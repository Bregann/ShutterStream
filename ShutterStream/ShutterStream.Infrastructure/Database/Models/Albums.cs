using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShutterStream.Infrastructure.Database.Models
{
    public class Albums
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string AlbumName { get; set; }
        public required string? Location { get; set; }
        public required long Likes { get; set; }
        public required long Views { get; set; }
        public required DateTime CreatedDate { get; set; }
        public required Users User { get; set; }
        public List<Images> Images { get; set; }
    }
}
