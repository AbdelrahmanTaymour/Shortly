using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Mappers;
using Shortly.Core.RepositoryContract.OrganizationManagement;
using Shortly.Core.ServiceContracts.OrganizationManagement;
using Shortly.Core.ServiceContracts.UserManagement;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.Services.OrganizationManagement;

/// <summary>
/// Service implementation for managing organizations and their lifecycle operations.
/// Provides business logic for organization CRUD operations, ownership management, member access control,
/// activation/deactivation workflows, and organization search functionality with comprehensive validation and logging.
/// </summary>
/// <param name="organizationRepository">The repository for organization data operations.</param>
/// <param name="memberRepository">The repository for organization member data operations.</param>
/// <param name="userService">The service for user-related operations and validation.</param>
/// <param name="logger">The logger instance for recording operation details and errors.</param>
public class OrganizationService(
    IOrganizationRepository organizationRepository,
    IOrganizationMemberRepository memberRepository,
    IUserService userService,
    ILogger<OrganizationService> logger) : IOrganizationService
{
    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationDto>> GetAllAsync(int page = 1, int pageSize = 50,CancellationToken cancellationToken = default)
    {
        var organizations = await organizationRepository.GetAllAsync(page, pageSize, cancellationToken);
        return organizations.MapToOrganizationDtos();
    }

    /// <inheritdoc />
    public async Task<OrganizationDto?> GetOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var organization = await organizationRepository.GetByIdAsync(organizationId, cancellationToken);
        if(organization == null)
            throw new NotFoundException("Organization", organizationId);
        
        return organization.MapToOrganizationDto();
    }

    /// <inheritdoc />
    public async Task<OrganizationDto?> GetOrganizationWithDetailsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var organization = await organizationRepository.GetByIdWithFullDetailsAsync(organizationId, cancellationToken);
        if(organization == null)
            throw new NotFoundException("Organization", organizationId);
        
        return organization.MapToOrganizationDto();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationDto>> GetUserOrganizationsAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var organizations = await organizationRepository.GetByOwnerIdAsync(ownerId, cancellationToken);
        return organizations.MapToOrganizationDtos();
    }

    /// <inheritdoc />
    public async Task<OrganizationDto> CreateOrganizationAsync(CreateOrganizationDto dto, Guid ownerId)
    {
        var userExists = await userService.ExistsAsync(ownerId);
        if(!userExists)
            throw new NotFoundException("User", ownerId);

        var organization = new Organization
        {
            Name = dto.Name,
            Description = dto.Description,
            Website = dto.Website,
            LogoUrl = dto.LogoUrl,
            MemberLimit = dto.MemberLimit,
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var createdOrg = await organizationRepository.AddAsync(organization);
        
        // Add an owner as the first member with an OrgOwner role
        await memberRepository.AddAsync(new OrganizationMember
        {
            OrganizationId = createdOrg.Id,
            UserId = createdOrg.OwnerId,
            RoleId = enUserRole.OrgOwner,
            InvitedBy = createdOrg.OwnerId
        });

        logger.LogInformation("Organization {OrganizationName} created by user {UserId}", dto.Name, ownerId);
        return createdOrg.MapToOrganizationDto();
    }

    /// <inheritdoc />
    public async Task<OrganizationDto> UpdateOrganizationAsync(Guid organizationId, UpdateOrganizationDto dto, CancellationToken cancellationToken = default)
    {
        var organization = await organizationRepository.GetByIdAsync(organizationId, cancellationToken);
        if(organization == null)
            throw new NotFoundException("Organization", organizationId);
        
        if (dto.Name != null) organization.Name =  dto.Name;
        if (dto.Description != null) organization.Description = dto.Description;
        if (dto.Website != null) organization.Website = dto.Website;
        if (dto.LogoUrl != null) organization.LogoUrl = dto.LogoUrl;
        if (dto.MemberLimit.HasValue) organization.MemberLimit = dto.MemberLimit.Value;
        if (dto.IsActive.HasValue) organization.IsActive = dto.IsActive.Value;
        if (dto.IsSubscribed.HasValue) organization.IsSubscribed = dto.IsSubscribed.Value;
        
        await organizationRepository.UpdateAsync(organization, cancellationToken);

        return organization.MapToOrganizationDto();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteOrganizationAsync(Guid organizationId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        var isOwner = await organizationRepository.IsOwnerAsync(organizationId, requestingUserId, cancellationToken);
        if(!isOwner)
            throw new ForbiddenException($"The requested user with ID '{requestingUserId}' must be the organization owner to delete it. Or the organization is already deleted. ");
        
        var succeed = await organizationRepository.DeleteAsync(organizationId, cancellationToken);
        logger.LogInformation("Organization {OrganizationId} deleted by user {UserId}", organizationId, requestingUserId);
        return succeed;
    }

    /// <inheritdoc />
    public async Task<bool> ActivateOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var isActivated = await organizationRepository.ActivateOrganizationAsync(organizationId, cancellationToken);
       
        if(!isActivated)
            throw new NotFoundException($"Organization with ID '{organizationId}' was not found or already activated.");
       
        return isActivated;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var isDeactivated = await organizationRepository.DeactivateOrganizationAsync(organizationId, cancellationToken);
        if(!isDeactivated)
            throw new NotFoundException($"Organization with ID '{organizationId}' was not found or deactivated.");
        
        return isDeactivated;
    }

    /// <inheritdoc />
    public async Task<bool> TransferOwnershipAsync(Guid organizationId, Guid currentOwnerId, Guid newOwnerId, CancellationToken cancellationToken = default)
    {
        var isNewOwnerMember = await memberRepository.IsMemberOfOrganizationAsync(newOwnerId, organizationId, cancellationToken);
        if(!isNewOwnerMember)
            throw new ArgumentException($"The new owner with ID '{newOwnerId}' is not a member of organization with ID '{organizationId}'.");
        
        var transferred = await organizationRepository.TransferOwnershipAsync(organizationId, currentOwnerId, newOwnerId, cancellationToken);
        if(!transferred)
            throw new NotFoundException($"Organization with ID '{organizationId}' and owned by '{currentOwnerId}' was not found or deactivated.");
        
        return transferred;
    }

    /// <inheritdoc />
    public async Task<bool> CanUserAccessOrganizationAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await memberRepository.IsMemberOfOrganizationAsync(userId, organizationId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationDto>> SearchOrganizationsAsync(string searchTerm, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var organizations = await organizationRepository.SearchByNameAsync(searchTerm, page, pageSize, cancellationToken);
        return organizations.MapToOrganizationDtos();
    }
}