using AuthSystem.Application.DTOs;
using AuthSystem.Application.Entities;
using AuthSystem.Application.Interfaces;
using AuthSystem.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace AuthSystem.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IRefreshTokenRepository> _tokenRepoMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IPasswordService> _passwordServiceMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _tokenRepoMock = new Mock<IRefreshTokenRepository>();
        _tokenServiceMock = new Mock<ITokenService>();
        _passwordServiceMock = new Mock<IPasswordService>();
        _configMock = new Mock<IConfiguration>();

        _authService = new AuthService(
            _userRepoMock.Object,
            _tokenRepoMock.Object,
            _tokenServiceMock.Object,
            _passwordServiceMock.Object,
            _configMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnSuccess_WhenEmailIsUnique()
    {
        // Arrange
        var dto = new RegisterDto("test@example.com", "Password123!", "John", "Doe");
        _userRepoMock.Setup(r => r.ExistsByEmailAsync(dto.Email)).ReturnsAsync(false);
        _passwordServiceMock.Setup(s => s.HashPassword(dto.Password)).Returns("hashed_password");

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Email.Should().Be(dto.Email);
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
    {
        // Arrange
        var dto = new LoginDto("test@example.com", "Password123!");
        var user = new User { Id = Guid.NewGuid(), Email = dto.Email, PasswordHash = "hashed", IsActive = true };
        
        _userRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        _passwordServiceMock.Setup(s => s.VerifyPassword(dto.Password, user.PasswordHash)).Returns(true);
        _tokenServiceMock.Setup(s => s.GenerateAccessToken(user)).Returns("access_token");
        _tokenServiceMock.Setup(s => s.GenerateRefreshToken()).Returns("refresh_token");
        _configMock.Setup(c => c["JwtSettings:AccessTokenExpiryMinutes"]).Returns("15");

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("access_token");
        result.Data!.RefreshToken.Should().Be("refresh_token");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldRotateToken_WhenValid()
    {
        // Arrange
        var oldToken = "old_token";
        var user = new User { Id = Guid.NewGuid(), IsActive = true };
        var refreshToken = new RefreshToken { Token = oldToken, UserId = user.Id, User = user, ExpiresAt = DateTime.UtcNow.AddDays(1) };
        
        _tokenRepoMock.Setup(r => r.GetByTokenAsync(oldToken)).ReturnsAsync(refreshToken);
        _tokenServiceMock.Setup(s => s.GenerateRefreshToken()).Returns("new_token");
        _tokenServiceMock.Setup(s => s.GenerateAccessToken(user)).Returns("new_access_token");

        // Act
        var result = await _authService.RefreshTokenAsync(oldToken);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.RefreshToken.Should().Be("new_token");
        refreshToken.RevokedAt.Should().NotBeNull();
        _tokenRepoMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldRevokeAll_WhenTokenIsAlreadyRevoked()
    {
        // Arrange
        var reusedToken = "reused_token";
        var refreshToken = new RefreshToken { Token = reusedToken, UserId = Guid.NewGuid(), RevokedAt = DateTime.UtcNow };
        
        _tokenRepoMock.Setup(r => r.GetByTokenAsync(reusedToken)).ReturnsAsync(refreshToken);

        // Act
        var result = await _authService.RefreshTokenAsync(reusedToken);

        // Assert
        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("REFRESH_TOKEN_INVALID");
        _tokenRepoMock.Verify(r => r.RevokeAllForUserAsync(refreshToken.UserId), Times.Once);
    }
}
