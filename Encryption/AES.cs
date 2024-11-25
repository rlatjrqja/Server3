﻿using System.Security.Cryptography;
using System.IO;

public class AES
{
    private readonly byte[] key = Convert.FromBase64String(
        "hOAI8BbC9ULx0ZjlGE0M9nIR8q3IvO+HXShg8opU6Ak=");
    private readonly byte[] iv = Convert.FromBase64String("4hiaOa17fk/1FiIfSwpHKQ==");

    public AES()
    {
        
    }

    public byte[] EncryptData(byte[] data)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Padding = PaddingMode.PKCS7;

            using MemoryStream memoryStream = new MemoryStream();
            using CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);

            cryptoStream.Write(data, 0, data.Length);
            cryptoStream.FlushFinalBlock();

            //Console.WriteLine("S " + memoryStream.ToArray().ToString());
            return memoryStream.ToArray();
        }
    }

    public byte[] DecryptData(byte[] encryptedData)
    {
        if (encryptedData == null || encryptedData.Length == 0)
        {
            throw new ArgumentException("Encrypted data cannot be null or empty.");
        }

        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Padding = PaddingMode.PKCS7;

            using MemoryStream memoryStream = new MemoryStream();
            using CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write);
            try
            {
                //Console.WriteLine("R " + memoryStream.ToArray().ToString());
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

}
