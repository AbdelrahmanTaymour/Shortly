namespace Shortly.Core.Auth.DTOs;

public record AuthenticationResponse(
    Guid Id,
    string? Email,
    TokenResponse? Tokens,
    bool Success,
    bool RequiresEmailConfirmation);