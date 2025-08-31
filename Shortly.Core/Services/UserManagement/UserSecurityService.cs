using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.UsersDTOs.Security;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Core.ServiceContracts.Tokens;
using Shortly.Core.ServiceContracts.UserManagement;

namespace Shortly.Core.Services.UserManagement;

/// <summary>
///     Provides security-related operations for user accounts, such as login tracking and account locking.
/// </summary>
public class UserSecurityService(
    IUserSecurityRepository securityRepository,
    ITokenService tokenService,
    ILogger<UserSecurityService> logger)
    : IUserSecurityService
{
    /// <inheritdoc />
    public async Task<UserSecurityStatusResponse> GetUserSecurityStatusAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        var security = await securityRepository.GetByUserIdAsync(userId, cancellationToken);
        if (security == null)
            throw new NotFoundException("UserSecurity", userId);

        return new UserSecurityStatusResponse
        {
            UserId = userId,
            IsLocked = security.LockedUntil.HasValue,
            LockedUntil = security.LockedUntil,
            LockReason = security.LockoutReason,
            FailedAttemptsCount = security.FailedLoginAttempts,
            DaysUntilUnlock = security.LockedUntil.HasValue
                ? Math.Max((security.LockedUntil.Value - DateTime.UtcNow).Days, 0)
                : 0
        };
    }

    /// <inheritdoc />
    public async Task<bool> RecordFailedLoginAttemptAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var recorded = await securityRepository.IncrementFailedLoginAttemptsAsync(userId, cancellationToken);
        if (!recorded)
            throw new NotFoundException("UserSecurity", userId);
        return recorded;
    }

    /// <inheritdoc />
    public async Task<bool> ResetFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var done = await securityRepository.ResetFailedLoginAttemptsAsync(userId, cancellationToken);
        if (!done)
            throw new NotFoundException("UserSecurity", userId);
        return done;
    }

    /// <inheritdoc />
    public async Task<bool> IsUserLockedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await securityRepository.IsUserLockedAsync(userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<LockUserResponse> LockUserAsync(Guid userId, LockUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var locked =
            await securityRepository.LockUserAsync(userId, request.LockUntil, request.Reason, cancellationToken);
        if (!locked)
            throw new NotFoundException($"User with id {userId} not found or already locked.");

        // Revoke all user tokens
        var tokensRevoked = await tokenService.RevokeAllUserTokensAsync(userId, cancellationToken);

        return new LockUserResponse
        {
            Success = true,
            Message = "User account locked successfully",
            UserId = userId,
            LockedUntil = request.LockUntil,
            TokensRevoked = tokensRevoked
        };
    }

    /// <inheritdoc />
    public async Task<UnlockUserResponse> UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unLocked = await securityRepository.UnlockUserAsync(userId, cancellationToken);
        if (!unLocked)
            throw new NotFoundException($"User with id {userId} not found or already unlocked.");
        return new UnlockUserResponse
        {
            Success = true,
            Message = "User account unlocked successfully",
            UserId = userId,
            UnlockedAt = DateTime.UtcNow
        };
    }
}