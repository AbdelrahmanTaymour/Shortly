namespace Shortly.Core.Abstractions.Authentication;

public interface ICookieService
{
    string? GetRefreshToken();
    void SetRefreshToken(string refreshToken, DateTime expiresAt);
    void DeleteRefreshToken();
}