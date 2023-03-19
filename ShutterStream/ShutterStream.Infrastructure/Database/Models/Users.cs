using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShutterStream.Infrastructure.Database.Models
{
    public class Users
    {
        [Key]
        public required string Username { get; set; }
        public required string HashedPassword { get; set; }
        public required string Email { get; set; }
        public required bool CanUploadFiles { get; set; }
        public required DateTime LastActivity { get; set; }
        public required List<Sessions> Sessions { get; set; }
        public required List<Albums> Albums { get; set; }
    }
}
