namespace Shortly.Core.DTOs.AuthDTOs;

public record LoginRequest(string EmailOrUsername, string Password);