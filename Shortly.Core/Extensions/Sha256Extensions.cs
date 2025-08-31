using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Shortly.Core.Extensions;

/// <summary>
/// Provides cryptographic helper methods including SHA-256 hashing and AES encryption/decryption.
/// </summary>
public static class Sha256Extensions
{
    /// <summary>
    /// Computes the SHA-256 hash of a UTF-8 encoded input string and returns the result as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="input">The input string to hash.</param>
    /// <returns>A lowercase hexadecimal representation of the SHA-256 hash.</returns>
    public static string ComputeHash(string input)
    {
        // Create an instance of the SHA-256 algorithm
        using var sha256 = SHA256.Create();
        
        // Compute the hash value from the UTF-8 encoded input string
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

        // Convert the byte array to a lowercase hexadecimal string
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    /// <summary>
    /// Encrypts the specified plain text using AES encryption with a specified key and returns a Base64-encoded string.
    /// </summary>
    /// <param name="plainText">The text to encrypt. If null, returns null.</param>
    /// <param name="key">The 16-byte encryption key (default is "1234567890123456").</param>
    /// <returns>The encrypted text as a Base64-encoded string.</returns>
    public static string Encrypt(string plainText, string key = "1234567890123456")
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = Encoding.UTF8.GetBytes(key);
        aesAlg.IV = new byte[aesAlg.BlockSize / 8];

        var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

        using var msEncrypt = new MemoryStream();
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        // Make it URL-safe
        return WebEncoders.Base64UrlEncode(msEncrypt.ToArray());
    }


    /// <summary>
    /// Decrypts a Base64-encoded AES-encrypted string using the specified key and returns the original plain text.
    /// </summary>
    /// <param name="cipherText">The Base64-encoded encrypted string. If null, returns null.</param>
    /// <param name="key">The 16-byte decryption key (must match the encryption key).</param>
    /// <returns>The decrypted plain text string.</returns>
    public static string Decrypt(string cipherText, string key = "1234567890123456")
    {
         using var aesAlg = Aes.Create();
        aesAlg.Key = Encoding.UTF8.GetBytes(key);
        aesAlg.IV = new byte[aesAlg.BlockSize / 8];

        var decrypted = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

        var buffer = WebEncoders.Base64UrlDecode(cipherText);

        using var msDecrypt = new MemoryStream(buffer);
        using var csDecrypt = new CryptoStream(msDecrypt, decrypted, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        return srDecrypt.ReadToEnd();
    }
}