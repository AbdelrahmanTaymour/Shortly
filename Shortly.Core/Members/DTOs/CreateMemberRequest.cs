using Shortly.Domain.Members;

namespace Shortly.Core.Members.DTOs;

public record CreateMemberRequest(Guid OrganizationId, Guid UserId, MemberRole Role, Guid InvitedBy);