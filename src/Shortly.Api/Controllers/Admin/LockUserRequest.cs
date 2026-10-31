namespace Shortly.API.Controllers.Admin;

public sealed record LockUserRequest(string Reason, TimeSpan Duration);
