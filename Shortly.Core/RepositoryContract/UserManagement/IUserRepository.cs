using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.UserManagement;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername, CancellationToken cancellationToken = default);
    Task<User?> GetWithProfileAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetWithSecurityAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetWithUsageAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetCompleteUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid deletedBy, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailOrUsernameExistsAsync(string email, string username, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}