using Shortly.Core.ServiceContracts.Authentication;
using Shortly.Domain.Enums;

namespace Shortly.Core.Models;

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