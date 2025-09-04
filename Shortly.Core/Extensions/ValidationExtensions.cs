using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace Shortly.Core.Extensions;

public static partial class ValidationExtensions
{
    // Pre-compiled regex patterns for better performance
    private static readonly Regex NamePattern = MyNameRegex();
    private static readonly Regex UsernamePattern = MyUsernameRegex();

    // Used FrozenSet for better performance
    private static readonly FrozenSet<string> ValidImageExtensions = new[]
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);


    // Cached time zones with lazy initialization
    private static readonly Lazy<FrozenSet<string>> ValidTimeZones = new(() =>
        TimeZoneInfo.GetSystemTimeZones()
            .Select(tz => tz.Id)
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Validates if the name contains only allowed characters (letters, spaces, hyphens, apostrophes, and periods).
    /// </summary>
    public static bool IsValidName(this string name)
    {
        return !string.IsNullOrWhiteSpace(name) && NamePattern.IsMatch(name);
    }

    

    /// <summary>
    /// Validates if the username contains only allowed characters (letters, numbers, underscores, hyphens, and periods).
    /// </summary>
    public static bool IsValidUsername(this string username)
    {
        return !string.IsNullOrWhiteSpace(username) && UsernamePattern.IsMatch(username);
    }

    /// <summary>
    /// Validates if the URL is a valid image URL with supported extensions.
    /// </summary>
    public static bool IsValidImageUrl(this string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        // Fast URI validation
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return false;

        // Fast extension check using ReadOnlySpan to avoid string allocations
        var urlSpan = url.AsSpan();
        var lastDotIndex = urlSpan.LastIndexOf('.');

        if (lastDotIndex == -1 || lastDotIndex == urlSpan.Length - 1)
            return false;

        var extension = urlSpan.Slice(lastDotIndex);
        return ValidImageExtensions.Contains(extension.ToString());
    }

    /// <summary>
    /// Validates if the time zone identifier is valid.
    /// </summary>
    public static bool IsValidTimeZone(this string? timeZone)
    {
        return string.IsNullOrWhiteSpace(timeZone) || ValidTimeZones.Value.Contains(timeZone);
    }

    /// <summary>
    /// Validates password complexity (at least one uppercase, lowercase, digit, and special character).
    /// </summary>
    public static bool HasPasswordComplexity(this string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        bool hasUpper = false, hasLower = false, hasDigit = false, hasSpecial = false;

        foreach (var c in password)
        {
            if (char.IsUpper(c)) hasUpper = true;
            else if (char.IsLower(c)) hasLower = true;
            else if (char.IsDigit(c)) hasDigit = true;
            else hasSpecial = true;

            // Early exit if all conditions are met
            if (hasUpper && hasLower && hasDigit && hasSpecial)
                return true;
        }

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }

    /// <summary>
    /// Checks if a DateTime is in the future.
    /// </summary>
    public static bool IsInFuture(this DateTime? dateTime)
    {
        return dateTime > DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if a DateTime is in the future.
    /// </summary>
    public static bool IsInFuture(this DateTime dateTime)
    {
        return dateTime > DateTime.UtcNow;
    }

    /// <summary>
    /// Check if the URL is valid 
    /// </summary>
    public static bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return false;

        try
        {
            var uri = new Uri(url);
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Validates that the token is a properly formatted base64 string.
    /// </summary>
    /// <param name="token">The refresh token to validate.</param>
    /// <returns>True if the token is valid base64, false otherwise.</returns>
    /// <remarks>
    /// This validation helps catch malformed tokens early and provides better user experience
    /// by rejecting obviously invalid tokens before making database calls.
    /// </remarks>
    public static bool BeValidBase64String(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        try
        {
            // Check if the string length is valid for base64 (must be multiple of 4)
            if (token.Length % 4 != 0)
                return false;

            // Attempt to convert from base64 - this will throw if invalid
            Convert.FromBase64String(token);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Ensures the token doesn't contain whitespace characters that could indicate tampering or corruption.
    /// </summary>
    /// <param name="token">The refresh token to validate.</param>
    /// <returns>True if the token contains no whitespace, false otherwise.</returns>
    /// <remarks>
    /// Base64 tokens should not contain spaces, tabs, or newlines. The presence of whitespace
    /// often indicates the token has been corrupted during transmission or storage.
    /// </remarks>
    public static bool BeNotContainWhitespace(string token)
    {
        return !string.IsNullOrEmpty(token) && !token.Any(char.IsWhiteSpace);
    }

    [GeneratedRegex(@"^[a-zA-Z\s\-'\.]+$", RegexOptions.Compiled)]
    private static partial Regex MyNameRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9_\-\.]+$", RegexOptions.Compiled)]
    private static partial Regex MyUsernameRegex();
}