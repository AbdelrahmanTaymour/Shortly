using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Members;

namespace Shortly.Core.Invitations.CreateInvitation;

public sealed record CreateInvitationCommand(Guid OrganizationId, string Email, MemberRole Role) : ICommand<Guid>;
