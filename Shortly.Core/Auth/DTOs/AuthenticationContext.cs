using Shortly.Core.Auth.Contracts;
using Shortly.Domain.Enums;

namespace Shortly.Core.Auth.DTOs;

public class AuthenticationContext : IAuthenticationContext
{
    public Guid? UserId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? MemberId { get; set; }
    public enShortUrlOwnerType OwnerType { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool IsAnonymous => !IsAuthenticated;
    public string? AnonymousSessionId { get; set; }
    public string? IpAddress { get; set; }
}