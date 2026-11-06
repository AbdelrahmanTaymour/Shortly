namespace Shortly.Domain.Users;

public interface IUserAuditLogRepository
{
    void Add(UserAuditLog entry);
}
