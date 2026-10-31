using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Admin.GetUserDetail;

public sealed record GetUserDetailQuery(Guid? UserId, string? Email, string? Username) : IQuery<UserAdminDetailResponse>;
