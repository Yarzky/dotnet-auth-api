using AuthSystem.Application.DTOs;
using AuthSystem.Application.Entities;
using AuthSystem.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AuthSystem.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordService _passwordService;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        IPasswordService passwordService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _passwordService = passwordService;
        _configuration = configuration;
    }

    public async Task<AuthResponse<UserDto>> RegisterAsync(RegisterDto registerDto)
    {
        if (await _userRepository.ExistsByEmailAsync(registerDto.Email))
        {
            return new AuthResponse<UserDto>(false, Error: new AuthError("EMAIL_ALREADY_EXISTS", "Email already registered."));
        }

        var user = new User
        {
            Email = registerDto.Email,
            PasswordHash = _passwordService.HashPassword(registerDto.Password),
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var userDto = new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString(), user.CreatedAt);
        return new AuthResponse<UserDto>(true, userDto);
    }

    public async Task<AuthResponse<TokenResponseDto>> LoginAsync(LoginDto loginDto)
    {
        var user = await _userRepository.GetByEmailAsync(loginDto.Email);

        if (user == null || !_passwordService.VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            return new AuthResponse<TokenResponseDto>(false, Error: new AuthError("INVALID_CREDENTIALS", "Email or password is incorrect."));
        }

        if (!user.IsActive)
        {
            return new AuthResponse<TokenResponseDto>(false, Error: new AuthError("ACCOUNT_DISABLED", "User account has been deactivated."));
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshTokenExpiryDays = double.Parse(_configuration["JwtSettings:RefreshTokenExpiryDays"] ?? "7");
        var refreshToken = new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays)
        };

        await _refreshTokenRepository.AddAsync(refreshToken);
        await _refreshTokenRepository.SaveChangesAsync();

        var expiresIn = int.Parse(_configuration["JwtSettings:AccessTokenExpiryMinutes"] ?? "15") * 60;
        var tokenResponse = new TokenResponseDto(accessToken, refreshTokenValue, expiresIn);

        return new AuthResponse<TokenResponseDto>(true, tokenResponse);
    }

    public async Task<AuthResponse<TokenResponseDto>> RefreshTokenAsync(string token)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token);

        if (refreshToken == null)
        {
            return new AuthResponse<TokenResponseDto>(false, Error: new AuthError("REFRESH_TOKEN_INVALID", "Invalid refresh token."));
        }

        if (refreshToken.IsRevoked)
        {
            // Reuse detection: revoke all tokens for this user
            await _refreshTokenRepository.RevokeAllForUserAsync(refreshToken.UserId);
            await _refreshTokenRepository.SaveChangesAsync();
            return new AuthResponse<TokenResponseDto>(false, Error: new AuthError("REFRESH_TOKEN_INVALID", "Token already used or revoked. Security breach suspected."));
        }

        if (refreshToken.IsExpired)
        {
            return new AuthResponse<TokenResponseDto>(false, Error: new AuthError("REFRESH_TOKEN_EXPIRED", "Refresh token has expired."));
        }

        var user = refreshToken.User;
        if (!user.IsActive)
        {
            return new AuthResponse<TokenResponseDto>(false, Error: new AuthError("ACCOUNT_DISABLED", "User account has been deactivated."));
        }

        // Rotate token
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiryDays = double.Parse(_configuration["JwtSettings:RefreshTokenExpiryDays"] ?? "7");
        
        var newRefreshToken = new RefreshToken
        {
            Token = newRefreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays)
        };

        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.ReplacedByToken = newRefreshTokenValue;

        await _refreshTokenRepository.AddAsync(newRefreshToken);
        _refreshTokenRepository.Update(refreshToken);
        await _refreshTokenRepository.SaveChangesAsync();

        var accessToken = _tokenService.GenerateAccessToken(user);
        var expiresIn = int.Parse(_configuration["JwtSettings:AccessTokenExpiryMinutes"] ?? "15") * 60;

        return new AuthResponse<TokenResponseDto>(true, new TokenResponseDto(accessToken, newRefreshTokenValue, expiresIn));
    }

    public async Task<bool> RevokeTokenAsync(string token)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token);

        if (refreshToken == null || !refreshToken.IsActive)
        {
            return false;
        }

        refreshToken.RevokedAt = DateTime.UtcNow;
        _refreshTokenRepository.Update(refreshToken);
        return await _refreshTokenRepository.SaveChangesAsync();
    }
}
