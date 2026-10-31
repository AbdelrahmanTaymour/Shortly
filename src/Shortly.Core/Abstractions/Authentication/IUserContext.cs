using Shortly.Domain.Enums;
using Shortly.Domain.Members;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.Users;

namespace Shortly.Core.Abstractions.Authentication;

/// <summary>
/// Ambient, per-request authentication/authorization context. Single source of truth for
/// "who is making this request"
/// </summary>
public interface IUserContext
{
    Guid UserId { get; }
    string Email { get; }
    bool IsAuthenticated { get; }

    UserRole? Role { get; }

    Guid? OrganizationId { get; }
    Guid? MemberId { get; }
    MemberRole? MemberRole { get; }
    enShortUrlOwnerType OwnerType { get; }

    string? AnonymousSessionId { get; }
    string IpAddress { get; }

    bool IsAdmin { get; }
    bool IsOwner(Guid ownerId);
}