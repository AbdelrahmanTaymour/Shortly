using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shortly.Core.Abstractions.ClickTracking;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ClickEvents;

namespace Shortly.Infrastructure.ClickTracking;

internal sealed class ClickTrackingBackgroundService(
    IClickEventQueue queue,
    IServiceScopeFactory scopeFactory,
    IUserAgentParser userAgentParser,
    IGeoLocationService geoLocationService,
    ILogger<ClickTrackingBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var capture in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(capture, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to record click event for short URL {ShortUrlId}", capture.ShortUrlId);
            }
        }
    }

    private async Task ProcessAsync(ClickEventCapture capture, CancellationToken cancellationToken)
    {
        var uaInfo = userAgentParser.Parse(capture.UserAgent);
        var geoInfo = await geoLocationService.LookupAsync(capture.IpAddress, cancellationToken);
        var referrerDomain = ExtractDomain(capture.Referrer);
        var trafficSource = ClassifyTrafficSource(capture.Referrer, capture.UtmSource, referrerDomain);

        var clickEvent = ClickEvent.Record(
            capture.ShortUrlId, capture.ClickedAtUtc, capture.IpAddress, capture.SessionId, capture.UserAgent,
            capture.Referrer, referrerDomain, trafficSource,
            capture.UtmSource, capture.UtmMedium, capture.UtmCampaign, capture.UtmTerm, capture.UtmContent,
            geoInfo.Country, geoInfo.City, uaInfo.Browser, uaInfo.OperatingSystem, uaInfo.Device, uaInfo.DeviceType);

        using var scope = scopeFactory.CreateScope();
        var clickEventRepository = scope.ServiceProvider.GetRequiredService<IClickEventRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        clickEventRepository.Add(clickEvent);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string? ExtractDomain(string? referrer) =>
        Uri.TryCreate(referrer, UriKind.Absolute, out var uri) ? uri.Host : null;

    private static string ClassifyTrafficSource(string? referrer, string? utmSource, string? referrerDomain)
    {
        if (!string.IsNullOrWhiteSpace(utmSource)) return "Campaign";
        if (string.IsNullOrWhiteSpace(referrer) || referrerDomain is null) return "Direct";

        if (Contains(referrerDomain, "google.", "bing.", "duckduckgo.")) return "Search";
        if (Contains(referrerDomain, "facebook.", "twitter.", "x.com", "linkedin.", "instagram.")) return "Social";

        return "Referral";
    }

    private static bool Contains(string domain, params string[] needles) =>
        needles.Any(n => domain.Contains(n, StringComparison.OrdinalIgnoreCase));
}