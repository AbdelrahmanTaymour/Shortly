using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.OrganizationDTOs;

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