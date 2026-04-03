using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Ccd.Server.Helpers;

namespace Ccd.Server.Deduplication;

public static class IdEncryptor
{
    private static byte[] GetKey()
    {
        var secret = StaticConfiguration.EncryptionKey
            ?? throw new InvalidOperationException(
                "ENCRYPTION_KEY environment variable is not configured."
            );
        return SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    }

    private static byte[] GetIv(byte[] key)
    {
        // Derive a deterministic IV from the key so same plaintext → same ciphertext
        return SHA256.HashData(key)[..16];
    }

    public static string Encrypt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var key = GetKey();
        var iv = GetIv(key);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(value.Trim());
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        return Convert.ToBase64String(cipherBytes);
    }

    public static string Decrypt(string encrypted)
    {
        if (string.IsNullOrWhiteSpace(encrypted))
            return encrypted;

        var key = GetKey();
        var iv = GetIv(key);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(encrypted);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
