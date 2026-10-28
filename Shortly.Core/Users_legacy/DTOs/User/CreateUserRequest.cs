namespace Shortly.Core.Users_legacy.DTOs.User;

public sealed record CreateUserRequest(string Email, string Username, string Password);