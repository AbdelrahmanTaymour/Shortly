using Microsoft.EntityFrameworkCore;
using Shortly.Domain.Teams;

namespace Shortly.Infrastructure.Repositories;

internal sealed class OrganizationTeamMemberRepository(ApplicationDbContext dbContext) 
    : Repository<OrganizationTeamMember>(dbContext), IOrganizationTeamMemberRepository
{
    public async Task<OrganizationTeamMember?> GetByTeamAndMemberAsync(Guid teamId, Guid memberId, CancellationToken cancellationToken = default)
        => await DbContext.Set<OrganizationTeamMember>()
            .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.MemberId == memberId, cancellationToken);

    public async Task<bool> IsMemberOfTeamAsync(Guid memberId, Guid teamId, CancellationToken cancellationToken = default)
        => await DbContext.Set<OrganizationTeamMember>()
            .AsNoTracking()
            .AnyAsync(tm => tm.MemberId == memberId && tm.TeamId == teamId, cancellationToken);
}