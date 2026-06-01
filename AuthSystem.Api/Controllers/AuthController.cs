using AuthSystem.Application.DTOs;
using AuthSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AuthSystem.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto registerDto)
    {
        var result = await _authService.RegisterAsync(registerDto);

        if (!result.Success)
        {
            if (result.Error?.Code == "EMAIL_ALREADY_EXISTS")
            {
                return Conflict(result);
            }
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(Register), new { id = result.Data?.Id }, result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto loginDto)
    {
        var result = await _authService.LoginAsync(loginDto);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenDto refreshTokenDto)
    {
        var result = await _authService.RefreshTokenAsync(refreshTokenDto.RefreshToken);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(RefreshTokenDto refreshTokenDto)
    {
        var result = await _authService.RevokeTokenAsync(refreshTokenDto.RefreshToken);

        if (!result)
        {
            return BadRequest(new AuthResponse<object>(false, Error: new AuthError("REFRESH_TOKEN_INVALID", "Invalid or already revoked token.")));
        }

        return NoContent();
    }
}
