using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.OrganizationDTOs;

public record UpdateMemberPermissionsRequest(enPermissions Permissions);