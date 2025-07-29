using Shortly.Core.DTOs.UsersDTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract;

public interface IUserRepository
{
    #region Admin

    Task<IEnumerable<User>> GetAll();
    Task<User?> GetUserById(Guid userId);
    Task<User?> GetActiveUserByEmail(string? email);
    Task<User?> GetActiveUserByUsername(string? username);
    Task<User?> GetActiveUserByEmailAndPassword(string? email, string? password);
    Task<User?> AddUser(User user);
    Task<User?> UpdateUser(User user);
    Task<bool> HardDeleteUser(Guid userId);
    Task<bool> SoftDeleteUser(Guid userId, Guid deletedBy);
    Task<bool> LockUser(Guid userId, DateTime? lockUntil);
    Task<bool> UnlockUser(Guid userId);
    Task<bool> ActivateUser(Guid userId);
    Task<bool> DeactivateUser(Guid userId);
    Task<bool> IsEmailOrUsernameTaken(string email, string username);
    Task<UserAvailabilityInfo?> GetUserAvailabilityInfo(Guid userId);
    
    // Search and pagination
    Task<(IEnumerable<UserViewDto> Users, int TotalCount)> SearchUsers(
        string? searchTerm, 
        Domain.Enums.enUserRole? role, 
        Domain.Enums.enSubscriptionPlan? subscriptionPlan,
        bool? isActive,
        int page, 
        int pageSize);
    
    #endregion
    
}