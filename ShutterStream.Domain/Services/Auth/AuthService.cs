using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using ShutterStream.Domain.Database.Context;
using ShutterStream.Domain.Database.Models;
using ShutterStream.Domain.DTOs.Auth.Requests;
using ShutterStream.Domain.DTOs.Auth.Responses;
using ShutterStream.Domain.Interfaces.Api.Auth;
using ShutterStream.Domain.Results;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ShutterStream.Domain.Services.Auth
{
    public class AuthService(AppDbContext context) : IAuthService
    {
        private readonly PasswordHasher<User> _passwordHasher = new();

        // Checked against when the username doesn't exist, so a failed login takes the same time
        // either way and the response time can't be used to discover the username
        private static readonly string _dummyPasswordHash = new PasswordHasher<User>().HashPassword(new User(), Guid.NewGuid().ToString());

        public async Task<Result<LoginUserResponse>> LoginUser(LoginUserRequest request)
        {
            Log.Information($"Logging in user {request.Username}");

            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == request.Username.ToLower().Trim());

            // The same message is used for both failures so the endpoint can't be used to find the username
            if (user == null)
            {
                _passwordHasher.VerifyHashedPassword(new User(), _dummyPasswordHash, request.Password);
                Log.Information($"User not found {request.Username}");
                return Result.UnauthorizedFail<LoginUserResponse>("Invalid username or password");
            }

            if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            {
                Log.Information($"Invalid password for user {request.Username}");
                return Result.UnauthorizedFail<LoginUserResponse>("Invalid username or password");
            }

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            await SaveRefreshToken(refreshToken, user.Id);

            Log.Information($"User logged in {request.Username}");

            return Result.Ok(new LoginUserResponse
            {
                AccessToken = token,
                RefreshToken = refreshToken
            });
        }

        public async Task<Result<LoginUserResponse>> RefreshToken(string userRefreshToken)
        {
            // Never log the raw token - it is a credential
            var tokenFingerprint = Fingerprint(userRefreshToken);

            Log.Information("[Refresh] Attempting refresh for token {Fingerprint}", tokenFingerprint);
            var refreshToken = await context.UserRefreshTokens.FirstOrDefaultAsync(t => t.Token == userRefreshToken);

            if (refreshToken == null)
            {
                Log.Warning("[Refresh] REJECTED - token {Fingerprint} not found in database", tokenFingerprint);
                return Result.UnauthorizedFail<LoginUserResponse>("Token not found");
            }

            if (refreshToken.IsRevoked)
            {
                Log.Warning("[Refresh] REJECTED - token {Fingerprint} already revoked for user {UserId}", tokenFingerprint, refreshToken.UserId);
                return Result.UnauthorizedFail<LoginUserResponse>("Refresh token has been revoked");
            }

            if (refreshToken.ExpiresAt < DateTime.UtcNow)
            {
                Log.Warning("[Refresh] REJECTED - token {Fingerprint} expired at {ExpiresAt} for user {UserId}", tokenFingerprint, refreshToken.ExpiresAt, refreshToken.UserId);
                return Result.UnauthorizedFail<LoginUserResponse>("Refresh token expired");
            }

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == refreshToken.UserId);

            if (user == null)
            {
                Log.Information($"User not found for token {refreshToken.UserId}");
                return Result.UnauthorizedFail<LoginUserResponse>("User not found");
            }

            var token = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            // Revoking the old token and issuing the new one must be atomic, otherwise a failure
            // between the two saves leaves the user holding a revoked token with no replacement
            await using var transaction = await context.Database.BeginTransactionAsync();

            refreshToken.IsRevoked = true;

            context.UserRefreshTokens.Add(new UserRefreshToken
            {
                Token = newRefreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            });

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            Log.Information("[Refresh] SUCCESS - user {UserId} rotated {OldFingerprint} -> {NewFingerprint}", refreshToken.UserId, tokenFingerprint, Fingerprint(newRefreshToken));

            return Result.Ok(new LoginUserResponse
            {
                AccessToken = token,
                RefreshToken = newRefreshToken
            });
        }

        public async Task DeleteUserSession(string userRefreshToken)
        {
            var refreshToken = await context.UserRefreshTokens.FirstOrDefaultAsync(t => t.Token == userRefreshToken);

            // Logging out with an unknown token is not an error, the cookies are cleared either way
            if (refreshToken == null)
            {
                return;
            }

            refreshToken.IsRevoked = true;
            await context.SaveChangesAsync();

            Log.Information("Session {Fingerprint} logged out for user {UserId}", Fingerprint(userRefreshToken), refreshToken.UserId);
        }

        private static string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JwtKey")!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable("JwtValidIssuer"),
                audience: Environment.GetEnvironmentVariable("JwtValidAudience"),
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(128));
        }

        /// <summary>
        /// Short identifier for correlating a token in the logs without writing the credential itself to disk.
        /// </summary>
        private static string Fingerprint(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return "none";
            }

            return token.Length <= 10
                ? $"(len {token.Length})"
                : $"{token[..6]}…{token[^4..]} (len {token.Length})";
        }

        private async Task SaveRefreshToken(string token, string userId)
        {
            var refreshToken = new UserRefreshToken
            {
                Token = token,
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            context.UserRefreshTokens.Add(refreshToken);
            await context.SaveChangesAsync();
        }
    }
}
