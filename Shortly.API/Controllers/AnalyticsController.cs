using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Middleware;
using Shortly.Core.DTOs;
using Shortly.Core.ServiceContracts;

namespace Shortly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Get analytics summary for a specific short URL
    /// </summary>
    /// <param name="shortCode">The short code of the URL</param>
    /// <param name="days">Number of days to include in analytics (default: 30)</param>
    [HttpGet("{shortCode}/summary")]
    [RateLimit(MaxRequests = 100, WindowSeconds = 3600)]
    [ProducesResponseType(typeof(AnalyticsSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalyticsSummary(string shortCode, int days = 30)
    {
        var summary = await _analyticsService.GetAnalyticsSummaryAsync(shortCode, days);
        if (summary == null)
            return NotFound();

        return Ok(summary);
    }

    /// <summary>
    /// Get detailed click data for a specific short URL
    /// </summary>
    /// <param name="shortCode">The short code of the URL</param>
    /// <param name="page">Page number for pagination</param>
    /// <param name="pageSize">Number of records per page</param>
    [HttpGet("{shortCode}/clicks")]
    [RateLimit(MaxRequests = 50, WindowSeconds = 3600)]
    [ProducesResponseType(typeof(PaginatedResponse<ClickDataResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetailedClicks(string shortCode, int page = 1, int pageSize = 50)
    {
        var clicks = await _analyticsService.GetDetailedClicksAsync(shortCode, page, pageSize);
        return Ok(clicks);
    }

    /// <summary>
    /// Get analytics for all URLs belonging to the authenticated user
    /// </summary>
    /// <param name="days">Number of days to include in analytics</param>
    [HttpGet("dashboard")]
    [RateLimit(MaxRequests = 20, WindowSeconds = 3600)]
    [ProducesResponseType(typeof(DashboardAnalyticsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardAnalytics(int days = 30)
    {
        var userId = GetCurrentUserId();
        var analytics = await _analyticsService.GetDashboardAnalyticsAsync(userId, days);
        return Ok(analytics);
    }

    /// <summary>
    /// Export analytics data in various formats
    /// </summary>
    /// <param name="shortCode">The short code of the URL</param>
    /// <param name="format">Export format (csv, json, excel)</param>
    /// <param name="days">Number of days to include</param>
    [HttpGet("{shortCode}/export")]
    [RateLimit(MaxRequests = 10, WindowSeconds = 3600)]
    public async Task<IActionResult> ExportAnalytics(string shortCode, string format = "csv", int days = 30)
    {
        var data = await _analyticsService.ExportAnalyticsAsync(shortCode, format, days);
        
        return format.ToLower() switch
        {
            "csv" => File(data, "text/csv", $"{shortCode}_analytics.csv"),
            "json" => File(data, "application/json", $"{shortCode}_analytics.json"),
            "excel" => File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{shortCode}_analytics.xlsx"),
            _ => BadRequest("Unsupported format")
        };
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
    }
}