using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.ServiceContracts.OrganizationManagement;

public interface IOrganizationService
{
    Task<IEnumerable<Organization>> GetAllAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<Organization?> GetOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<Organization?> GetOrganizationWithDetailsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Organization>> GetUserOrganizationsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Organization> CreateOrganizationAsync(CreateOrganizationDto dto);
    Task<Organization> UpdateOrganizationAsync(Guid organizationId, UpdateOrganizationDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteOrganizationAsync(Guid organizationId, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<bool> ActivateOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> DeactivateOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> TransferOwnershipAsync(Guid organizationId, Guid currentOwnerId, Guid newOwnerId, CancellationToken cancellationToken = default);
    Task<bool> CanUserAccessOrganizationAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Organization>> SearchOrganizationsAsync(string searchTerm, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
}