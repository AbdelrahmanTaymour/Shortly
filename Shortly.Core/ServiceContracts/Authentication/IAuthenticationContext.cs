using Shortly.Domain.Enums;

namespace Shortly.Core.ServiceContracts.Authentication;

public interface IAuthenticationContext
{
    Guid? UserId { get; }
    Guid? OrganizationId { get; }
    Guid? MemberId { get; }
    enShortUrlOwnerType OwnerType { get; }
    bool IsAuthenticated { get; }
    bool IsAnonymous { get; }
    string? AnonymousSessionId { get; }
    string? IpAddress { get; }
}