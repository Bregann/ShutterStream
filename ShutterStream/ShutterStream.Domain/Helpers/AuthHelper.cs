using ShutterStream.Infrastructure.Database.Context;
using Microsoft.AspNetCore.Http;

namespace ShutterStream.Domain.Helpers
{
    public class AuthHelper
    {
        public static string? ValidateSessionIdAndReturnUsername(IHeaderDictionary headers)
        {
            if (headers["Authorization"].Count == 0)
            {
                return null;
            }

            using (var context = new DatabaseContext())
            {
                string authHeader = headers["Authorization"]!;

                var user = context.Sessions.Where(x => x.SessionId == authHeader).Select(x => x.User).FirstOrDefault();

                if (user == null)
                {
                    return null;
                }
                else
                {
                    return user.Username;
                }
            }
        }

        public static bool CanUserUploadImages(string username)
        {
            using(var context = new DatabaseContext()) 
            {
                var user = context.Users.First(x => x.Username == username);

                if (!user.CanUploadFiles || user.ImageUploadsLeftForDay == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
    }
}
