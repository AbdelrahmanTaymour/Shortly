namespace Shortly.Core.Security.DTOs;

public record LockUserRequest(DateTime LockUntil, string? Reason);