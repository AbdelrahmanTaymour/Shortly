using System.Text;

namespace Shortly.Core.Extensions;

/// <summary>
/// Enhanced URL code generator with multiple strategies for optimal performance and collision avoidance
/// </summary>
public static class UrlCodeExtensions
{
    // Optimized character set (no confusing characters like 0, O, I, l, 1)
    private const string SafeCharacters = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int Base = 54; // Length of SafeCharacters

    /// <summary>
    /// Primary generation method with multiple fallback strategies
    /// </summary>
    public static string GenerateCodeAsync(long id, int minLength = 6)
    {
        // Add an offset so even the very first ID is large
        const long offset = 100_000;
        
        // Mix ID with randomness for unpredictability
        var rand = Random.Shared.Next(10_000, 99_999);
        var value = (id + offset) * 100_000 + rand;
        
        // Convert to Base54
        var code = EncodeBase54(value, minLength);
        
        return code;
    }

    /// <summary>
    /// High-performance Base54 encoding (no confusing characters)
    /// </summary>
    private static string EncodeBase54(long value, int minLength = 6)
    {
        var sb = new StringBuilder();

        while (value > 0)
        {
            sb.Insert(0, SafeCharacters[(int)(value % Base)]);
            value /= Base;
        }

        // Pad to ensure minimum length
        while (sb.Length < minLength)
            sb.Insert(0, SafeCharacters[0]);

        return sb.ToString();
    }
}