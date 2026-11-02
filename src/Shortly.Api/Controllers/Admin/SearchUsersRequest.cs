using Shortly.Core.Admin.SearchUsers;
using Shortly.Domain.Users;

namespace Shortly.Api.Controllers.Admin;

public sealed record SearchUsersRequest(string? Search, UserStatusFilter Status = UserStatusFilter.Active, UserRole? Role = null, int Page = 1, int PageSize = 10);
