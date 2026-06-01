using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthSystem.Application.Entities;
using AuthSystem.Application.Enums;
using AuthSystem.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace AuthSystem.Tests.Services;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;
    private readonly Mock<IConfiguration> _configMock;
    private const string SecretKey = "a_very_long_and_secure_secret_key_at_least_256_bits";

    public TokenServiceTests()
    {
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["JWT_SECRET_KEY"]).Returns(SecretKey);
        _configMock.Setup(c => c.GetSection("JwtSettings")["Issuer"]).Returns("AuthSystem");
        _configMock.Setup(c => c.GetSection("JwtSettings")["Audience"]).Returns("AuthSystem.Client");
        _configMock.Setup(c => c.GetSection("JwtSettings")["AccessTokenExpiryMinutes"]).Returns("15");

        _tokenService = new TokenService(_configMock.Object);
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwt()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Role = UserRole.User
        };

        // Act
        var token = _tokenService.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        jwtToken.Subject.Should().Be(user.Id.ToString());
        jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value.Should().Be(user.Email);
        jwtToken.Claims.First(c => c.Type == "role" || c.Type == ClaimTypes.Role).Value.Should().Be(user.Role.ToString());
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnRandomString()
    {
        // Act
        var token1 = _tokenService.GenerateRefreshToken();
        var token2 = _tokenService.GenerateRefreshToken();

        // Assert
        token1.Should().NotBeNullOrEmpty();
        token2.Should().NotBeNullOrEmpty();
        token1.Should().NotBe(token2);
    }
}
