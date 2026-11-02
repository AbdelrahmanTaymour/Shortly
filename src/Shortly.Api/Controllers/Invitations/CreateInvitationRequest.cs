using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Api.Controllers.Invitations;

public sealed record CreateInvitationRequest(string Email, MemberRole Role, enPermissions CustomPermissions = enPermissions.None);
