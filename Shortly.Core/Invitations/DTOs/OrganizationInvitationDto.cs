using Shortly.Domain.Enums;
using Shortly.Domain.Members;
using Shortly.Domain.RepositoryContract.Invitations;
using Shortly.Domain.Users;

namespace Shortly.Core.Invitations.DTOs;

public record OrganizationInvitationDto(
    Guid Id,
    Guid OrganizationId,
    string InvitedUserEmail,
    MemberRole Role,
    enPermissions CustomPermissions,
    Guid InvitedBy,
    enInvitationStatus Status,
    DateTime? RegisteredAt,
    DateTime? ExpiresAt,
    DateTime CreatedAt
);