namespace Shortly.Core.Teams.GetTeam;

public sealed record OrganizationTeamResponse(Guid Id, Guid OrganizationId, Guid TeamManagerId, string TeamManagerUsername, string Name, string? Description, DateTime CreatedAt, int MemberCount);
