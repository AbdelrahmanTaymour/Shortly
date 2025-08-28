using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shortly.Core.Models;
using Shortly.Core.ServiceContracts.ClickTracking;

namespace Shortly.Infrastructure.BackgroundServices;

public class ClickTrackingBackgroundWorker(
    ClickTrackingQueueService clickQueueService,
    IServiceProvider serviceProvider,
    ILogger<ClickTrackingBackgroundWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Click tracking background worker is starting.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var clickJob = await clickQueueService.DequeueClickAsync(stoppingToken);

                if (clickJob.HasValue)
                {
                    var (redirectId, trackingData) = clickJob.Value;
                    
                    // Process the click tracking job within a scoped DI context
                    using var scope = serviceProvider.CreateScope();
                    var clickTrackingService = scope.ServiceProvider.GetRequiredService<IClickTrackingService>();
                    
                    try
                    {
                        logger.LogInformation("Processing click tracking for redirect ID {RedirectId}.", redirectId);
                        await clickTrackingService.TrackClickAsync(redirectId, trackingData);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to process click tracking for redirect ID {RedirectId}.", redirectId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Gracefully handle cancellation (e.g., shutdown signal)
                break;
            }
            catch (Exception ex)
            {
                // Log unexpected errors and continue processing
                logger.LogError(ex, "An error occurred in the click tracking background worker.");
            }
        }
    }
}