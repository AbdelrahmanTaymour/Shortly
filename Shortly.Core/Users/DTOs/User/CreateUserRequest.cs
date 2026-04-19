namespace Shortly.Core.Users.DTOs.User;

public sealed record CreateUserRequest(string Email, string Username, string Password);