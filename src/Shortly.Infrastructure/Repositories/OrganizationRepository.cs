using Microsoft.EntityFrameworkCore;
using Shortly.Domain.Organizations;

namespace Shortly.Infrastructure.Repositories;

internal sealed class OrganizationRepository(ApplicationDbContext dbContext) 
    :Repository<Organization, Guid>(dbContext), IOrganizationRepository
{
    public new async Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbContext.Set<Organization>()
            .Include(o => o.Usage)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);
    
    public async Task<bool> IsOwnerAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
        => await DbContext.Set<Organization>()
            .AsNoTracking()
            .AnyAsync(o => o.Id == organizationId && o.OwnerId == userId && !o.IsDeleted, cancellationToken);
}