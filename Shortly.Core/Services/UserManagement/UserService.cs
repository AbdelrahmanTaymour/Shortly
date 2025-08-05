using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.Mappers;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Core.ServiceContracts.UserManagement;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.Services.UserManagement;

/// <summary>
/// Provides core business logic for managing user accounts, including creation, updates,
/// activation/deactivation, availability checks, and soft deletion.
/// </summary>
public class UserService(IUserRepository userRepository, ILogger<UserService> logger) : IUserService
{
    /// <inheritdoc/>
    public async Task<UserDto> GetByIdAsync(Guid userId)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new NotFoundException("User", userId);
        return user.MapToUserDto();
    }

    /// <inheritdoc/>
    public async Task<UserDto> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (user == null)
            throw new NotFoundException("User", email);
        return user.MapToUserDto();
    }

    /// <inheritdoc/>
    public async Task<UserDto> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByUsernameAsync(username, cancellationToken);
        if (user == null)
            throw new NotFoundException("User", username);
        return user.MapToUserDto();
    }

    /// <inheritdoc/>
    public async Task<CreateUserResponse> CreateAsync(CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var userExists = await userRepository
            .EmailOrUsernameExistsAsync(request.Email, request.Username, cancellationToken);

        if (userExists)
            throw new ConflictException("Email or username is already taken.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 10),
            SubscriptionPlanId = enSubscriptionPlan.Free,
            IsActive = true
        };

        user = await userRepository.CreateAsync(user);
        if (user == null)
            throw new DatabaseException("Failed to create user due to internal error.");

        logger.LogInformation("User created successfully. UserId: {UserId}", user.Id);

        return user.MapToCreateUserResponse();
    }

    /// <inheritdoc/>
    public async Task<UserDto> UpdateAsync(Guid userId, UpdateUserDto dto)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new NotFoundException("User", userId);

        var isEmailOrUsernameTaken = await userRepository
            .EmailOrUsernameExistsAsync(dto.Email, dto.Username);

        if (isEmailOrUsernameTaken)
            throw new ConflictException("Email or username is already taken.");

        // Update user properties
        user.Email = dto.Email;
        user.Username = dto.Username;
        user.SubscriptionPlanId = dto.SubscriptionPlanId;
        user.Permissions = dto.Permissions;
        user.IsActive = dto.IsActive;
        user.IsEmailConfirmed = dto.IsEmailConfirmed;
        user.UpdatedAt = DateTime.UtcNow;
        user.IsDeleted = dto.IsDeleted;
        user.DeletedBy = dto.DeletedBy;

        var updated = await userRepository.UpdateAsync(user);
        if (!updated)
            throw new ServiceUnavailableException("Update User");

        logger.LogInformation("User updated successfully. UserId: {UserId}, " +
                              "SubscriptionPlan: {SubscriptionPlan}", userId, user.SubscriptionPlan);

        return user.MapToUserDto();
    }
    
    /// <inheritdoc/>
    public async Task<bool> SoftDeleteAsync(Guid userId, Guid deletedBy, CancellationToken cancellationToken = default)
    {
        var deleted = await userRepository.DeleteAsync(userId, deletedBy, cancellationToken);
        if (!deleted)
            throw new NotFoundException("User", userId);

        logger.LogInformation("User soft deleted. UserId: {UserId}, DeletedBy: {DeletedBy}", userId, deletedBy);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var activated = await userRepository.ActivateUserAsync(userId, cancellationToken);
        if (!activated)
            throw new NotFoundException("User", userId);

        logger.LogInformation("User activated. UserId: {UserId}", userId);
        return activated;
    }

    /// <inheritdoc/>
    public async Task<bool> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var deactivated = await userRepository.DeactivateUserAsync(userId, cancellationToken);
        if (!deactivated)
            throw new NotFoundException("User", userId);

        logger.LogInformation("User deactivated. UserId: {UserId}", userId);
        return deactivated;
    }
    
    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await userRepository.ExistsAsync(userId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> IsUsernameAvailableAsync(string username, CancellationToken cancellationToken = default)
    {
        return !await userRepository.UsernameExistsAsync(username, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> IsEmailAvailableAsync(string email, CancellationToken cancellationToken = default)
    {
        return !await userRepository.EmailExistsAsync(email, cancellationToken);
    }
}