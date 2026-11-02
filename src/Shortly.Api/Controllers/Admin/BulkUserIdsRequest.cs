namespace Shortly.Api.Controllers.Admin;

public sealed record BulkUserIdsRequest(IReadOnlyCollection<Guid> UserIds);
