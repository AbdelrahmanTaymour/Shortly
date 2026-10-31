using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Members;

namespace Shortly.Core.Members.AddMember;

public sealed record AddMemberCommand(Guid OrganizationId, Guid UserId, MemberRole Role) : ICommand;
