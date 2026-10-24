using ShutterStream.Domain.Database.Models;

namespace ShutterStream.Domain.Extensions
{
    public static class PhotoQueryExtensions
    {
        /// <summary>
        /// The single source of truth for the order photos are shown in, so the album grid,
        /// the photo page next/previous buttons and the admin all agree.
        ///
        /// Photos are kept together by sub folder: loose photos in the album folder first, then each
        /// sub folder in order of its earliest photo, so "Day 2" comes before "Day 10" without relying
        /// on how the folders are named. Within a folder photos run in the order they were taken.
        /// </summary>
        public static IOrderedQueryable<Photo> OrderForDisplay(this IQueryable<Photo> photos)
        {
            return photos
                .OrderBy(p => p.SubFolder != "")
                .ThenBy(p => p.Album.Photos
                    .Where(s => s.SubFolder == p.SubFolder)
                    .Min(s => s.DateTaken ?? s.FileModifiedAt))
                .ThenBy(p => p.SubFolder)
                .ThenBy(p => p.DateTaken ?? p.FileModifiedAt)
                .ThenBy(p => p.FileName);
        }
    }
}
