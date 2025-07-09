namespace Shortly.Core.DTOs;

public record AuthenticationResponse(Guid Id, string? Name, string? Email, string? Token,DateTime? TokenExpiration, bool Success)
{
    // Parameterless constructor
    public AuthenticationResponse(): this(Guid.Empty, null, null, null, null,false) { }
}