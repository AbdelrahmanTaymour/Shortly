namespace Shortly.Core.Teams.DTOs;

public record CreateTeamRequest(string Name, string? Description, Guid TeamManagerId, Guid OrganizationId);