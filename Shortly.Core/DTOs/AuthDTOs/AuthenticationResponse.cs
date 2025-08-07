namespace Shortly.Core.DTOs.AuthDTOs;

public record AuthenticationResponse(
    Guid Id,
    string? Email,
    TokenResponse? Tokens,
    bool Success,
    bool RequiresEmailConfirmation);