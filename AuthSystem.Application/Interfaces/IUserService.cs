using AuthSystem.Application.DTOs;

namespace AuthSystem.Application.Interfaces;

public interface IUserService
{
    Task<AuthResponse<UserDto>> GetProfileAsync(Guid userId);
    Task<AuthResponse<UserDto>> UpdateProfileAsync(Guid userId, UpdateUserDto updateDto);
    Task<AuthResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto passwordDto);
    Task<AuthResponse<IEnumerable<UserDto>>> GetAllUsersAsync();
    Task<AuthResponse<UserDto>> GetUserByIdAsync(Guid userId);
    Task<AuthResponse<bool>> UpdateUserRoleAsync(Guid userId, string role);
    Task<AuthResponse<bool>> DeactivateUserAsync(Guid userId);
}
