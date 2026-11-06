using Microsoft.EntityFrameworkCore;
using Shortly.Domain.Organizations;

namespace Shortly.Infrastructure.Repositories;

internal sealed class OrganizationRepository(ApplicationDbContext dbContext) 
    : Repository<Organization, Guid>(dbContext), IOrganizationRepository
{
    public new async Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbContext.Set<Organization>()
            .Include(o => o.Usage)
            .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null, cancellationToken);
    
    public async Task<int> CountOwnedByAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbContext.Set<Organization>()
            .AsNoTracking()
            .CountAsync(o => o.OwnerId == userId && o.DeletedAt == null, cancellationToken);

    public async Task<bool> IsOwnerAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
        => await DbContext.Set<Organization>()
            .AsNoTracking()
            .AnyAsync(o => o.Id == organizationId && o.OwnerId == userId && o.DeletedAt == null, cancellationToken);
}