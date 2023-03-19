using BreganUtils;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ShutterStream.Infrastructure.Database.Context;
using ShutterStream.Infrastructure.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShutterStream.Domain.Dtos.AuthDto;

namespace ShutterStream.Domain.Data
{
    public class AuthData
    {
        public static LoginUserDto ValidateUserLogin(string username, string password)
        {
            using (var context = new DatabaseContext())
            {
                //Get the user
                var user = context.Users.FirstOrDefault(x => x.Username == username);

                if (user == null)
                {
                    Log.Information($"[User Login] Attempted login with username {username} failed due to user not existing");

                    return new LoginUserDto
                    {
                        Successful = false
                    };
                }

                //Validate if the password is correct
                var isMatch = BCrypt.Net.BCrypt.Verify(password, user.HashedPassword);

                if (!isMatch)
                {
                    Log.Information($"[User Login] Attempted login with username {username} failed due to incorrect password");

                    return new LoginUserDto
                    {
                        Successful = false
                    };
                }

                var sessionId = $"D{DateTime.UtcNow.Ticks / 730}G{Guid.NewGuid()}";

                //Create a new session id and add it into the database
                context.Sessions.Add(new Sessions
                {
                    SessionId = sessionId,
                    User = user
                });

                context.SaveChanges();
                Log.Information($"[User Login] Attempted login with username {username} successful");

                return new LoginUserDto
                {
                    Successful = true,
                    SessionId = sessionId,
                    Username = username
                };
            }
        }
        public static RegisterUserDto RegisterNewUser(string username, string password, string email)
        {
            if (!GeneralDataHelper.IsValidEmailAddress(email))
            {
                return new RegisterUserDto
                {
                    Success = false,
                    Reason = "Invalid email address"
                };
            }

            using(var context = new DatabaseContext())
            {
                if (context.Users.Any(x => x.Username == username))
                {
                    return new RegisterUserDto
                    {
                        Success = false,
                        Reason = "Username already exists"
                    };
                }

                if (context.Users.Any(x => x.Email == email))
                {
                    return new RegisterUserDto
                    {
                        Success = false,
                        Reason = "User with email already exists"
                    };
                }

                //hash the password and store it
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

                context.Users.Add(new Users
                {
                    Username = username,
                    HashedPassword = hashedPassword,
                    LastActivity = DateTime.UtcNow,
                    CanUploadFiles = false,
                    Email = email
                });

                context.SaveChanges();

                Log.Information($"[Registration] New user registered - {username}");

                return new RegisterUserDto 
                { 
                    Success = true 
                };
            }
        }
        public static void DeleteUserSession(string sessionId)
        {
            using (var context = new DatabaseContext())
            {
                context.Sessions.Where(x => x.SessionId == sessionId).ExecuteDelete();
                context.SaveChanges();

                Log.Information($"[Logout User] Session {sessionId} deleted");
            }
        }
    }
}
