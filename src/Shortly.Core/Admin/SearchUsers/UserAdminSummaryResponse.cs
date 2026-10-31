using Shortly.Domain.Users;

namespace Shortly.Core.Admin.SearchUsers;

public sealed record UserAdminSummaryResponse(
    Guid Id, string Email, string Username, UserRole Role, bool IsActive,
    bool IsEmailConfirmed, DateTime CreatedAtUtc, DateTime? LastLoginAtUtc);