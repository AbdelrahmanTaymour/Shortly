namespace Shortly.Core.DTOs.OrganizationDTOs;

public record CreateTeamDto(string Name, string? Description, Guid TeamManagerId, Guid OrganizationId);