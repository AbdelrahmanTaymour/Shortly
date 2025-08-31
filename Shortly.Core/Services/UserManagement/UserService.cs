using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.Mappers;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Core.ServiceContracts.Tokens;
using Shortly.Core.ServiceContracts.UserManagement;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.Services.UserManagement;

/// <summary>
///     Provides core business logic for managing user accounts, including creation, updates,
///     activation/deactivation, availability checks, and soft deletion.
/// </summary>
public class UserService(IUserRepository userRepository, ITokenService tokenService, ILogger<UserService> logger) : IUserService
{
    /// <inheritdoc />
    public async Task<UserDto> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new NotFoundException("User", userId);
        return user.MapToUserDto();
    }

    /// <inheritdoc />
    public async Task<UserDto> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (user == null)
            throw new NotFoundException("User", email);
        return user.MapToUserDto();
    }

    /// <inheritdoc />
    public async Task<UserDto> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByUsernameAsync(username, cancellationToken);
        if (user == null)
            throw new NotFoundException("User", username);
        return user.MapToUserDto();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<UserDto> UpdateAsync(Guid userId, UpdateUserRequest request)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new NotFoundException("User", userId);

        
        if (!user.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase) &&
            await userRepository.UsernameExistsAsync(request.Username))
        {
            throw new ConflictException("Username is already taken.");
        }
        

        // Update user properties
        user.Username = request.Username;
        user.SubscriptionPlanId = request.SubscriptionPlanId;
        user.Permissions = request.Permissions;
        user.IsActive = request.IsActive;
        user.IsEmailConfirmed = request.IsEmailConfirmed;
        user.UpdatedAt = DateTime.UtcNow;

        var updated = await userRepository.UpdateAsync(user);
        if (!updated)
            throw new ServiceUnavailableException("Update User");

        logger.LogInformation("User updated successfully. UserId: {UserId}, " +
                              "SubscriptionPlan: {SubscriptionPlan}", userId, user.SubscriptionPlan);

        return user.MapToUserDto();
    }

    /// <inheritdoc />
    public async Task<bool> SoftDeleteAsync(Guid userId, Guid deletedBy, CancellationToken cancellationToken = default)
    {
        var deleted = await userRepository.DeleteAsync(userId, deletedBy, cancellationToken);
        if (!deleted)
            throw new NotFoundException("User", userId);

        await tokenService.RevokeAllUserTokensAsync(userId, cancellationToken);
        
        logger.LogInformation("User soft deleted. UserId: {UserId}, DeletedBy: {DeletedBy}", userId, deletedBy);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> MarkEmailAsConfirmedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await userRepository.MarkEmailAsConfirmedAsync(userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var activated = await userRepository.ActivateUserAsync(userId, cancellationToken);
        if (!activated)
            throw new NotFoundException("User not found or already activated");

        logger.LogInformation("User activated. UserId: {UserId}", userId);
        return activated;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var deactivated = await userRepository.DeactivateUserAsync(userId, cancellationToken);
        if (!deactivated)
            throw new NotFoundException("User not found or already deactivated.");

        logger.LogInformation("User deactivated. UserId: {UserId}", userId);
        return deactivated;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await userRepository.ExistsAsync(userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsUsernameAvailableAsync(string username, CancellationToken cancellationToken = default)
    {
        return await userRepository.UsernameExistsAsync(username, cancellationToken) == false;
    }

    /// <inheritdoc />
    public async Task<bool> IsEmailAvailableAsync(string email, CancellationToken cancellationToken = default)
    {
        return await userRepository.EmailExistsAsync(email, cancellationToken) == false;
    }
}