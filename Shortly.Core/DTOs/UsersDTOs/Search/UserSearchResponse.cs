namespace Shortly.Core.DTOs.UsersDTOs.Search;

public record UserSearchResponse(IEnumerable<IUserSearchResult> Users,  int TotalCount, int Page, int PageSize, int TotalPages);