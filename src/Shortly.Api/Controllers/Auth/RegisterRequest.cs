namespace Shortly.API.Controllers.Auth;

public record RegisterRequest(string Email, string Username, string Password);