using Microsoft.Extensions.Logging;
using Quartz;
using Shortly.Core.Abstractions.Clock;
using Shortly.Domain.RefreshToken;
using Shortly.Domain.Tokens;

namespace Shortly.Infrastructure.ScheduledJobs;

[DisallowConcurrentExecution]
public sealed class ExpiredTokenCleanupJob(
    IUserActionTokenRepository userActionTokenRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IDateTimeProvider dateTimeProvider,
    ILogger<ExpiredTokenCleanupJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var utcNow = dateTimeProvider.UtcNow;

        try
        {
            var deletedActionTokens = await userActionTokenRepository.DeleteExpiredOrUsedAsync(utcNow, cancellationToken);
            var deletedRefreshTokens = await refreshTokenRepository.CleanupExpiredAsync(utcNow, cancellationToken);

            logger.LogInformation(
                "Token cleanup removed {ActionTokens} user action tokens and {RefreshTokens} refresh tokens.",
                deletedActionTokens, deletedRefreshTokens);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Token cleanup was cancelled (job shutdown).");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Token cleanup failed.");
            throw new JobExecutionException(ex) { RefireImmediately = false };
        }
    }
}