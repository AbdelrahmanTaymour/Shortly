namespace Shortly.Core.DTOs.AuthDTOs.GoogleOAuth;

/// <summary>
/// Response after Google OAuth callback with authorization code
/// </summary>
public record GoogleCallbackRequest
{
    public required string Code { get; init; }
    public string? State { get; init; }
}