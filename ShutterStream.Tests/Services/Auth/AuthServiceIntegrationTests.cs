using Microsoft.EntityFrameworkCore;
using ShutterStream.Domain.DTOs.Auth.Requests;
using ShutterStream.Domain.Results;
using ShutterStream.Domain.Services.Auth;
using ShutterStream.Tests.Infrastructure;

namespace ShutterStream.Tests.Services.Auth
{
    [TestFixture]
    public class AuthServiceIntegrationTests : DatabaseIntegrationTestBase
    {
        private AuthService _authService = null!;

        protected override async Task CustomSetUp()
        {
            Environment.SetEnvironmentVariable("JwtKey", "test-jwt-key-123123123123123123123123123123123123123123123123123");
            Environment.SetEnvironmentVariable("JwtValidIssuer", "https://localhost");
            Environment.SetEnvironmentVariable("JwtValidAudience", "shutterstream");

            await TestDatabaseSeedHelper.SeedTestUser(DbContext);

            _authService = new AuthService(DbContext);
        }

        [Test]
        public async Task LoginUser_WithValidCredentials_ShouldReturnTokensAndSaveRefreshToken()
        {
            // Act
            var result = (await _authService.LoginUser(new LoginUserRequest { Username = "testuser", Password = "test123!" })).Value;

            // Assert
            Assert.That(result.AccessToken, Is.Not.Empty);
            Assert.That(result.RefreshToken, Is.Not.Empty);
            Assert.That(await DbContext.UserRefreshTokens.AnyAsync(t => t.Token == result.RefreshToken), Is.True);
        }

        [Test]
        public async Task LoginUser_ShouldIgnoreUsernameCaseAndWhitespace()
        {
            // Act
            var result = (await _authService.LoginUser(new LoginUserRequest { Username = "  TestUser ", Password = "test123!" })).Value;

            // Assert
            Assert.That(result.AccessToken, Is.Not.Empty);
        }

        [Test]
        public async Task LoginUser_WithWrongPassword_ShouldReturnUnauthorized()
        {
            // Act & Assert
            var result = await _authService.LoginUser(new LoginUserRequest { Username = "testuser", Password = "wrong" });
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Unauthorized));

            Assert.That(result.Error, Is.EqualTo("Invalid username or password"));
        }

        [Test]
        public async Task LoginUser_WithUnknownUser_ShouldThrowSameMessageAsWrongPassword()
        {
            // Act & Assert
            var result = await _authService.LoginUser(new LoginUserRequest { Username = "nobody", Password = "test123!" });
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Unauthorized));

            Assert.That(result.Error, Is.EqualTo("Invalid username or password"));
        }

        [Test]
        public async Task RefreshToken_WithValidToken_ShouldRotateToken()
        {
            // Arrange
            var login = (await _authService.LoginUser(new LoginUserRequest { Username = "testuser", Password = "test123!" })).Value;

            // Act
            var refreshed = (await _authService.RefreshToken(login.RefreshToken)).Value;

            // Assert
            Assert.That(refreshed.RefreshToken, Is.Not.EqualTo(login.RefreshToken));

            var oldToken = await DbContext.UserRefreshTokens.AsNoTracking().FirstAsync(t => t.Token == login.RefreshToken);
            Assert.That(oldToken.IsRevoked, Is.True);
        }

        [Test]
        public async Task RefreshToken_WithRevokedToken_ShouldReturnUnauthorized()
        {
            // Arrange
            var login = (await _authService.LoginUser(new LoginUserRequest { Username = "testuser", Password = "test123!" })).Value;
            await _authService.RefreshToken(login.RefreshToken);

            // Act & Assert
            var result = await _authService.RefreshToken(login.RefreshToken);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Unauthorized));
        }

        [Test]
        public async Task RefreshToken_WithUnknownToken_ShouldReturnUnauthorized()
        {
            // Act & Assert
            var result = await _authService.RefreshToken("not-a-real-token");
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Unauthorized));
        }

        [Test]
        public async Task DeleteUserSession_ShouldRevokeRefreshToken()
        {
            // Arrange
            var login = (await _authService.LoginUser(new LoginUserRequest { Username = "testuser", Password = "test123!" })).Value;

            // Act
            await _authService.DeleteUserSession(login.RefreshToken);

            // Assert
            var result = await _authService.RefreshToken(login.RefreshToken);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Unauthorized));
        }

        [Test]
        public async Task DeleteUserSession_WithUnknownToken_ShouldNotThrow()
        {
            // Act & Assert
            await Assert.DoesNotThrowAsync(async () => await _authService.DeleteUserSession("not-a-real-token"));
        }
    }
}
