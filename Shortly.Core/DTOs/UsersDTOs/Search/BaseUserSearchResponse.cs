namespace Shortly.Core.DTOs.UsersDTOs.Search;

/// <summary>
/// Base response for user search operations containing pagination metadata.
/// </summary>
/// <param name="TotalCount">Total number of users matching the search criteria.</param>
/// <param name="Page">Current page number (1-based).</param>
/// <param name="PageSize">Number of items per page.</param>
/// <param name="TotalPages">Total number of pages available.</param>
public abstract record BaseUserSearchResponse(int TotalCount, int Page, int PageSize, int TotalPages);