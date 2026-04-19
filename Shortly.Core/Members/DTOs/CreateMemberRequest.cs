using Shortly.Domain.Enums;

namespace Shortly.Core.Members.DTOs;

public record CreateMemberRequest(Guid OrganizationId, Guid UserId, enUserRole RoleId, Guid InvitedBy);