using AuthSystem.Application.Entities;

namespace AuthSystem.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task AddAsync(User user);
    void Update(User user);
    void Delete(User user);
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> SaveChangesAsync();
}
