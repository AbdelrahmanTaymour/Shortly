using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Shortly.Domain.Enums;

namespace Shortly.Domain.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername, CancellationToken cancellationToken = default);
    Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> EmailOrUsernameExistsAsync(string email, string username, CancellationToken cancellationToken = default);
    void Add(User user);
    void Remove(User user);
}