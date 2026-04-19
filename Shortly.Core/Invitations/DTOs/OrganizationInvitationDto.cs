using Shortly.Domain.Enums;

namespace Shortly.Core.Invitations.DTOs;

public record OrganizationInvitationDto(
    Guid Id,
    Guid OrganizationId,
    string InvitedUserEmail,
    enUserRole RoleId,
    enPermissions CustomPermissions,
    Guid InvitedBy,
    enInvitationStatus Status,
    DateTime? RegisteredAt,
    DateTime? ExpiresAt,
    DateTime CreatedAt
);