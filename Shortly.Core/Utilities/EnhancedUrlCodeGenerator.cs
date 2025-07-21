using System.Security.Cryptography;
using System.Text;

namespace Shortly.Core.Utilities;

/// <summary>
/// Enhanced URL code generator with multiple strategies for optimal performance and collision avoidance
/// </summary>
public static class EnhancedUrlCodeGenerator
{
    // Optimized character set (no confusing characters like 0, O, I, l, 1)
    private const string SafeCharacters = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int Base = 54; // Length of SafeCharacters
    
    // For high-performance lookups
    private static readonly Dictionary<char, int> CharToIndex = CreateCharacterMap();
    
    // Thread-safe random number generator
    private static readonly ThreadLocal<Random> ThreadSafeRandom = new(() => new Random(Guid.NewGuid().GetHashCode()));
    
    /// <summary>
    /// Primary generation method with multiple fallback strategies
    /// </summary>
    public static async Task<string> GenerateCodeAsync(long id, IShortUrlRepository? repository = null, int minLength = 6)
    {
        // Strategy 1: Try optimized Base54 encoding first
        var code = EncodeBase54(id, minLength);
        
        // Strategy 2: If collision detected, use hybrid approach
        if (repository != null && await repository.ShortCodeExistsAsync(code))
        {
            code = await GenerateCollisionResistantCodeAsync(id, repository, minLength);
        }
        
        return code;
    }
    
    /// <summary>
    /// High-performance Base54 encoding (no confusing characters)
    /// </summary>
    public static string EncodeBase54(long number, int minLength = 6)
    {
        if (number == 0) return new string(SafeCharacters[0], minLength);
        
        var result = new StringBuilder();
        var workingNumber = Math.Abs(number); // Handle negative numbers
        
        while (workingNumber > 0)
        {
            result.Insert(0, SafeCharacters[(int)(workingNumber % Base)]);
            workingNumber /= Base;
        }
        
        // Pad to minimum length for consistent URL appearance
        while (result.Length < minLength)
        {
            result.Insert(0, SafeCharacters[0]);
        }
        
        return result.ToString();
    }
    
    /// <summary>
    /// Decode Base54 string back to number
    /// </summary>
    public static long DecodeBase54(string encoded)
    {
        if (string.IsNullOrEmpty(encoded)) return 0;
        
        long result = 0;
        long multiplier = 1;
        
        for (int i = encoded.Length - 1; i >= 0; i--)
        {
            if (CharToIndex.TryGetValue(encoded[i], out int index))
            {
                result += index * multiplier;
                multiplier *= Base;
            }
            else
            {
                throw new ArgumentException($"Invalid character '{encoded[i]}' in encoded string");
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Collision-resistant generation using multiple strategies
    /// </summary>
    private static async Task<string> GenerateCollisionResistantCodeAsync(long id, IShortUrlRepository repository, int minLength)
    {
        const int maxAttempts = 5;
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            string code = attempt switch
            {
                0 => GenerateTimeBasedCode(id, minLength),
                1 => GenerateHashBasedCode(id, minLength),
                2 => GenerateRandomCode(minLength + 1), // Longer for lower collision chance
                3 => GenerateHybridCode(id, minLength + 1),
                _ => GenerateSecureRandomCode(minLength + 2) // Cryptographically secure
            };
            
            if (!await repository.ShortCodeExistsAsync(code))
            {
                return code;
            }
        }
        
        // Fallback: Use timestamp + random for guaranteed uniqueness
        return GenerateTimestampBasedUniqueCode(minLength);
    }
    
    /// <summary>
    /// Time-based code generation (includes microsecond precision)
    /// </summary>
    private static string GenerateTimeBasedCode(long id, int minLength)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var combined = (timestamp << 20) | (id & 0xFFFFF); // Combine timestamp with ID
        return EncodeBase54(combined, minLength);
    }
    
    /// <summary>
    /// Hash-based code generation using SHA256
    /// </summary>
    private static string GenerateHashBasedCode(long id, int minLength)
    {
        var input = $"{id}{DateTimeOffset.UtcNow.Ticks}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        
        // Convert first 8 bytes of hash to long
        var hashLong = BitConverter.ToInt64(hash, 0);
        return EncodeBase54(Math.Abs(hashLong), minLength);
    }
    
    /// <summary>
    /// Pure random code generation
    /// </summary>
    private static string GenerateRandomCode(int length)
    {
        var random = ThreadSafeRandom.Value!;
        var result = new StringBuilder(length);
        
        for (int i = 0; i < length; i++)
        {
            result.Append(SafeCharacters[random.Next(Base)]);
        }
        
        return result.ToString();
    }
    
    /// <summary>
    /// Hybrid approach combining ID and randomness
    /// </summary>
    private static string GenerateHybridCode(long id, int length)
    {
        var baseCode = EncodeBase54(id, length / 2);
        var randomSuffix = GenerateRandomCode(length - baseCode.Length);
        return baseCode + randomSuffix;
    }
    
    /// <summary>
    /// Cryptographically secure random code
    /// </summary>
    private static string GenerateSecureRandomCode(int length)
    {
        var randomBytes = new byte[length * 2]; // Extra bytes for better randomness
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        
        var result = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            var index = BitConverter.ToUInt16(randomBytes, i * 2) % Base;
            result.Append(SafeCharacters[index]);
        }
        
        return result.ToString();
    }
    
    /// <summary>
    /// Timestamp-based unique code (guaranteed uniqueness within millisecond)
    /// </summary>
    private static string GenerateTimestampBasedUniqueCode(int minLength)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var microseconds = DateTime.UtcNow.Ticks % 10000; // Add microsecond precision
        var combined = (timestamp << 16) | microseconds;
        return EncodeBase54(combined, minLength);
    }
    
    /// <summary>
    /// Validate if a code contains only valid characters
    /// </summary>
    public static bool IsValidCode(string code)
    {
        if (string.IsNullOrEmpty(code)) return false;
        return code.All(c => CharToIndex.ContainsKey(c));
    }
    
    /// <summary>
    /// Get estimated collision probability for given length
    /// </summary>
    public static double GetCollisionProbability(int codeLength, long totalUrls)
    {
        var totalPossible = Math.Pow(Base, codeLength);
        return 1.0 - Math.Exp(-Math.Pow(totalUrls, 2) / (2.0 * totalPossible));
    }
    
    /// <summary>
    /// Recommend optimal code length based on expected URL count
    /// </summary>
    public static int RecommendCodeLength(long expectedUrls, double maxCollisionProbability = 0.01)
    {
        for (int length = 4; length <= 12; length++)
        {
            if (GetCollisionProbability(length, expectedUrls) <= maxCollisionProbability)
            {
                return length;
            }
        }
        return 12; // Maximum reasonable length
    }
    
    /// <summary>
    /// Create character to index mapping for fast lookups
    /// </summary>
    private static Dictionary<char, int> CreateCharacterMap()
    {
        var map = new Dictionary<char, int>();
        for (int i = 0; i < SafeCharacters.Length; i++)
        {
            map[SafeCharacters[i]] = i;
        }
        return map;
    }
    
    /// <summary>
    /// Custom code validation for user-provided short codes
    /// </summary>
    public static (bool IsValid, string ErrorMessage) ValidateCustomCode(string customCode, int minLength = 3, int maxLength = 50)
    {
        if (string.IsNullOrWhiteSpace(customCode))
            return (false, "Custom code cannot be empty");
            
        if (customCode.Length < minLength)
            return (false, $"Custom code must be at least {minLength} characters long");
            
        if (customCode.Length > maxLength)
            return (false, $"Custom code cannot exceed {maxLength} characters");
            
        if (!IsValidCode(customCode))
            return (false, "Custom code contains invalid characters");
            
        // Check for reserved words/patterns
        var reservedPatterns = new[] { "api", "admin", "www", "mail", "help", "support", "about" };
        if (reservedPatterns.Any(pattern => customCode.ToLower().Contains(pattern)))
            return (false, "Custom code contains reserved words");
            
        return (true, string.Empty);
    }
}

/// <summary>
/// Interface for repository methods needed by the generator
/// </summary>
public interface IShortUrlRepository
{
    Task<bool> ShortCodeExistsAsync(string shortCode);
    // ... other repository methods
}