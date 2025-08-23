namespace Shortly.Core.DTOs.OrganizationDTOs;

public record CreateTeamRequest(string Name, string? Description, Guid TeamManagerId, Guid OrganizationId);