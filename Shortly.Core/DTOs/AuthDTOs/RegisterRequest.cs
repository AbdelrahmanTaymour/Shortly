namespace Shortly.Core.DTOs.AuthDTOs;

public record RegisterRequest(string Name, string Email, string Username, string Password);