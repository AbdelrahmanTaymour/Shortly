namespace Shortly.Api.Controllers.Admin;

public sealed record LockUserRequest(string Reason, TimeSpan Duration);
