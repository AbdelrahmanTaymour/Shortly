namespace Shortly.Core.DTOs.AuthDTOs;

public record AuthenticationResponse(Guid Id, string? Name, string? Email, TokenResponse? Tokens, bool Success)
{
    // Parameterless constructor
    public AuthenticationResponse(): this(Guid.Empty,null, null,null,false) { }
}