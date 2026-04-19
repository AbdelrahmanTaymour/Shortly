using Shortly.Domain.Enums;

namespace Shortly.Core.Members.DTOs;

public record OrganizationMemberDto(
    Guid Id,
    Guid OrganizationId,
    Guid UserId,
    enUserRole RoleId,
    enPermissions CustomPermissions,
    bool IsActive,
    Guid InvitedBy,
    DateTime JoinedAt
);