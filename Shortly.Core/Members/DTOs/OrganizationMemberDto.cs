using Shortly.Domain.Enums;
using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Core.Members.DTOs;

public record OrganizationMemberDto(
    Guid Id,
    Guid OrganizationId,
    Guid UserId,
    MemberRole Role,
    bool IsActive,
    Guid InvitedBy,
    DateTime JoinedAt
);