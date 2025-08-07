using Shortly.Core.DTOs.UsersDTOs.Usage;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Domain.Entities;

namespace Shortly.Core.ServiceContracts.UserManagement;

/// <summary>
/// Service interface for high-level user audit logging operations.
/// </summary>
public interface IUserAuditLogService
{
    /// <summary>
    /// Logs a user action with optional details and context (IP address, user agent, etc).
    /// </summary>
    /// <param name="userId">The ID of the user who performed the action.</param>
    /// <param name="action">The name of the action performed (e.g., "Login", "ShortUrlCreated").</param>
    /// <param name="details">Optional detailed description of the action (e.g., slug or entity affected).</param>
    /// <param name="ipAddress">Optional IP address where the action originated.</param>
    /// <param name="userAgent">Optional user-agent header string.</param>
    Task LogAsync(Guid userId, string action, string? details = null, string? ipAddress = null,
        string? userAgent = null);

    /// <summary>
    /// Retrieves the most recent audit logs for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="limit">Maximum number of logs to return. Default is 50.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A list of <see cref="UserAuditLog"/> entries.</returns>
    Task<IEnumerable<UserAuditLog>> GetRecentLogsAsync(Guid userId, int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific audit log entry by its ID.
    /// </summary>
    /// <param name="id">The ID of the log entry to retrieve.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The <see cref="UserAuditLog"/> entry if found, otherwise null.</returns>
    Task<UserAuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches audit logs with optional filters like action name, user, or date range.
    /// </summary>
    /// <param name="userId">Optional user ID to filter by.</param>
    /// <param name="action">Optional action name to filter by.</param>
    /// <param name="from">Optional start date/time (inclusive).</param>
    /// <param name="to">Optional end date/time (inclusive).</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page. Default is 100.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A list of matching <see cref="UserAuditLog"/> entries.</returns>
    Task<IEnumerable<UserAuditLog>> SearchLogsAsync(
        Guid? userId = null,
        string? action = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);
}