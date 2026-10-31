using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Shortly.Core.Abstractions.Authentication;

namespace Shortly.Infrastructure.Authentication;

internal sealed class TokenHasher : ITokenHasher
{
    public string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    public string Hash(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
}