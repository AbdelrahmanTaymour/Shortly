using Shortly.Domain.Enums;
using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Core.Invitations.DTOs;

public record InviteMemberDto(string Email, MemberRole Role, enPermissions CustomPermissions, Guid InvitedBy);