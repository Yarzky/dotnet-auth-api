using AuthSystem.Application.Entities;
using AuthSystem.Application.Interfaces;
using AuthSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthSystem.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task AddAsync(RefreshToken refreshToken)
    {
        await _context.RefreshTokens.AddAsync(refreshToken);
    }

    public void Update(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Update(refreshToken);
    }

    public async Task RevokeAllForUserAsync(Guid userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();
        
        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        _context.RefreshTokens.UpdateRange(tokens);
    }

    public async Task DeleteExpiredTokensAsync()
    {
        var expiredTokens = _context.RefreshTokens
            .Where(rt => rt.ExpiresAt <= DateTime.UtcNow);
        
        _context.RefreshTokens.RemoveRange(expiredTokens);
        await Task.CompletedTask;
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
