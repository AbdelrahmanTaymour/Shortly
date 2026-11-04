using MediatR;
using Shortly.Core.Abstractions.Clock;
using Microsoft.Extensions.Logging;
using Quartz;
using Shortly.Core.Admin.ResetAllOrganizationsUsage;
using Shortly.Core.Admin.ResetAllUsersUsage;
using Shortly.Domain.Abstractions;

namespace Shortly.Infrastructure.ScheduledJobs;

/// <summary>
/// Monthly scheduled job that resets usage counters for all users and organizations.
/// Protected against concurrent overlapping runs.
/// </summary>
[DisallowConcurrentExecution]
public sealed class MonthlyUsageResetJob(
    ISender sender, 
    IDateTimeProvider dateTimeProvider,
    ILogger<MonthlyUsageResetJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var startedAt = dateTimeProvider.UtcNow;
        
        logger.LogInformation("Monthly usage reset job starting at {StartedAtUtc}.", startedAt);

        // Execute user and organization usage resets independently
        var userResult = await TryResetAsync("user usage", () => sender.Send(new ResetAllUsersUsageCommand(), cancellationToken), cancellationToken);
        var orgResult = await TryResetAsync("organization usage", () => sender.Send(new ResetAllOrganizationsUsageCommand(), cancellationToken), cancellationToken);

        var durationMs = (dateTimeProvider.UtcNow - startedAt).TotalMilliseconds;

        // If both operations succeeded, log success and complete
        if (userResult.Succeeded && orgResult.Succeeded)
        {
            logger.LogInformation(
                "Monthly usage reset completed successfully in {DurationMs:F2}ms: {UsersReset} users, {OrgsReset} organizations.",
                durationMs, userResult.AffectedCount, orgResult.AffectedCount);
            return;
        }

        // Aggregate failures and abort job execution without immediate refire
        var failures = new[] { userResult, orgResult }.Where(r => !r.Succeeded).Select(r => r.Label);
        var message = $"Monthly usage reset partially failed: {string.Join(", ", failures)} did not complete.";
        
        logger.LogError("{Message}", message);
        throw new JobExecutionException(message) { RefireImmediately = false };
    }

    private async Task<ResetOutcome> TryResetAsync(
        string label, 
        Func<Task<Result<int>>> operation, 
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await operation();
            if (result.IsFailure)
            {
                logger.LogError("Monthly {Label} reset failed: {Error}", label, result.Error);
                return ResetOutcome.Failed(label);
            }
            
            return ResetOutcome.Ok(label, result.Value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Monthly {Label} reset was cancelled due to application shutdown.", label);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected exception occurred while resetting monthly {Label}.", label);
            return ResetOutcome.Failed(label);
        }
    }

    private readonly record struct ResetOutcome(string Label, bool Succeeded, int AffectedCount)
    {
        public static ResetOutcome Ok(string label, int count) => new(label, true, count);
        public static ResetOutcome Failed(string label) => new(label, false, 0);
    }
}