using AuthSystem.Application.DTOs;

namespace AuthSystem.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse<UserDto>> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponse<TokenResponseDto>> LoginAsync(LoginDto loginDto);
}
