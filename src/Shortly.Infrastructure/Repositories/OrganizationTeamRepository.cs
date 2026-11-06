using Microsoft.EntityFrameworkCore;
using Shortly.Domain.Teams;

namespace Shortly.Infrastructure.Repositories;

internal sealed class OrganizationTeamRepository(ApplicationDbContext dbContext) 
    : Repository<OrganizationTeam, Guid>(dbContext), IOrganizationTeamRepository
{
    public async Task<bool> IsTeamNameExistAsync(
        string name, Guid organizationId, Guid? excludeTeamId = null, CancellationToken cancellationToken = default)
        => await DbContext.Set<OrganizationTeam>()
            .AsNoTracking()
            .AnyAsync(t => t.Name == name && t.OrganizationId == organizationId
                           && (excludeTeamId == null || t.Id != excludeTeamId), cancellationToken);

    public async Task<bool> IsManagerOfAnyTeamAsync(Guid memberId, CancellationToken cancellationToken = default)
        => await DbContext.Set<OrganizationTeam>()
            .AsNoTracking()
            .AnyAsync(t => t.TeamManagerId == memberId, cancellationToken);

    public async Task<bool> IsTeamManagerAsync(Guid teamId, Guid managerId, CancellationToken cancellationToken = default)
        => await DbContext.Set<OrganizationTeam>()
            .AsNoTracking()
            .AnyAsync(t => t.Id == teamId && t.TeamManagerId == managerId, cancellationToken);
}