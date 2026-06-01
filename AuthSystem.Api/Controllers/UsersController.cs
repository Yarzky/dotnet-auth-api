using System.Security.Claims;
using AuthSystem.Application.DTOs;
using AuthSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthSystem.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        var result = await _userService.GetProfileAsync(userId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(UpdateUserDto updateDto)
    {
        var userId = GetUserId();
        var result = await _userService.UpdateProfileAsync(userId, updateDto);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto passwordDto)
    {
        var userId = GetUserId();
        var result = await _userService.ChangePasswordAsync(userId, passwordDto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllUsers()
    {
        var result = await _userService.GetAllUsersAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var result = await _userService.GetUserByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{id}/role")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateUserRole(Guid id, UpdateRoleDto roleDto)
    {
        var result = await _userService.UpdateUserRoleAsync(id, roleDto.Role);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        var result = await _userService.DeactivateUserAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null)
        {
            throw new UnauthorizedAccessException("User ID claim not found.");
        }
        return Guid.Parse(userIdClaim.Value);
    }
}
