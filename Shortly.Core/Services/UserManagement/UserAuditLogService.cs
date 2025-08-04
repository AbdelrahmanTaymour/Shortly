using Microsoft.Extensions.Logging;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Core.ServiceContracts.UserManagement;
using Shortly.Domain.Entities;

namespace Shortly.Core.Services.UserManagement;


public class UserAuditLogService(IUserAuditLogRepository auditLogRepository, ILogger<UserAuditLogService> logger)
    : IUserAuditLogService
{
    /// <inheritdoc/>
    public async Task LogAsync(Guid userId, string action, string? details = null, string? ipAddress = null,
        string? userAgent = null)
    {
        var log = new UserAuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            Details = details,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            TimeStamp = DateTime.UtcNow
        };

        await auditLogRepository.AddAsync(log);
        logger.LogInformation("Audit log added for user {UserId}: {Action}", userId, action);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<UserAuditLog>> GetRecentLogsAsync(Guid userId, int limit = 50,
        CancellationToken cancellationToken = default)
    {
        return await auditLogRepository.GetByUserIdAsync(userId, limit, cancellationToken);
    }
    
    /// <inheritdoc/>
    public async Task<UserAuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await auditLogRepository.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<UserAuditLog>> SearchLogsAsync(Guid? userId = null, string? action = null,
        DateTime? from = null, DateTime? to = null,
        int page = 1, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        return await auditLogRepository.SearchAsync(userId, action, from, to, page, pageSize, cancellationToken);
    }
}