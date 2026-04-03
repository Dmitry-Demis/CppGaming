using cppQuest.Server.Models;

namespace cppQuest.Server.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIsuNumberAsync(string isuNumber);
    Task<User?> GetByIdAsync(int id);
    Task<bool> IsIsuAllowedAsync(string isuNumber);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}
