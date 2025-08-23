namespace Shortly.Core.DTOs.OrganizationDTOs;

public record OrganizationTeamDto(Guid Id, Guid OrganizationId, Guid TeamManagerId, string Name, string? Description, DateTime CreatedAt);