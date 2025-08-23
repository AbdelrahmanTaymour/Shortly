using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;

namespace Shortly.Core.ServiceContracts.OrganizationManagement;

/// <summary>
/// Defins service for managing organization teams, including creating, updating, deleting teams and managing team memberships.
/// </summary>
public interface IOrganizationTeamService
{
    /// <summary>
    /// Retrieves a specific team by its unique identifier.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The organization team if found.</returns>
    /// <exception cref="NotFoundException">Thrown when the team is not found.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<OrganizationTeamDto> GetTeamAsync(Guid teamId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves a specific team along with its members by the team's unique identifier.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The organization team with its members if found.</returns>
    /// <exception cref="NotFoundException">Thrown when the team is not found.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<OrganizationTeamDto> GetTeamWithMembersAsync(Guid teamId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves all teams belonging to a specific organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A collection of teams in the specified organization.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<IEnumerable<OrganizationTeamDto>> GetOrganizationTeamsAsync(Guid organizationId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves all members of a specific team.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A collection of team members in the specified team.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<IEnumerable<OrganizationTeamMemberDto>> GetTeamMembersAsync(Guid teamId, CancellationToken cancellationToken = default);
  
    /// <summary>
    /// Creates a new team within an organization and automatically adds the team manager as the first member.
    /// </summary>
    /// <param name="request">Data transfer object containing team creation information.</param>
    /// <returns>The newly created organization team.</returns>
    /// <exception cref="BusinessRuleException">Thrown when the team manager is not a member of the organization.</exception>
    /// <exception cref="ConflictException">Thrown when a team with the same name already exists in the organization.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<OrganizationTeamDto> CreateTeamAsync(CreateTeamRequest request);
   
    /// <summary>
    /// Adds a member to an existing team.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team.</param>
    /// <param name="memberId">The unique identifier of the member to add.</param>
    /// <param name="requestingUserId">The unique identifier of the user requesting the addition.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The added team member dto.</returns>
    /// <exception cref="ConflictException">Thrown when the member is already a member of the team.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<OrganizationTeamMemberDto> AddMemberToTeamAsync(Guid teamId, Guid memberId, Guid requestingUserId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Updates the name and/or description of an existing team.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team to update.</param>
    /// <param name="request">The new data of the team.</param>
    /// <param name="requestingUserId">The unique identifier of the user requesting the update.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the team was successfully updated, false otherwise.</returns>
    /// <exception cref="NotFoundException">Thrown when the team is not found.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<bool> UpdateTeamAsync(Guid teamId, UpdateTeamRequest request, Guid requestingUserId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Deletes a team from the organization.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team to delete.</param>
    /// <param name="requestingUserId">The unique identifier of the user requesting the deletion.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the team was successfully deleted, false otherwise.</returns>
    /// <exception cref="NotFoundException">Thrown when the team is not found.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<bool> DeleteTeamAsync(Guid teamId, Guid requestingUserId, CancellationToken cancellationToken = default);
  
    /// <summary>
    /// Removes a member from a team.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team.</param>
    /// <param name="memberId">The unique identifier of the member to remove.</param>
    /// <param name="requestingUserId">The unique identifier of the user requesting the removal.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the member was successfully removed from the team, false otherwise.</returns>
    /// <exception cref="NotFoundException">Thrown when the team member is not found.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<bool> RemoveMemberFromTeamAsync(Guid teamId, Guid memberId, Guid requestingUserId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Changes the manager of a team to a new organization member.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team.</param>
    /// <param name="newManagerId">The unique identifier of the new team manager.</param>
    /// <param name="requestingUserId">The unique identifier of the user requesting the change.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the team manager was successfully changed, false otherwise.</returns>
    /// <exception cref="NotFoundException">Thrown when the team is not found.</exception>
    /// <exception cref="BusinessRuleException">Thrown when the new manager is not a member of the organization.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<bool> ChangeTeamManagerAsync(Guid teamId, Guid newManagerId, Guid requestingUserId, CancellationToken cancellationToken = default);
}