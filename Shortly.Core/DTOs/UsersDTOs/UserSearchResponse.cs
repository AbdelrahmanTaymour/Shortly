namespace Shortly.Core.DTOs.UsersDTOs;

public record UserSearchResponse(IEnumerable<UserViewDto> Users,  int TotalCount, int Page, int PageSize, int TotalPages);