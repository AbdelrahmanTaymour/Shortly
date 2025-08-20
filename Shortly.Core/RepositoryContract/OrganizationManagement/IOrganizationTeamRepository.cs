using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.OrganizationManagement;

/// <summary>
/// Defins repository for managing OrganizationTeam entities in the database.
/// Provides CRUD operations, team queries, and organization team-specific business logic.
/// </summary>
public interface IOrganizationTeamRepository
{
    /// <summary>
    /// Retrieves all organization teams from the database.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of all organization teams.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<OrganizationTeam>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves an organization team by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the organization team.</param>
    /// <returns>The organization team if found; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<OrganizationTeam?> GetByIdAsync(Guid id);
   
    /// <summary>
    /// Retrieves all teams belonging to a specific organization, including team manager details.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of organization teams ordered by name with team manager details.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<OrganizationTeam>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves all teams managed by a specific team manager.
    /// </summary>
    /// <param name="managerId">The unique identifier of the team manager.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of teams managed by the specified manager.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<OrganizationTeam>> GetManagedTeamsAsync(Guid managerId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves an organization team by its unique identifier, including all team members and their user details.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The organization team with members and team manager details if found; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<OrganizationTeam?> GetByIdWithMembersAsync(Guid teamId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves an organization team by its name within a specific organization.
    /// </summary>
    /// <param name="name">The name of the team to retrieve.</param>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The organization team if found; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<OrganizationTeam?> GetByNameAndOrganizationAsync(string name, Guid organizationId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Gets the total count of members in a specific team.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The number of members in the team.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<int> GetTeamMemberCountAsync(Guid teamId, CancellationToken cancellationToken = default); 
   
    /// <summary>
    /// Adds a new organization team to the database.
    /// </summary>
    /// <param name="entity">The organization team to add.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The added organization team with any database-generated values.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<OrganizationTeam> AddAsync(OrganizationTeam entity, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Updates an existing organization team in the database.
    /// </summary>
    /// <param name="entity">The organization team with updated values.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> UpdateAsync(OrganizationTeam entity, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Permanently deletes an organization team from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the organization team to delete.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the deletion was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Checks if an organization team exists in the database.
    /// </summary>
    /// <param name="id">The unique identifier of the organization team to check.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the organization team exists; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
  
    /// <summary>
    /// Checks if a user is the manager of a specific team.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team.</param>
    /// <param name="managerId">The unique identifier of the potential manager.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the user is the team manager; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> IsTeamManagerAsync(Guid teamId, Guid managerId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if the name of the team already taken within a specific organization.
    /// </summary>
    /// <param name="name">The name of the team to retrieve.</param>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the user is the team name exists; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> IsTeamNameExistAsync(string name, Guid organizationId, CancellationToken cancellationToken = default);
}