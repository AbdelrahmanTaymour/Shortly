using System.Security.Cryptography;

namespace Shortly.Domain.ShortUrls;

/// <summary>Cryptographically secure and high-performance short code generator using stack allocation.</summary>
public sealed class ShortCodeGenerator : IShortCodeGenerator
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public ShortCode Generate(int length = 7)
    {
        if (length is < ShortCode.MinLength or > ShortCode.MaxLength)
            throw new ArgumentOutOfRangeException(nameof(length), length,
                $"Short code length must be between {ShortCode.MinLength} and {ShortCode.MaxLength}.");

        Span<byte> buffer = stackalloc byte[length];
        RandomNumberGenerator.Fill(buffer);

        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = Alphabet[buffer[i] % Alphabet.Length];

        return ShortCode.Create(new string(chars)).Value;
    }
}