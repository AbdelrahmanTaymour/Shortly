namespace Shortly.Core.DTOs.UsersDTOs.Security;

public record LockUserRequest(DateTime LockUntil, string? Reason);