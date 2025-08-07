namespace Shortly.Core.DTOs.UsersDTOs.Search;

/// <summary>
/// Response containing complete user search results with all related data and pagination metadata.
/// </summary>
/// <param name="Users">Collection of complete user information including related entities.</param>
/// <param name="TotalCount">Total number of users matching the search criteria.</param>
/// <param name="Page">Current page number (1-based).</param>
/// <param name="PageSize">Number of items per page.</param>
/// <param name="TotalPages">Total number of pages available.</param>
public record CompleteUserSearchResponse
(
    IEnumerable<CompleteUserSearchResult> Users,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
) : BaseUserSearchResponse(TotalCount, Page, PageSize, TotalPages);