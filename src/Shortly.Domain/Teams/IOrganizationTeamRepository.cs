namespace Shortly.Domain.Teams;

public interface IOrganizationTeamRepository
{
    Task<OrganizationTeam?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsTeamNameExistAsync(string name, Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> IsTeamManagerAsync(Guid teamId, Guid managerId, CancellationToken cancellationToken = default);
    void Add(OrganizationTeam team);
    void Remove(OrganizationTeam team);
}