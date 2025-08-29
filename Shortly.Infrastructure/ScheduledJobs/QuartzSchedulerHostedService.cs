using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Spi;

namespace Shortly.Infrastructure.ScheduledJobs;

/// <summary>
/// Hosted service for configuring and starting Quartz scheduler.
/// </summary>
public class QuartzSchedulerHostedService(IServiceProvider serviceProvider, ISchedulerFactory schedulerFactory) : IHostedService
{
    private IScheduler _scheduler;
    
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Create the scheduler
        _scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        
        // Set the job factory to use the DI service provider
        _scheduler.JobFactory = serviceProvider.GetRequiredService<IJobFactory>();

        // Start the scheduler
        await _scheduler.Start(cancellationToken);

        // Schedule the MonthlyUsageResetJob
        var job = JobBuilder.Create<MonthlyUsageResetJob>()
            .WithIdentity("MonthlyUsageResetJob", "UsageResetGroup")
            .Build();

        // Trigger to run at midnight on the 1st of each month
        var trigger = TriggerBuilder.Create()
            .WithIdentity("MonthlyUsageResetTrigger", "UsageResetGroup")
            .StartNow()
            .WithSchedule(CronScheduleBuilder
                .MonthlyOnDayAndHourAndMinute(1, 0, 0)) // 1st of each month at 00:00
            .Build();

        // Schedule the job
        await _scheduler.ScheduleJob(job, trigger, cancellationToken);

    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_scheduler != null)
        {
            await _scheduler.Shutdown(cancellationToken);
        }
    }
}