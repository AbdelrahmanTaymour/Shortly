using Microsoft.EntityFrameworkCore;
using Shortly.Domain.Members;

namespace Shortly.Infrastructure.Repositories;

internal sealed class OrganizationMemberRepository(ApplicationDbContext dbContext) 
    : Repository<OrganizationMember, Guid>(dbContext), IOrganizationMemberRepository
{
    public async Task<OrganizationMember?> GetByOrganizationAndUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
        => await DbContext.Set<OrganizationMember>()
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId, cancellationToken);

    public async Task<bool> IsMemberOfOrganizationAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default)
        => await DbContext.Set<OrganizationMember>()
            .AsNoTracking()
            .AnyAsync(m => m.UserId == userId && m.OrganizationId == organizationId && m.IsActive, cancellationToken);

    public async Task<int> GetMemberCountByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => await DbContext.Set<OrganizationMember>()
            .AsNoTracking()
            .CountAsync(m => m.OrganizationId == organizationId && m.IsActive, cancellationToken);
}