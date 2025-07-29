using Microsoft.AspNetCore.Identity.Data;
using Shortly.Core.DTOs.UsersDTOs;
using Shortly.Domain.Enums;
using ForgotPasswordRequest = Shortly.Core.DTOs.UsersDTOs.ForgotPasswordRequest;

namespace Shortly.Core.ServiceContracts;

public interface IUserService
{
    // Admin Operations
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto> GetUserByIdAsync(Guid id);
    Task<CreateUserResponse> CreateUserAsync(CreateUserRequest createUserRequest);
    Task<UserDto> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto);
    Task<bool> HardDeleteUserAsync(Guid id);
    Task<bool> SoftDeleteUserAccount(Guid userId, Guid deletedBy);
    Task<bool> LockUser(Guid userId, DateTime? lockUntil);
    Task<bool> UnlockUser(Guid userId);
    Task<bool> ActivateUser(Guid userId);
    Task<bool> DeactivateUser(Guid userId);
    Task<UserAvailabilityInfo?> GetUserAvailabilityInfo(Guid userId);
    Task<UserSearchResponse> SearchUsers(
        string? searchTerm,
        enUserRole? role,
        enSubscriptionPlan? subscriptionPlan,
        bool? isActive,
        int page,
        int pageSize);
    
    
    
}