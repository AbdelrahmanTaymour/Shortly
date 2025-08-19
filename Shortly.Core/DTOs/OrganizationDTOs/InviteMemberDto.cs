using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.OrganizationDTOs;

public record InviteMemberDto(string Email, enUserRole RoleId, enPermissions CustomPermissions, Guid InvitedBy);