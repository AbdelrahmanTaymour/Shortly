using Shortly.Domain.Enums;

namespace Shortly.Core.Invitations.DTOs;

public record InviteMemberDto(string Email, enUserRole RoleId, enPermissions CustomPermissions, Guid InvitedBy);