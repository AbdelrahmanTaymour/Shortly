using Shortly.Core.Exceptions.ServerErrors;

namespace Shortly.Core.RepositoryContract.OrganizationManagement;

public interface IOrganizationRepository
{
    /// <summary>
    /// Determines whether the specified user is the owner of any organization.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to check.</param>
    /// <returns>
    /// <c>true</c> if the user owns at least one organization; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="DatabaseException">
    /// Thrown when an error occurs while accessing the database.
    /// </exception>
    Task<bool> IsUserOwnerOfAnyOrganization(Guid userId);
}