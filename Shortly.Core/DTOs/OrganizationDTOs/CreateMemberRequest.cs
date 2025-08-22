using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.OrganizationDTOs;

public record CreateMemberRequest(Guid OrganizationId, Guid UserId, enUserRole RoleId, Guid InvitedBy);