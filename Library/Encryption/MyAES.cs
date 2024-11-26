using System.Security.Cryptography;
using System.IO;

public class MyAES
{
    private readonly byte[] key = Convert.FromBase64String("hOAI8BbC9ULx0ZjlGE0M9nIR8q3IvO+HXShg8opU6Ak=");
    private readonly byte[] iv = Convert.FromBase64String("4hiaOa17fk/1FiIfSwpHKQ==");

    public byte[] EncryptData(byte[] data)
    {
        Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Padding = PaddingMode.PKCS7;

        using MemoryStream memoryStream = new();
        using CryptoStream cryptoStream = new(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);

        cryptoStream.Write(data, 0, data.Length);
        cryptoStream.FlushFinalBlock();
        return memoryStream.ToArray();
    }

    public byte[] DecryptData(byte[] encryptedData)
    {
        Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Padding = PaddingMode.PKCS7;

        using MemoryStream memoryStream = new();
        using CryptoStream cryptoStream = new(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write);

        try
        {
            cryptoStream.Write(encryptedData, 0, encryptedData.Length);
            cryptoStream.FlushFinalBlock();
            return memoryStream.ToArray();
        }
        catch (CryptographicException ex)
        {
            Console.WriteLine($"Decryption failed: {ex.Message}");
            throw;
        }
    }

}