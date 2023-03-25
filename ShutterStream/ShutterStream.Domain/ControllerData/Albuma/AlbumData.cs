using ShutterStream.Domain.Helpers;
using ShutterStream.Infrastructure.Database.Context;
using ShutterStream.Infrastructure.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShutterStream.Domain.ControllerData.Album
{
    public class AlbumData
    {
        public static async Task<int?> CreateNewAlbumForUser(string username, string albumName, string? location)
        {
            if (!AuthHelper.CanUserUploadImages(username))
            {
                return null;
            }

            using(var context = new DatabaseContext())
            {
                var user = context.Users.First(x => x.Username == username);

                var newAlbum = new Albums
                {
                    AlbumName = albumName,
                    Location = location,
                    CreatedDate = DateTime.UtcNow,
                    Likes = 0,
                    Views = 0,
                    User = user
                };

                context.Albums.Add(newAlbum);
                await context.SaveChangesAsync();

                return newAlbum.Id;
            }
        }
    }
}
