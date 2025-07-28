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
    Task<UserSearchResponse> SearchUsers(
        string? searchTerm,
        enUserRole? role,
        enSubscriptionPlan? subscriptionPlan,
        bool? isActive,
        int page,
        int pageSize);
    
    // Client management Operations
    Task<UserProfileDto?> GetUserProfileAsync(Guid userId);
    Task<UserProfileDto> UpdateUserProfile(Guid userId, UpdateUserProfileRequest updateUserProfileRequest);
    Task<bool> SoftDeleteUserAccount(Guid userId, Guid deletedBy);
    
    // Password management
    Task<bool> ChangePassword(Guid userId, ChangePasswordRequest request);
    Task<bool> ForgotPassword(ForgotPasswordRequest request);
    Task<bool> ResetPassword(ResetPasswordRequest request);
    Task<bool> ValidateResetCode(string email, string code);
    
    // Email verification
    Task<bool> SendEmailVerification(Guid userId);
    Task<bool> VerifyEmail(EmailVerificationRequest request);
    Task<bool> ResendEmailVerification(string email);
    
    
    
    
}