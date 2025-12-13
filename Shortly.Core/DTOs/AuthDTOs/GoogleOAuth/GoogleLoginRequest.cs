namespace Shortly.Core.DTOs.AuthDTOs.GoogleOAuth;

/// <summary>
/// Request for initiating Google OAuth login
/// </summary>
public record GoogleLoginRequest{
    public string? ReturnUrl { get; init; }
}