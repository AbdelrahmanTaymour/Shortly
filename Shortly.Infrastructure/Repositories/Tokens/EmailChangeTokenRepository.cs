using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.Tokens;
using Shortly.Domain.Entities;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.Tokens;

/// <inheritdoc />
public class EmailChangeTokenRepository(SQLServerDbContext dbContext, ILogger<EmailChangeTokenRepository> logger) : IEmailChangeTokenRepository
{
    /// <inheritdoc />
    public async Task<EmailChangeToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.EmailChangeTokens
                .AsNoTracking()
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving email change token by ID: {Id}", id);
            throw new DatabaseException("Failed to retrieve email change token by ID", ex);
        }
    }

    /// <inheritdoc />
    public async Task<EmailChangeToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.EmailChangeTokens
                .AsNoTracking()
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Token.Equals(token) && !e.IsUsed && e.ExpiresAt > DateTime.UtcNow, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving email change token by token: {Token}", token);
            throw new DatabaseException("Failed to retrieve email change token by token", ex);
        }
    }

    /// <inheritdoc />
    public async Task<EmailChangeToken?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.EmailChangeTokens
                .AsNoTracking()
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.UserId == userId && !e.IsUsed && e.ExpiresAt > DateTime.UtcNow, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving active email change token by user ID: {UserId}", userId);
            throw new DatabaseException("Failed to retrieve active email change token by user ID", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EmailChangeToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.EmailChangeTokens
                .AsNoTracking()
                .Include(e => e.User)
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving email change tokens by user ID: {UserId}", userId);
            throw new DatabaseException("Failed to retrieve email change tokens by user ID", ex);
        }
    }
    
    /// <inheritdoc />
    public async Task<EmailChangeToken> CreateAsync(EmailChangeToken emailChangeToken, CancellationToken cancellationToken = default)
    {
        try
        {
            dbContext.EmailChangeTokens.Add(emailChangeToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return emailChangeToken;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating email change token for user: {UserId}", emailChangeToken.UserId);
            throw new DatabaseException("Failed to create email change token", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(EmailChangeToken emailChangeToken, CancellationToken cancellationToken = default)
    {
        try
        {
            dbContext.EmailChangeTokens.Update(emailChangeToken);
            return await dbContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating email change token: {Id}", emailChangeToken.Id);
            throw new DatabaseException("Failed to update email change token", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> UseTokenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.EmailChangeTokens
                .Where(e => e.Id == id)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(e => e.IsUsed, true)
                        .SetProperty(e => e.UsedAt, DateTime.UtcNow)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting email change token: {Id}", id);
            throw new DatabaseException("Failed to delete email change token", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.EmailChangeTokens
                .Where(e => e.Id == id)
                .ExecuteDeleteAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting email change token: {Id}", id);
            throw new DatabaseException("Failed to delete email change token", ex);
        }
    }

    /// <inheritdoc />
    public async Task<int> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.EmailChangeTokens
                .Where(e => e.ExpiresAt <= DateTime.UtcNow)
                .ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting expired email change tokens");
            throw new DatabaseException("Failed to delete expired email change tokens", ex);
        }
    }
}