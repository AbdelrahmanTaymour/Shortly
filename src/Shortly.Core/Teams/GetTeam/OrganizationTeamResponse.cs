namespace Shortly.Core.Teams.GetTeam;

public sealed class OrganizationTeamResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid TeamManagerMembershipId { get; init; }

    public required string TeamManagerUsername { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public DateTime CreatedAt { get; init; }

    public int MemberCount { get; init; }
}