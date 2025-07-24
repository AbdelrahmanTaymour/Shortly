namespace Shortly.Core.DTOs.AuthDTOs;

public record TokenResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiry, DateTime RefreshTokenExpiry);