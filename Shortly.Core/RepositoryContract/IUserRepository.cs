using Shortly.Core.Entities;

namespace Shortly.Core.RepositoryContract;

public interface IUserRepository
{
    Task<User?> AddUser(User user);
    Task<User?> GetUserByEmailAndPassword(string? email, string? password);
    Task<bool> IsEmailOrUsernameTaken(string email, string username);
    Task<User?> GetUserByEmail(string? email);
    Task<User?> GetUserByUsername(string? username);

}