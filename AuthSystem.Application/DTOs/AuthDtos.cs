namespace AuthSystem.Application.DTOs;

public record RegisterDto(
    string Email,
    string Password,
    string FirstName,
    string LastName
);

public record LoginDto(
    string Email,
    string Password
);

public record RefreshTokenDto(
    string RefreshToken
);

public record TokenResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType = "Bearer"
);

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    DateTime CreatedAt
);

public record AuthResponse<T>(
    bool Success,
    T? Data = default,
    AuthError? Error = null
);

public record AuthError(
    string Code,
    string Message
);
