using AuthSystem.Application.DTOs;
using AuthSystem.Application.Entities;
using AuthSystem.Application.Enums;
using AuthSystem.Application.Interfaces;

namespace AuthSystem.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;

    public UserService(IUserRepository userRepository, IPasswordService passwordService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
    }

    public async Task<AuthResponse<UserDto>> GetProfileAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return NotFoundError<UserDto>();

        return SuccessResponse(MapToDto(user));
    }

    public async Task<AuthResponse<UserDto>> UpdateProfileAsync(Guid userId, UpdateUserDto updateDto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return NotFoundError<UserDto>();

        user.FirstName = updateDto.FirstName;
        user.LastName = updateDto.LastName;

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        return SuccessResponse(MapToDto(user));
    }

    public async Task<AuthResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto passwordDto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return NotFoundError<bool>();

        if (!_passwordService.VerifyPassword(passwordDto.CurrentPassword, user.PasswordHash))
        {
            return new AuthResponse<bool>(false, Error: new AuthError("INVALID_PASSWORD", "Current password is incorrect."));
        }

        user.PasswordHash = _passwordService.HashPassword(passwordDto.NewPassword);
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        return SuccessResponse(true);
    }

    public async Task<AuthResponse<IEnumerable<UserDto>>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return SuccessResponse(users.Select(MapToDto));
    }

    public async Task<AuthResponse<UserDto>> GetUserByIdAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return NotFoundError<UserDto>();

        return SuccessResponse(MapToDto(user));
    }

    public async Task<AuthResponse<bool>> UpdateUserRoleAsync(Guid userId, string role)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return NotFoundError<bool>();

        if (!Enum.TryParse<UserRole>(role, true, out var userRole))
        {
            return new AuthResponse<bool>(false, Error: new AuthError("INVALID_ROLE", "Provided role is invalid."));
        }

        user.Role = userRole;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        return SuccessResponse(true);
    }

    public async Task<AuthResponse<bool>> DeactivateUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return NotFoundError<bool>();

        user.IsActive = false;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        return SuccessResponse(true);
    }

    private static UserDto MapToDto(User user) => 
        new(user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString(), user.CreatedAt);

    private static AuthResponse<T> SuccessResponse<T>(T data) => new(true, data);

    private static AuthResponse<T> NotFoundError<T>() => 
        new(false, Error: new AuthError("USER_NOT_FOUND", "User not found."));
}
