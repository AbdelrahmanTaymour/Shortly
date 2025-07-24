using Shortly.Core.Context;
using Shortly.Core.ServiceContracts;
using Shortly.Domain.Enums;

namespace Shortly.Core.Services;

public class AuthorizationService : IAuthorizationService
{
    public enPermissions GetUserPermissions(Guid userId)
    {
        throw new NotImplementedException();
    }

    public bool HasPermission(Guid userId, enPermissions permission)
    {
        throw new NotImplementedException();
    }

    public bool HasAnyPermission(Guid userId, params enPermissions[] permissions)
    {
        throw new NotImplementedException();
    }

    public bool HasAllPermissions(Guid userId, params enPermissions[] permissions)
    {
        throw new NotImplementedException();
    }

    public bool CheckLimit(Guid userId, string limitType, int currentUsage)
    {
        throw new NotImplementedException();
    }

    public void GrantPermission(Guid userId, enPermissions permission)
    {
        throw new NotImplementedException();
    }

    public void RevokePermission(Guid userId, enPermissions permission)
    {
        throw new NotImplementedException();
    }

    public void ChangeUserRole(Guid userId, enPermissions newRole)
    {
        throw new NotImplementedException();
    }

    public UserContext GetUserContext(Guid userId)
    {
        throw new NotImplementedException();
    }
}