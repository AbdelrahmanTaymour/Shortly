namespace Shortly.Core.Organizations.DTOs;

public record UpdateOrganizationDto(string? Name, string? Description, string? Website, string? LogoUrl, int? MemberLimit, bool? IsActive, bool? IsSubscribed);