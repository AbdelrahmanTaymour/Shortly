using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Members;

namespace Shortly.Core.Members.UpdateMemberRole;

public sealed record UpdateMemberRoleCommand(Guid OrganizationId, Guid UserId, MemberRole NewRole) : ICommand;
