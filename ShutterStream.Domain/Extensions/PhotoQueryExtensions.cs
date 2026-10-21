using ShutterStream.Domain.Database.Models;

namespace ShutterStream.Domain.Extensions
{
    public static class PhotoQueryExtensions
    {
        /// <summary>
        /// The single source of truth for the order photos are shown in, so the album grid,
        /// the photo page next/previous buttons and the admin all agree.
        /// </summary>
        public static IOrderedQueryable<Photo> OrderForDisplay(this IQueryable<Photo> photos)
        {
            return photos
                .OrderBy(p => p.DateTaken ?? p.FileModifiedAt)
                .ThenBy(p => p.FileName);
        }
    }
}
