using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shortly.Core.Abstractions.ClickTracking;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ClickEvents;

namespace Shortly.Infrastructure.ClickTracking;

/// <summary>
/// Background worker that asynchronously processes queued click events, performing user-agent parsing, 
/// geo-IP lookup, and traffic source classification before persisting them to the database.
/// </summary>
internal sealed class ClickTrackingBackgroundService(
    IClickEventQueue queue,
    IServiceScopeFactory scopeFactory,
    IUserAgentParser userAgentParser,
    IGeoLocationService geoLocationService,
    ILogger<ClickTrackingBackgroundService> logger) : BackgroundService
{
    private const int MaxReferrerDomainLength = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Continuously consume click events from the channel queue asynchronously
        await foreach (var capture in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(capture, stoppingToken);
            }
            catch (Exception ex)
            {
                // Isolate failures so a single bad record never crashes the background queue loop
                logger.LogError(ex, "Failed to record click event for short URL {ShortUrlId}", capture.ShortUrlId);
            }
        }
    }

    private async Task ProcessAsync(ClickEventCapture capture, CancellationToken cancellationToken)
    {
        // Parse metadata asynchronously or synchronously depending on provider implementation
        var uaInfo = userAgentParser.Parse(capture.UserAgent);
        var geoInfo = await geoLocationService.LookupAsync(capture.IpAddress, cancellationToken);
        
        var referrerDomain = ExtractDomain(capture.Referrer);
        var trafficSource = ClassifyTrafficSource(capture.Referrer, capture.UtmSource, referrerDomain);

        // Instantiate the click event aggregate via domain factory method
        var recorded = ClickEvent.Record(
            capture.ShortUrlId, capture.ClickedAtUtc, capture.IpAddress, capture.SessionId, capture.UserAgent,
            capture.Referrer, referrerDomain, trafficSource,
            capture.UtmSource, capture.UtmMedium, capture.UtmCampaign, capture.UtmTerm, capture.UtmContent,
            geoInfo.Country, geoInfo.City, uaInfo.Browser, uaInfo.OperatingSystem, uaInfo.Device, uaInfo.DeviceType);

        if (recorded.IsFailure)
        {
            logger.LogWarning("Dropped an invalid click for short URL {ShortUrlId}: {Error}", capture.ShortUrlId, recorded.Error.Code);
            return;
        }

        var clickEvent = recorded.Value;

        // Create a discrete dependency injection scope for scoped database persistence
        using var scope = scopeFactory.CreateScope();
        var clickEventRepository = scope.ServiceProvider.GetRequiredService<IClickEventRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        clickEventRepository.Add(clickEvent);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string? ExtractDomain(string? referrer)
    {
        if (!Uri.TryCreate(referrer, UriKind.Absolute, out var uri))
            return null;

        // Ensure the host string fits within the database column constraint (varchar(100))
        return uri.Host.Length <= MaxReferrerDomainLength ? uri.Host : uri.Host[..MaxReferrerDomainLength];
    }

    private static string ClassifyTrafficSource(string? referrer, string? utmSource, string? referrerDomain)
    {
        if (!string.IsNullOrWhiteSpace(utmSource)) 
            return "Campaign";
            
        if (string.IsNullOrWhiteSpace(referrer) || referrerDomain is null) 
            return "Direct";

        if (Contains(referrerDomain, "google.", "bing.", "duckduckgo.")) 
            return "Search";
            
        if (Contains(referrerDomain, "facebook.", "twitter.", "x.com", "linkedin.", "instagram.")) 
            return "Social";

        return "Referral";
    }

    private static bool Contains(string domain, params string[] needles) =>
        needles.Any(n => domain.Contains(n, StringComparison.OrdinalIgnoreCase));
}