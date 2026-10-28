namespace Shortly.Core.Abstractions.Authentication;

public sealed record AccessTokenResponse(string AccessToken, string RefreshToken, int ExpiresInSeconds);