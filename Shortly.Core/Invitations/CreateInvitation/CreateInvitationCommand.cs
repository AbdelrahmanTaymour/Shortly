using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Core.Invitations.CreateInvitation;

public sealed record CreateInvitationCommand(Guid OrganizationId, string Email, MemberRole Role, enPermissions CustomPermissions) : ICommand<Guid>;
