using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shortly.Core.ServiceContracts;

namespace Shortly.Infrastructure.Services;

public class UrlCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UrlCleanupService> _logger;

    public UrlCleanupService(IServiceProvider serviceProvider, ILogger<UrlCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var urlService = scope.ServiceProvider.GetRequiredService<IShortUrlsService>();
                
                // Clean up expired URLs
                await CleanupExpiredUrls(urlService);
                
                // Wait for 1 hour before next cleanup
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in URL cleanup service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task CleanupExpiredUrls(IShortUrlsService urlService)
    {
        _logger.LogInformation("Starting expired URL cleanup");
        
        // Implementation for cleaning up expired URLs
        var expiredCount = await urlService.DeleteExpiredUrlsAsync();
        
        _logger.LogInformation("Cleaned up {Count} expired URLs", expiredCount);
    }
}

public class AnalyticsProcessingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AnalyticsProcessingService> _logger;

    public AnalyticsProcessingService(IServiceProvider serviceProvider, ILogger<AnalyticsProcessingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
                
                // Process analytics aggregation
                await ProcessAnalyticsAggregation(analyticsService);
                
                // Wait for 15 minutes before next processing
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in analytics processing service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task ProcessAnalyticsAggregation(IAnalyticsService analyticsService)
    {
        _logger.LogInformation("Starting analytics aggregation");
        
        // Implementation for processing analytics
        await analyticsService.ProcessDailyAggregationAsync();
        
        _logger.LogInformation("Analytics aggregation completed");
    }
}

public class WebhookDeliveryService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebhookDeliveryService> _logger;

    public WebhookDeliveryService(IServiceProvider serviceProvider, ILogger<WebhookDeliveryService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();
                
                // Process pending webhook deliveries
                await ProcessPendingWebhooks(webhookService);
                
                // Wait for 30 seconds before next processing
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in webhook delivery service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task ProcessPendingWebhooks(IWebhookService webhookService)
    {
        // Implementation for processing webhook deliveries
        await webhookService.ProcessPendingDeliveriesAsync();
    }
}