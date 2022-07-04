using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Notus.Encryption
{
    public abstract class DES
    {
        public static string Encrypt(string text, string key, string iv)
        {
            return Encoding.Default.GetString(
                Encrypt(
                    Encoding.UTF8.GetBytes(text),
                    Encoding.UTF8.GetBytes(key),
                    Encoding.UTF8.GetBytes(iv)
                )
            );
        }
        public static string Encrypt(string text, byte[] key, byte[] iv)
        {
            return Encoding.UTF8.GetString(
                Encrypt(Encoding.UTF8.GetBytes(text), key, iv)
            );
        }
        public static byte[] Encrypt(byte[] rawData, byte[] key, byte[] iv)
        {
            using (DESCryptoServiceProvider desCryptoService = new DESCryptoServiceProvider())
            {
                desCryptoService.Key = key;
                desCryptoService.IV = iv;
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    CryptoStream cryptoStream = new CryptoStream(memoryStream, desCryptoService.CreateEncryptor(), CryptoStreamMode.Write);
                    cryptoStream.Write(rawData, 0, rawData.Length);
                    cryptoStream.FlushFinalBlock();
                    cryptoStream.Close();
                    memoryStream.Close();
                    return memoryStream.ToArray();
                }
            }
        }

        public static string Decrypt(string encryptedText, string key, string iv)
        {
            return Encoding.UTF8.GetString(
                Decrypt(
                    Encoding.Default.GetBytes(encryptedText),
                    Encoding.UTF8.GetBytes(key),
                    Encoding.UTF8.GetBytes(iv)
                )
            );
        }
        public static string Decrypt(string encryptedText, byte[] key, byte[] iv)
        {
            return Encoding.UTF8.GetString(
                Decrypt(
                    Encoding.Default.GetBytes(encryptedText),
                    key,
                    iv
                )
            );
        }
        public static byte[] Decrypt(byte[] encryptedText, byte[] key, byte[] iv)
        {
            using (DESCryptoServiceProvider desCryptoService = new DESCryptoServiceProvider())
            {
                desCryptoService.Key = key;
                desCryptoService.IV = iv;
                using (MemoryStream msDecrypt = new MemoryStream())
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, desCryptoService.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        csDecrypt.Write(encryptedText, 0, encryptedText.Length);
                    }
                    return msDecrypt.ToArray();
                }
            }
        }
    }
}