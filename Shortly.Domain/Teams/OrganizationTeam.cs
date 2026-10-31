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

    public static OrganizationTeam Create(Guid organizationId, Guid teamManagerId, string name, string? description, DateTime utcNow)
        => new(Guid.NewGuid(), organizationId, teamManagerId, name, description, utcNow);

    public void Rename(string name) => Name = name;

    public void UpdateDescription(string? description) => Description = description;

    public void ChangeManager(Guid newManagerId) => TeamManagerId = newManagerId;
}