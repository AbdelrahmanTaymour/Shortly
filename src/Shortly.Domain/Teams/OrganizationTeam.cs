using Shortly.Domain.Abstractions;

namespace Shortly.Domain.Teams;

public sealed class OrganizationTeam : Entity<Guid>
{
    private OrganizationTeam(Guid id, Guid organizationId, Guid teamManagerId, string name, string? description, DateTime utcNow) : base(id)
    {
        OrganizationId = organizationId;
        TeamManagerId = teamManagerId;
        Name = name;
        Description = description;
        CreatedAt = utcNow;
    }

    private OrganizationTeam() { } // EF

    public Guid OrganizationId { get; private set; }
    public Guid TeamManagerId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Result<OrganizationTeam> Create(Guid organizationId, Guid teamManagerId, string name, string? description, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(name)) return Result.Failure<OrganizationTeam>(OrganizationTeamErrors.NameRequired);
        if (teamManagerId == Guid.Empty) return Result.Failure<OrganizationTeam>(OrganizationTeamErrors.ManagerRequired);

        return new OrganizationTeam(Guid.NewGuid(), organizationId, teamManagerId, name, description, utcNow);
    }

    public Result Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return Result.Failure(OrganizationTeamErrors.NameRequired);

        Name = name;
        return Result.Success();
    }

    public void UpdateDescription(string? description) => Description = description;

    public Result ChangeManager(Guid newManagerId)
    {
        if (newManagerId == Guid.Empty) return Result.Failure(OrganizationTeamErrors.ManagerRequired);

        TeamManagerId = newManagerId;
        return Result.Success();
    }
}