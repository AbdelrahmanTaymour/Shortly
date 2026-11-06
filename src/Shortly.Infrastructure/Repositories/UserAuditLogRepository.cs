using Shortly.Domain.Users;

namespace Shortly.Infrastructure.Repositories;

public sealed class UserAuditLogRepository(ApplicationDbContext dbContext)
    : Repository<UserAuditLog>(dbContext), IUserAuditLogRepository;
