using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.OrganizationManagement;

/// <summary>
/// Defines repository implementation for managing OrganizationTeamMember entities in the database.
/// Provides CRUD operations, team membership queries, and organization team member-specific business logic.
/// </summary>
public interface IOrganizationTeamMemberRepository
{
    /// <summary>
    /// Retrieves all organization team members from the database.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of all organization team members.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<OrganizationTeamMember>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves an organization team member by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the organization team member.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The organization team member if found; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<OrganizationTeamMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves all members of a specific team including member and user details, ordered by join date.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of team members with member and user details ordered by join date.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<OrganizationTeamMember>> GetByTeamIdAsync(Guid teamId, CancellationToken cancellationToken = default);
 
    /// <summary>
    /// Retrieves all team memberships for a specific organization member including team details.
    /// </summary>
    /// <param name="memberId">The unique identifier of the organization member.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of team memberships with team details.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<OrganizationTeamMember>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default);
  
    /// <summary>
    /// Retrieves a specific team membership by team and member identifiers.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team.</param>
    /// <param name="memberId">The unique identifier of the organization member.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The team membership if found; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<OrganizationTeamMember?> GetByTeamAndMemberAsync(Guid teamId, Guid memberId, CancellationToken cancellationToken = default);
  
    /// <summary>
    /// Retrieves all teams that a specific organization member belongs to.
    /// </summary>
    /// <param name="memberId">The unique identifier of the organization member.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of teams that the member belongs to.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<OrganizationTeam>> GetTeamsByMemberAsync(Guid memberId, CancellationToken cancellationToken = default);
  
    /// <summary>
    /// Adds a new organization team member to the database.
    /// </summary>
    /// <param name="entity">The organization team member to add.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The added organization team member with any database-generated values.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<OrganizationTeamMember> AddAsync(OrganizationTeamMember entity, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Updates an existing organization team member in the database.
    /// </summary>
    /// <param name="entity">The organization team member with updated values.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> UpdateAsync(OrganizationTeamMember entity, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Permanently deletes an organization team member from the database by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the organization team member to delete.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the deletion was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
  
    /// <summary>
    /// Removes a specific member from a team by deleting their team membership record.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team.</param>
    /// <param name="memberId">The unique identifier of the organization member to remove.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the member was successfully removed from the team; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> RemoveFromTeamAsync(Guid teamId, Guid memberId, CancellationToken cancellationToken = default);
  
    /// <summary>
    /// Checks if an organization team member exists in the database.
    /// </summary>
    /// <param name="id">The unique identifier of the organization team member to check.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the organization team member exists; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Checks if a specific organization member belongs to a specific team.
    /// </summary>
    /// <param name="memberId">The unique identifier of the organization member.</param>
    /// <param name="teamId">The unique identifier of the team.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the member belongs to the team; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> IsMemberOfTeamAsync(Guid memberId, Guid teamId, CancellationToken cancellationToken = default);
}