using Microsoft.Extensions.Logging;
using Quartz;
using Shortly.Core.ServiceContracts.OrganizationManagement;
using Shortly.Core.ServiceContracts.UserManagement;

namespace Shortly.Infrastructure.ScheduledJobs;

/// <summary>
/// Scheduled job that resets monthly usage counters for both users and organizations.
/// Runs at midnight on the 1st of each month.
/// </summary>
[DisallowConcurrentExecution]
public class MonthlyUsageResetJob(
    IUserUsageService userUsageService,
    IOrganizationUsageService organizationUsageService,
    ILogger<MonthlyUsageResetJob> logger
) : IJob
{
    /// <summary>
    /// Executes the job to reset monthly usage counters.
    /// </summary>
    /// <param name="context">Quartz job execution context.</param>
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Starting monthly usage reset job at {UtcNow}.", DateTime.UtcNow);
        try
        {
            int userUsageRested = await userUsageService.ResetMonthlyUsageForAllAsync(context.CancellationToken);
            int orgUsageRested = await organizationUsageService.ResetMonthlyUsageForAllAsync(context.CancellationToken);

            if (userUsageRested > 0 && orgUsageRested > 0)
            {
                logger.LogInformation(
                    "Successfully reset monthly usage for {userUsageRested} users and {orgUsageRested} organizations.",
                    userUsageRested, orgUsageRested);
            }
            else
            {
                logger.LogWarning(
                    "Monthly usage reset partially succeeded. UserReset: {UserReset}, OrganizationReset: {OrgReset}",
                    userUsageRested, orgUsageRested);

            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while resetting monthly usage.");
        }
    }
}