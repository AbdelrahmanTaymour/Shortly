namespace Shortly.Core.ShortUrls.DTOs;

/// <summary>
/// Encapsulates all filter, sort, and pagination parameters for the search endpoint.
/// Bound from query-string via [FromQuery] in the controller — every field is optional
/// except UserId, which is required to scope results to the correct owner.
/// </summary>
public sealed record ShortUrlSearchRequest
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public string? Visibility { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public string SortBy { get; init; } = "newest";
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}