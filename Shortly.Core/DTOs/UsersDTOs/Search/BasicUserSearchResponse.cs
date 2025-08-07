namespace Shortly.Core.DTOs.UsersDTOs.Search;

/// <summary>
/// Response containing basic user search results with pagination metadata.
/// </summary>
/// <param name="Users">Collection of basic user information.</param>
/// <param name="TotalCount">Total number of users matching the search criteria.</param>
/// <param name="Page">Current page number (1-based).</param>
/// <param name="PageSize">Number of items per page.</param>
/// <param name="TotalPages">Total number of pages available.</param>
public record BasicUserSearchResponse(
    IEnumerable<UserSearchResult> Users,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
) : BaseUserSearchResponse(TotalCount, Page, PageSize, TotalPages);