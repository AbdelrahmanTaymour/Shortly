using Shortly.Domain.Abstractions;

namespace Shortly.Domain.Teams;

public sealed class OrganizationTeamMember : Entity<Guid>
{
    private OrganizationTeamMember(Guid id, Guid teamId, Guid memberId, DateTime utcNow) : base(id)
    {
        TeamId = teamId;
        MemberId = memberId;
        JoinedAt = utcNow;
    }

    private OrganizationTeamMember() { } // EF

    public Guid TeamId { get; private set; }
    public Guid MemberId { get; private set; }
    public DateTime JoinedAt { get; private set; }

    public static OrganizationTeamMember Create(Guid teamId, Guid memberId, DateTime utcNow)
        => new(Guid.NewGuid(), teamId, memberId, utcNow);
}