namespace Shortly.Core.Organizations.DTOs;

public record CreateOrganizationDto(string Name, string? Description, string? Website, string? LogoUrl, int MemberLimit);