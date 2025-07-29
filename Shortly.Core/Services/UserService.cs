using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.UsersDTOs;
using Shortly.Core.RepositoryContract;
using Shortly.Core.ServiceContracts;
using Shortly.Core.Mappers;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.Services;

public class UserService(IUserRepository userRepository, ILogger<UserService> logger): IUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ILogger<UserService> _logger = logger;

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
        if(user == null) 
            throw new Exception("Error creating user");
        
        _logger.LogInformation("User created successfully. UserId: {UserId}", user.Id);
        
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
        _logger.LogInformation("User updated successfully. UserId: {UserId}, Role: {Role}, " +
                               "SubscriptionPlan: {SubscriptionPlan}", userId, user.Role, user.SubscriptionPlan);
        return user.MapToUserDto();
    }
    public async Task<bool> HardDeleteUserAsync(Guid userId)
    {
        var isDeleteSucceed = await _userRepository.HardDeleteUser(userId);
        
        if (!isDeleteSucceed) 
            throw new Exception("User not found");
        
        _logger.LogWarning("User hard deleted. UserId: {UserId}", userId);
        
        return true;
    }
    public async Task<bool> SoftDeleteUserAccount(Guid userId, Guid deletedBy)
    {
        var success = await _userRepository.SoftDeleteUser(userId, deletedBy);
        if(success) 
            _logger.LogInformation("User soft deleted. UserId: {UserId}, DeletedBy: {DeletedBy}", userId, deletedBy);
        return success;
    }

    public async Task<bool> LockUser(Guid userId, DateTime? lockUntil)
    {
        var success = await _userRepository.LockUser(userId, lockUntil);
        if(!success)
            throw new Exception("User not found");
        
        _logger.LogWarning("User account with UserId: {UserId} Locked Until: {LockUntil}", userId, lockUntil);

        return success;
    }

    public async Task<bool> UnlockUser(Guid userId)
    {
        var success = await _userRepository.UnlockUser(userId);
        
        if(!success)
            throw new Exception("User not found");
        
        _logger.LogInformation("User account with UserId: {UserId}, Unlocked", userId);
        
        return success;
    }

    public async Task<bool> ActivateUser(Guid userId)
    {
        var success = await _userRepository.ActivateUser(userId);
        
        if(!success)
            throw new Exception("User not found");
        
        _logger.LogInformation("User account activated. UserId: {UserId}", userId);
        
        return success;
    }

    public async Task<bool> DeactivateUser(Guid userId)
    {
        var success = await _userRepository.DeactivateUser(userId);
        
        if(!success)
            throw new Exception("User not found");
        
        _logger.LogInformation("User account deactivated. UserId: {UserId}", userId);
        
        return success;
    }

    public async Task<UserAvailabilityInfo?> GetUserAvailabilityInfo(Guid userId)
    {
        var userAvailability = await _userRepository.GetUserAvailabilityInfo(userId);
        
        if(userAvailability == null)
            throw new Exception("User not found");
        
        return userAvailability;
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

    
    
}