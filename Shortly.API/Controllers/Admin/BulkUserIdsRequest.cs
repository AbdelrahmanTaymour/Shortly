namespace Shortly.API.Controllers.Admin;

public sealed record BulkUserIdsRequest(IReadOnlyCollection<Guid> UserIds);
