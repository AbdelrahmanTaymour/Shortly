using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.Tokens;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.Tokens;

/// <inheritdoc />
/// <param name="dbContext">The database context for accessing UserActionTokens.</param>
/// <param name="logger">Logger instance for tracking operations and errors.</param>
public class UserActionTokenRepository(SQLServerDbContext dbContext, ILogger<UserActionTokenRepository> logger) : IUserActionTokenRepository
{
    /// <inheritdoc />
    public async Task<UserActionToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.UserActionTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving action token by ID {Id}", id);
            throw new DatabaseException("Failed to retrieve action token by ID.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<UserActionToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.UserActionTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving action token by TokenHash {TokenHash}", tokenHash);
            throw new DatabaseException("Failed to retrieve action token by token hash.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<UserActionToken?> GetActiveTokenAsync(Guid userId, enUserActionTokenType tokenType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.UserActionTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.UserId == userId &&
                                          t.TokenType == tokenType && !t.Used &&
                                          t.ExpiresAt > DateTime.UtcNow, 
                    cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving active {TokenType} token for UserId {UserId}", tokenType, userId);
            throw new DatabaseException($"Failed to retrieve active {tokenType} token for the specified user.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserActionToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.UserActionTokens
                .AsNoTracking()
                .Where(t => t.UserId == userId && !t.Used && t.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving active tokens for UserId {UserId}", userId);
            throw new DatabaseException("Failed to retrieve active tokens for the specified user.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserActionToken>> GetExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.UserActionTokens
                .AsNoTracking()
                .Where(t => t.Used || t.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving expired tokens");
            throw new DatabaseException("Failed to retrieve expired tokens.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<UserActionToken> CreateAsync(UserActionToken token, CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.UserActionTokens.AddAsync(token, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return token;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating action token for UserId {UserId}, TokenType {TokenType}", token.UserId, token.TokenType);
            throw new DatabaseException("Failed to create action token.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<UserActionToken> UpdateAsync(UserActionToken token, CancellationToken cancellationToken = default)
    {
        try
        {
            dbContext.UserActionTokens.Update(token);
            await dbContext.SaveChangesAsync(cancellationToken);
            return token;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating action token with ID {Id}", token.Id);
            throw new DatabaseException("Failed to update action token.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ConsumeTokenAsync(string tokenHash, enUserActionTokenType tokenType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.UserActionTokens
                .Where(t => t.TokenHash == tokenHash && t.TokenType == tokenType && !t.Used)
                .ExecuteUpdateAsync(setters =>
                        setters.SetProperty(t => t.Used, true)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError("Error consuming action token with tokenHash {tokenHash}", tokenHash);
            throw new DatabaseException("Failed to consume action token.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.UserActionTokens
                .Where(t => t.Id == id)
                .ExecuteUpdateAsync(setters =>
                        setters.SetProperty(t => t.Used, true)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting (marking as used) token with ID {Id}", id);
            throw new DatabaseException("Failed to delete action token.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<int> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.UserActionTokens
                .Where(t => t.Used || t.ExpiresAt <= DateTime.UtcNow)
                .ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting expired tokens");
            throw new DatabaseException("Failed to delete expired tokens.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> InvalidateUserTokensAsync(Guid userId, enUserActionTokenType tokenType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.UserActionTokens
                .Where(t=> t.UserId == userId && t.TokenType == tokenType && !t.Used)
                .ExecuteUpdateAsync(setters =>
                        setters.SetProperty(t => t.Used, true)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error invalidating {TokenType} tokens for UserId {UserId}", tokenType, userId);
            throw new DatabaseException("Failed to invalidate user tokens.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasActiveTokenAsync(Guid userId, enUserActionTokenType tokenType, CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.UserActionTokens
                .AsNoTracking()
                .Where(t => t.UserId == userId && t.TokenType == tokenType && !t.Used && t.ExpiresAt > DateTime.UtcNow)
                .AnyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if UserId {UserId} has an active {TokenType} token", userId, tokenType);
            throw new DatabaseException("Failed to check if user has an active token.", ex);
        }
    }
}