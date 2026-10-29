using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Admin.SearchUsers;

public enum UserStatusFilter { All, Active, Inactive, Deleted }

public sealed record SearchUsersQuery(
    string? Search, UserStatusFilter Status, UserRole? Role, int Page = 1, int PageSize = 10)
    : IQuery<PagedResponse<UserAdminSummaryResponse>>;

