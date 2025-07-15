namespace Shortly.Core.DTOs;

public record TokenResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiry, DateTime RefreshTokenExpiry);