using Shortly.Domain.Users;

namespace Shortly.Core.Admin.SearchUsers;

public sealed class UserAdminSummaryResponse
{
    public Guid Id { get; init; }

    public required string Email { get; init; }

    public required string Username { get; init; }

    public UserRole Role { get; init; }

    public bool IsActive { get; init; }

    public bool IsEmailConfirmed { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? LastLoginAtUtc { get; init; }
}