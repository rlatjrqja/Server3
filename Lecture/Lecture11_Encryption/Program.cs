using System.Security.Cryptography;

namespace Lecture11_Encryption
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                using (FileStream fs = new FileStream("TestData.txt", FileMode.OpenOrCreate))
                {
                    using (Aes aes = Aes.Create())
                    {
                        byte[] key_ =
                        {
                            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                            0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16
                        };
                        aes.Key = key_;

                        byte[] iv = aes.IV;
                        fs.Write(iv, 0, iv.Length);

                        using(CryptoStream cryptoStream = new (fs,aes.CreateEncryptor(),CryptoStreamMode.Write))
                        {
                            using(StreamWriter encryptWriter = new(cryptoStream))
                            {
                                encryptWriter.WriteLine("Hello~~~~");
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
