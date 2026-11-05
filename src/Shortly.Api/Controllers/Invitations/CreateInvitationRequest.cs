using Shortly.Domain.Members;

namespace Shortly.Api.Controllers.Invitations;

public sealed record CreateInvitationRequest(string Email, MemberRole Role);
