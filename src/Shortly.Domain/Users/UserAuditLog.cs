using Shortly.Domain.Abstractions;

namespace Shortly.Domain.Users;

public sealed class UserAuditLog : Entity<Guid>
{
    private UserAuditLog(Guid id, Guid userId, string action, string? details, string? ipAddress, string? userAgent, DateTime utcNow)
        : base(id)
    {
        UserId = userId;
        Action = action;
        Details = details;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        TimeStampUtc = utcNow;
    }

    private UserAuditLog() { }

    public Guid UserId { get; private set; }
    public string Action { get; private set; } = null!;
    public string? Details { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime TimeStampUtc { get; private set; }

    public static UserAuditLog Record(Guid userId, string action, string? details, string? ipAddress, string? userAgent, DateTime utcNow) =>
        new(Guid.NewGuid(), userId, action, details, ipAddress, userAgent, utcNow);
}