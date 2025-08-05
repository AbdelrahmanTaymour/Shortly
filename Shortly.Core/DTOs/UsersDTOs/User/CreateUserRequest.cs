namespace Shortly.Core.DTOs.UsersDTOs.User;

public sealed record CreateUserRequest(string Email, string Username, string Password);