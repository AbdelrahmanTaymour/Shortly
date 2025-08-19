namespace Shortly.Core.DTOs.OrganizationDTOs;

public record UpdateOrganizationDto(string? Name, string? Description, string? Website, string? LogoUrl, int? MemberLimit, bool? IsActive, bool? IsSubscribed);