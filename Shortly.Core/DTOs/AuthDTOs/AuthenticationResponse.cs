namespace Shortly.Core.DTOs.AuthDTOs;

public record AuthenticationResponse(Guid Id, string? Name, string? Email, TokenResponse? Tokens, bool Success);