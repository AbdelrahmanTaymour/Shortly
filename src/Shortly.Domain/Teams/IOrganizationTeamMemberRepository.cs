namespace Shortly.Domain.Teams;

public interface IOrganizationTeamMemberRepository
{
    Task<OrganizationTeamMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrganizationTeamMember?> GetByTeamAndMemberAsync(Guid teamId, Guid memberId, CancellationToken cancellationToken = default);
    Task<bool> IsMemberOfTeamAsync(Guid memberId, Guid teamId, CancellationToken cancellationToken = default);
    void Add(OrganizationTeamMember teamMember);
    void Remove(OrganizationTeamMember teamMember);
}