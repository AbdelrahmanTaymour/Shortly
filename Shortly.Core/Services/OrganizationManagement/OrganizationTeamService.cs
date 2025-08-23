using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Mappers;
using Shortly.Core.RepositoryContract.OrganizationManagement;
using Shortly.Core.ServiceContracts.OrganizationManagement;
using Shortly.Domain.Entities;

namespace Shortly.Core.Services.OrganizationManagement;

/// <summary>
/// Service for managing organization teams, including creating, updating, deleting teams and managing team memberships.
/// </summary>
/// <param name="teamRepository">Repository for organization team data operations.</param>
/// <param name="teamMemberRepository">Repository for team member data operations.</param>
/// <param name="memberRepository">Repository for organization member data operations.</param>
/// <param name="logger">Logger for recording service operations and events.</param>
public class OrganizationTeamService(
    IOrganizationTeamRepository teamRepository,
    IOrganizationTeamMemberRepository teamMemberRepository,
    IOrganizationMemberRepository memberRepository,
    ILogger<OrganizationTeamService> logger) : IOrganizationTeamService
{
    /// <inheritdoc />
    public async Task<OrganizationTeamDto> GetTeamAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var team = await teamRepository.GetByIdAsync(teamId);
        if(team == null)
            throw new NotFoundException("Team", teamId);
        
        return team.MapToOrganizationTeamDto();
    }

    /// <inheritdoc />
    public async Task<OrganizationTeamDto> GetTeamWithMembersAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var team = await teamRepository.GetByIdWithMembersAsync(teamId, cancellationToken);
        if (team == null)
            throw new NotFoundException("Team", teamId);
        return team.MapToOrganizationTeamDto();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationTeamDto>> GetOrganizationTeamsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var teams = await teamRepository.GetByOrganizationIdAsync(organizationId, cancellationToken);
        return teams.MapToOrganizationTeamDtos();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationTeamMemberDto>> GetTeamMembersAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var teamMembers = await teamMemberRepository.GetByTeamIdAsync(teamId, cancellationToken);
        return teamMembers.MapToOrganizationTeamMemberDtos();
    }

    /// <inheritdoc />
    public async Task<OrganizationTeamDto> CreateTeamAsync(CreateTeamRequest request)
    {
        // Validate team manager is a member of the organization
        var isManagerMember = await memberRepository.ExistsAsync(request.TeamManagerId);
        
        if(!isManagerMember)
            throw new BusinessRuleException("Team manager must be a member of the organization.");
        
        // Check for duplicate team name in the organization
        var existingTeamName = await teamRepository.IsTeamNameExistAsync(request.Name, request.OrganizationId);
        if(existingTeamName)
            throw new ConflictException("Team", "name");

        // Create New Team
        var team = new OrganizationTeam
        {
            OrganizationId = request.OrganizationId,
            TeamManagerId = request.TeamManagerId,
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };
        var createdTeam = await teamRepository.AddAsync(team);
        
        // Add team manager as first team member
        await teamMemberRepository.AddAsync(new OrganizationTeamMember
        {
            TeamId = createdTeam.Id,
            MemberId = createdTeam.TeamManagerId,
            JoinedAt = DateTime.UtcNow
        });
        
        logger.LogInformation("Team {TeamName} created in organization {OrganizationId}", request.Name, request.OrganizationId);
        return createdTeam.MapToOrganizationTeamDto();
    }

    /// <inheritdoc />
    public async Task<OrganizationTeamMemberDto> AddMemberToTeamAsync(Guid teamId, Guid memberId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        // Check if the team exists
        var team = await teamRepository.GetByIdAsync(teamId);
        if(team == null)
            throw new NotFoundException("Team", teamId);
        
        // check if the member exists in the organization
        var isMember = await memberRepository.ExistsAsync(memberId, team.OrganizationId, cancellationToken);
        if(!isMember)
            throw new BusinessRuleException("Member must be a member of the organization.");
        
        // Check if the member is already in the team
        var isTeamMember = await teamMemberRepository.IsMemberOfTeamAsync(memberId, teamId, cancellationToken);
        if(isMember)
            throw new ConflictException("Member is already a member of this team");

        var createdTeamMember = await teamMemberRepository.AddAsync(new OrganizationTeamMember
        {
            TeamId = teamId,
            MemberId = memberId,
            JoinedAt = DateTime.UtcNow
        }, cancellationToken);
        
        return createdTeamMember.MapToOrganizationTeamMemberDto();
    }

    /// <inheritdoc />
    public async Task<bool> UpdateTeamAsync(Guid teamId, UpdateTeamRequest request, Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        var team = await teamRepository.GetByIdAsync(teamId);
        if(team == null)
            throw new NotFoundException("Team", teamId);
        
        if (request.Name != null) team.Name = request.Name;
        if (request.Description != null) team.Description = request.Description;
        
        return await teamRepository.UpdateAsync(team, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTeamAsync(Guid teamId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        var deleted = await teamRepository.DeleteAsync(teamId, cancellationToken);
        if(!deleted)
            throw new NotFoundException("Team", teamId);
        
        logger.LogInformation("Team {TeamId} deleted by user {UserId}", teamId, requestingUserId);
        return deleted;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveMemberFromTeamAsync(Guid teamId, Guid memberId, Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        var deleted = await teamMemberRepository.RemoveFromTeamAsync(teamId, memberId, cancellationToken);
        if(!deleted)
            throw new NotFoundException("TeamMember", memberId);
        
        logger.LogInformation("Member {MemberId} removed from team {TeamId} by user {UserId}", memberId, teamId, requestingUserId);
        return deleted;
    }

    /// <inheritdoc />
    public async Task<bool> ChangeTeamManagerAsync(Guid teamId, Guid newManagerId, Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        var team = await teamRepository.GetByIdAsync(teamId);
        if(team == null)
            throw new NotFoundException("Team", teamId);
        
        // Check if the new manager is a member of the organization
        var isManagerMember =
            await memberRepository.IsMemberOfOrganizationAsync(newManagerId, team.OrganizationId, cancellationToken);
        
        if(!isManagerMember)
            throw new BusinessRuleException("New team manager must be a member of the organization.");
        
        team.TeamManagerId = newManagerId;
        return await teamRepository.UpdateAsync(team, cancellationToken);
    }
}