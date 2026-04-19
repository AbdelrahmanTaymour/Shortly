using Shortly.Domain.Enums;

namespace Shortly.Core.Members.DTOs;

public record UpdateMemberPermissionsRequest(enPermissions Permissions);