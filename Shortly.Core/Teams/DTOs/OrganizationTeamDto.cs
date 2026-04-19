namespace Shortly.Core.Teams.DTOs;

public record OrganizationTeamDto(Guid Id, Guid OrganizationId, Guid TeamManagerId, string Name, string? Description, DateTime CreatedAt);