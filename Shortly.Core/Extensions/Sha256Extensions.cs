using System.Security.Cryptography;
using System.Text;

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
        using (SHA256 sha256 = SHA256.Create())
        {
            // Compute the hash value from the UTF-8 encoded input string
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            
            // Convert the byte array to a lowercase hexadecimal string
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    /// <summary>
    /// Encrypts the specified plain text using AES encryption with a specified key and returns a Base64-encoded string.
    /// </summary>
    /// <param name="plainText">The text to encrypt. If null, returns null.</param>
    /// <param name="key">The 16-byte encryption key (default is "1234567890123456").</param>
    /// <returns>The encrypted text as a Base64-encoded string.</returns>
    public static string? Encrypt(string? plainText, string key = "1234567890123456")
    {
        if (plainText == null) return null;

        using (Aes aesAlg = Aes.Create())
        {
            // Set the key and IV for AES encryption
            aesAlg.Key = Encoding.UTF8.GetBytes(key);
            aesAlg.IV = new byte[aesAlg.BlockSize / 8];
            
            // Create an encryptor
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            // Encrypt the data
            using (var msEncrypt = new System.IO.MemoryStream())
            {
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }

                // Return the encrypted data as a Base64-encoded string
                return Convert.ToBase64String(msEncrypt.ToArray());
            }
        }
    }

    
    /// <summary>
    /// Decrypts a Base64-encoded AES-encrypted string using the specified key and returns the original plain text.
    /// </summary>
    /// <param name="cipherText">The Base64-encoded encrypted string. If null, returns null.</param>
    /// <param name="key">The 16-byte decryption key (must match the encryption key).</param>
    /// <returns>The decrypted plain text string.</returns>
    public static string? Decrypt(string? cipherText, string key = "1234567890123456")
    {
        if (cipherText == null) return null;

        using (Aes aesAlg = Aes.Create())
        {
            // Set the key and IV for AES decryption
            aesAlg.Key = Encoding.UTF8.GetBytes(key);
            aesAlg.IV = new byte[aesAlg.BlockSize / 8];
            
            // Create a decrypt
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
            
            // Decrypt the data
            using (var msDecrypt = new System.IO.MemoryStream(Convert.FromBase64String(cipherText)))
            using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
            {
                // Read the decrypted data from the StreamReader
                return srDecrypt.ReadToEnd();
            }
        }
    }
}