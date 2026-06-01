using AuthSystem.Application.Entities;
using AuthSystem.Application.DTOs;

namespace AuthSystem.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
