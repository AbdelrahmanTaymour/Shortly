using Shortly.Domain.Enums;
using Shortly.Domain.Users;

namespace Shortly.Core.Members.DTOs;

public record UpdateMemberPermissionsRequest(enPermissions Permissions);