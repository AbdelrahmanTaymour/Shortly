namespace Shortly.Core.DTOs.OrganizationDTOs;

public record CreateOrganizationDto(string Name, string? Description, string? Website, string? LogoUrl, int MemberLimit);