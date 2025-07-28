using Microsoft.AspNetCore.Identity.Data;
using Shortly.Core.DTOs.UsersDTOs;
using Shortly.Core.RepositoryContract;
using Shortly.Core.ServiceContracts;
using Shortly.Core.Mappers;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;
using ForgotPasswordRequest = Shortly.Core.DTOs.UsersDTOs.ForgotPasswordRequest;

namespace Shortly.Core.Services;

public class UserService(IUserRepository userRepository): IUserService
{
    private readonly IUserRepository _userRepository = userRepository;

    // Admin Operations
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAll();
        return users.MapToUserDtoList();
    }
    public async Task<UserDto> GetUserByIdAsync(Guid id)
    {
        var user = await _userRepository.GetUserById(id);
        if (user == null)
            throw new Exception("User not found");
        return user.MapToUserDto();
    }
    public async Task<CreateUserResponse> CreateUserAsync(CreateUserRequest createUserRequest)
    {
        var userExists = await _userRepository
            .IsEmailOrUsernameTaken(createUserRequest.Email, createUserRequest.Username);
        
        if(userExists) 
            throw new Exception("User with the same username or email already exists.");

        var user = new User()
        {
            Name = createUserRequest.Name,
            Email = createUserRequest.Email,
            Username = createUserRequest.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(createUserRequest.Password, 10),
            SubscriptionPlan = createUserRequest.SubscriptionPlan ?? enSubscriptionPlan.Free,
            Role = createUserRequest.Role ?? enUserRole.StandardUser,
            ProfilePictureUrl = createUserRequest.ProfilePictureUrl,
            TimeZone = createUserRequest.TimeZone,
            IsActive = createUserRequest.IsActive
        };

        user = await _userRepository.AddUser(user);
        if(user == null) throw new Exception("Error creating user");
        
        return user.MapToUserCreateResponse();
    }
    public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserDto updateUserDto)
    {
        var user = await _userRepository.GetUserById(userId);
        if(user == null) throw new Exception("User not found");
        
        var isEmailOrUsernameTaken = await _userRepository
            .IsEmailOrUsernameTaken(updateUserDto.Email, updateUserDto.Username);
        
        if(isEmailOrUsernameTaken) 
            throw new Exception("User with the same username or email already exists.");

        user.Name = updateUserDto.Name;
        user.Email = updateUserDto.Email;
        user.Username = updateUserDto.Username;
        user.SubscriptionPlan = updateUserDto.SubscriptionPlan;
        user.Role = updateUserDto.Role;
        user.IsActive = updateUserDto.IsActive;
        user.IsEmailConfirmed = updateUserDto.IsEmailConfirmed;
        user.TimeZone = updateUserDto.TimeZone;
        user.ProfilePictureUrl = updateUserDto.ProfilePictureUrl;
        user.MonthlyLinksCreated = updateUserDto.MonthlyLinksCreated;
        user.TotalLinksCreated = updateUserDto.TotalLinksCreated;
        user.MonthlyResetDate = updateUserDto.MonthlyResetDate;
        user.FailedLoginAttempts = updateUserDto.FailedLoginAttempts;
        user.LockedUntil = updateUserDto.LockedUntil;
        user.UpdatedAt = DateTime.UtcNow;
        
        await _userRepository.UpdateUser(user);
        return user.MapToUserDto();
    }
    public async Task<bool> HardDeleteUserAsync(Guid userId)
    {
        var user = await _userRepository.GetUserById(userId);
        if(user == null) throw new Exception("User not found");
        return await _userRepository.HardDeleteUser(user);
    }

    public async Task<UserSearchResponse> SearchUsers(string? searchTerm,
        enUserRole? role, enSubscriptionPlan? subscriptionPlan, bool? isActive, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var (users, count) = await _userRepository
            .SearchUsers(searchTerm, role, subscriptionPlan, isActive, page, pageSize);

        return new UserSearchResponse
        (
            users,
            count,
            page,
            pageSize,
            (int)Math.Ceiling(count / (double)pageSize)
        );
    }

    // Client management Operations
    public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId)
    {
        var user = await _userRepository.GetUserById(userId);
        if(user == null) throw new Exception("User not found");
        return user.MapToUserProfileDto();
    }
    public Task<UserProfileDto> UpdateUserProfile(Guid userId, UpdateUserProfileRequest updateUserProfileRequest)
    {
        throw new NotImplementedException();
    }
    public async Task<bool> SoftDeleteUserAccount(Guid userId, Guid deletedBy)
    {
        return await _userRepository.SoftDeleteUser(userId, deletedBy);
    }

    // Password management
    public Task<bool> ChangePassword(Guid userId, ChangePasswordRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ForgotPassword(ForgotPasswordRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ResetPassword(ResetPasswordRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ValidateResetCode(string email, string code)
    {
        throw new NotImplementedException();
    }

    // Email verification
    public Task<bool> SendEmailVerification(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> VerifyEmail(EmailVerificationRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ResendEmailVerification(string email)
    {
        throw new NotImplementedException();
    }
    
}