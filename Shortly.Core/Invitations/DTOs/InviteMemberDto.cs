using Shortly.Domain.Enums;
using Shortly.Domain.Members;

namespace Shortly.Core.Invitations.DTOs;

public record InviteMemberDto(string Email, MemberRole Role, enPermissions CustomPermissions, Guid InvitedBy);