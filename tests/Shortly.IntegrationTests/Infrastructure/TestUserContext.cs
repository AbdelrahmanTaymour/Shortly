using Shortly.Core.Abstractions.Authentication;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.Users;

namespace Shortly.IntegrationTests.Infrastructure;

/// <summary>
/// Replaces the HTTP-based <see cref="IUserContext"/> so a test states who is calling.
/// One instance lives in every DI scope; <see cref="IntegrationTestBase"/> applies the current <see cref="Caller"/> to it.
/// </summary>
public sealed class TestUserContext : IUserContext
{
    public Caller Caller { get; set; } = Caller.Anonymous;

    public Guid UserId => Caller.IsAuthenticated
        ? Caller.UserId
        : throw new ApplicationException("User context is unavailable");

    public string Email => Caller.Email;
    public bool IsAuthenticated => Caller.IsAuthenticated;
    public UserRole? Role => Caller.IsAuthenticated ? Caller.Role : null;
    public Guid? OrganizationId => Caller.OrganizationId;

    public enShortUrlOwnerType OwnerType =>
        !IsAuthenticated ? enShortUrlOwnerType.Anonymous :
        OrganizationId is not null ? enShortUrlOwnerType.Organization :
        enShortUrlOwnerType.User;

    public string? AnonymousSessionId => IsAuthenticated ? null : Caller.AnonymousSessionId;
    public string IpAddress => Caller.IpAddress;
    public bool IsAdmin => Role is UserRole.SuperAdmin or UserRole.SystemAdmin;
    public bool IsOwner(Guid ownerId) => IsAuthenticated && UserId == ownerId;
}

/// <summary>Who is making the call. Use the factory methods; <see cref="OrganizationId"/> selects the organization workspace.</summary>
public sealed record Caller(
    bool IsAuthenticated,
    Guid UserId,
    string Email,
    UserRole Role,
    Guid? OrganizationId,
    string? AnonymousSessionId,
    string IpAddress)
{
    public static Caller Anonymous { get; } =
        new(false, Guid.Empty, string.Empty, UserRole.User, null, "anonymous-session", "203.0.113.7");

    public static Caller User(Guid userId, Guid? organizationId = null, UserRole role = UserRole.User, string? email = null) =>
        new(true, userId, email ?? $"{userId:N}@example.com", role, organizationId, null, "203.0.113.7");

    public static Caller Admin(Guid? userId = null) => User(userId ?? Guid.NewGuid(), role: UserRole.SystemAdmin);
}
