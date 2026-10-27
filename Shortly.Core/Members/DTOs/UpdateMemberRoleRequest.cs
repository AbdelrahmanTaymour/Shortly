using Shortly.Domain.Enums;
using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Core.Members.DTOs;

public record UpdateMemberRoleRequest(MemberRole Role);