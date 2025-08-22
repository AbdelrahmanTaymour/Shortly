using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Mappers;
using Shortly.Core.RepositoryContract.OrganizationManagement;
using Shortly.Core.ServiceContracts.OrganizationManagement;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.Services.OrganizationManagement;

/// <summary>
/// Service for managing organization members, including adding, removing, and updating member roles and permissions.
/// </summary>
/// <param name="memberRepository">Repository for organization member data operations.</param>
/// <param name="organizationRepository">Repository for organization data operations.</param>
/// <param name="logger">Logger for recording service operations and events.</param>
public class OrganizationMemberService(
    IOrganizationMemberRepository memberRepository,
    IOrganizationRepository organizationRepository,
    ILogger<OrganizationMemberService> logger) : IOrganizationMemberService
{
    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationMemberDto>> GetAllMembersAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var members = await memberRepository.GetAllAsync(page, pageSize, cancellationToken);
        return members.MapToOrganizationMemberDtos();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationMemberDto>> GetOrganizationMembersAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var members = await memberRepository.GetByOrganizationIdAsync(organizationId, cancellationToken);
        return members.MapToOrganizationMemberDtos();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationMemberDto>> GetUserMembershipsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var members = await memberRepository.GetByUserIdAsync(userId, cancellationToken);
        return members.MapToOrganizationMemberDtos();
    }

    /// <inheritdoc />
    public async Task<OrganizationMemberDto> GetMembershipAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var members = await memberRepository.GetByOrganizationAndUserAsync(organizationId, userId, cancellationToken);
        if(members == null)
            throw new NotFoundException($"Member with '{userId}' not found for organization '{organizationId}'");
        
        return members.MapToOrganizationMemberDto();
    }

    /// <inheritdoc />
    public async Task<OrganizationMemberDto> AddMemberAsync(CreateMemberRequest request, CancellationToken cancellationToken = default)
    {
        // Validate organization exists and has capacity
        var org = await organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);
        if(org == null)
            throw new NotFoundException("Organization", request.OrganizationId);

        // Check if the org reached the member limits
        var currentMemberCount =
            await memberRepository.GetMemberCountByOrganizationAsync(request.OrganizationId, cancellationToken);
        
        if(currentMemberCount >= org.MemberLimit)
            throw new BusinessRuleException("Organization has reached member limit.");

        // Check if a user is already a member
        var existingMember = await memberRepository.IsMemberOfOrganizationAsync(request.UserId, request.OrganizationId, cancellationToken);
        if (existingMember)
            throw new InvalidOperationException("User is already a member of this organization");
        
        var member = new OrganizationMember
        {
            OrganizationId = request.OrganizationId,
            UserId = request.UserId,
            RoleId = request.RoleId,
            InvitedBy = request.InvitedBy,
            JoinedAt = DateTime.UtcNow
        };
        
        var created = await memberRepository.AddAsync(member, cancellationToken);
        logger.LogInformation("User {UserId} added to organization {OrganizationId} with role {RoleId}", request.UserId, request.OrganizationId, request.RoleId);
        return created.MapToOrganizationMemberDto();
    }

    /// <inheritdoc />
    public async Task<bool> RemoveMemberAsync(Guid organizationId, Guid userId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        // Don't allow removing the owner
        var isOwner = await organizationRepository.IsOwnerAsync(organizationId, userId, cancellationToken);
        if(isOwner)
            throw new BusinessRuleException("Organization owner cannot be removed.");
        
        var removed = await memberRepository.RemoveMemberAsync(organizationId, userId, cancellationToken);
        if(!removed)
            throw new NotFoundException($"Member with '{userId}' not found for organization '{organizationId}'");
        
        logger.LogInformation("User {UserId} removed from organization {OrganizationId} by {RequestingUserId}",
            userId, organizationId, requestingUserId);
        return removed;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateMemberRoleAsync(Guid organizationId, Guid userId, enUserRole newRoleId, CancellationToken cancellationToken = default)
    {
        var isOwner = await organizationRepository.IsOwnerAsync(organizationId, userId, cancellationToken);
        if(isOwner)
            throw new BusinessRuleException("Can not change the owner role.");
        
        var updated = await memberRepository.UpdateMemberRoleAsync(organizationId, userId, newRoleId, cancellationToken);
        if(!updated)
            throw new NotFoundException("OrganizationMemberDto", userId);
        
        logger.LogInformation("User {UserId} updated role of organization {OrganizationId} to {NewRoleId}", userId, organizationId, newRoleId);
        return updated;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateMemberPermissionsAsync(Guid organizationId, Guid userId, enPermissions permissions, CancellationToken cancellationToken = default)
    {
        var updated = await memberRepository.UpdateMemberPermissionsAsync(organizationId, userId, permissions, cancellationToken);
        if(!updated)
            throw new NotFoundException("OrganizationMemberDto", userId);
        
        logger.LogInformation("User {UserId} updated permissions of organization {OrganizationId} to {NewPermissions}", userId, organizationId, permissions);
        return updated;
    }

    /// <inheritdoc />
    public async Task<bool> IsMemberAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await memberRepository.IsMemberOfOrganizationAsync(userId, organizationId, cancellationToken);
    }
}