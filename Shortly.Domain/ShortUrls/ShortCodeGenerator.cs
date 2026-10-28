using System.Security.Cryptography;
using Shortly.Domain.Abstractions;

namespace Shortly.Domain.ShortUrls;

public sealed class ShortCodeGenerator
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public ShortCode Generate(int length = 7)
    {
        Span<byte> buffer = stackalloc byte[length];
        RandomNumberGenerator.Fill(buffer);

        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = Alphabet[buffer[i] % Alphabet.Length];

        return ShortCode.Create(new string(chars)).Value;
    }
}