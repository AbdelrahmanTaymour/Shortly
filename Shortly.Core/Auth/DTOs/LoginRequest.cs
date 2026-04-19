namespace Shortly.Core.Auth.DTOs;

public record LoginRequest(string EmailOrUsername, string Password);