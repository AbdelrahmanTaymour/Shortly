namespace Shortly.API.Controllers.Teams;

public sealed record CreateTeamRequest(Guid TeamManagerId, string Name, string? Description);
