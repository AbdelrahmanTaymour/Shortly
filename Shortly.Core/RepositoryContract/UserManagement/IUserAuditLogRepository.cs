using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.UserManagement;

/// <summary>
/// Repository interface for managing user audit log records in the database.
/// </summary>
public interface IUserAuditLogRepository
{
    /// <summary>
    /// Asynchronously adds a new audit log entry to the database.
    /// </summary>
    /// <param name="log">The audit log entry to add.</param>
    Task AddAsync(UserAuditLog log);

    /// <summary>
    /// Retrieves recent audit logs for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user whose logs to retrieve.</param>
    /// <param name="limit">The maximum number of logs to retrieve. Default is 50.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A list of <see cref="UserAuditLog"/> entries.</returns>
    Task<IEnumerable<UserAuditLog>> GetByUserIdAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific audit log by its unique ID.
    /// </summary>
    /// <param name="id">The ID of the audit log to retrieve.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The <see cref="UserAuditLog"/> entry if found, otherwise null.</returns>
    Task<UserAuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches audit logs using optional filters like user ID, action name, and date range.
    /// </summary>
    /// <param name="userId">Optional user ID to filter logs by.</param>
    /// <param name="action">Optional action name to filter by (partial match allowed).</param>
    /// <param name="from">Optional start date/time (inclusive).</param>
    /// <param name="to">Optional end date/time (inclusive).</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of results per page (default: 100).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A list of matching <see cref="UserAuditLog"/> entries.</returns>
    Task<IEnumerable<UserAuditLog>> SearchAsync(
        Guid? userId = null,
        string? action = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);
}