using System;
using System.Text;

namespace Notus.Core.Encryption
{
    /// <summary>
    /// Helper methods for Encryption.
    /// </summary>
    public class Common
    {
        /// <summary>
        /// Creates key and nonce <see cref="byte"/>[] via specified hash <see cref="string"/>
        /// </summary>
        /// <param name="HashData">Hash Data <see cref="string"/></param>
        /// <returns>Returns key <see cref="byte"/>[] and nonce <see cref="byte"/>[].</returns>
        public static (byte[],byte[]) ChaCha20SecretKeyAndIV(string HashData)
        {
            byte[] hexByte=Notus.Core.Convert.Hex2Byte(HashData);
            byte[] key = new byte[32];
            byte[] nonce = new byte[12];
            Array.Copy(hexByte, 0, key, 0, key.Length);
            Array.Copy(hexByte, 48, nonce, 0, nonce.Length);
            return (key, nonce);
        }

        /// <summary>
        /// Encrypts Input Data <see cref="string"/> with Secret Key <see cref="string"/> and Secret Nonce <see cref="string"/>
        /// </summary>
        /// <param name="InputData">Data <see cref="string"/> to encrypt.</param>
        /// <param name="SecretKey">Secret key to be used.</param>
        /// <param name="SecretNonce">Secret nonce to be used.</param>
        /// <returns>Returns encrypted ChaCha20 <see cref="byte"/>[].</returns>
        public static byte[] EncryptWithChaCha20(string InputData, string SecretKey, string SecretNonce)
        {
            return EncryptWithChaCha20(Encoding.UTF8.GetBytes(InputData), Notus.Core.Convert.Hex2Byte(SecretKey), Notus.Core.Convert.Hex2Byte(SecretNonce));
        }

        /// <summary>
        /// Encrypts Input Data <see cref="byte"/>[] with Secret Key <see cref="string"/> and Secret Nonce <see cref="string"/>
        /// </summary>
        /// <param name="InputData">Data <see cref="byte"/>[] to encrypt.</param>
        /// <param name="SecretKey">Secret key to be used.</param>
        /// <param name="SecretNonce">Secret nonce to be used.</param>
        /// <returns>Returns encrypted ChaCha20 <see cref="byte"/>[].</returns>
        public static byte[] EncryptWithChaCha20(byte[] InputData, string SecretKey, string SecretNonce)
        {
            return EncryptWithChaCha20(InputData, Notus.Core.Convert.Hex2Byte(SecretKey), Notus.Core.Convert.Hex2Byte(SecretNonce));
        }

        /// <summary>
        /// Encrypts Input Data <see cref="byte"/>[] with Secret Key <see cref="byte"/>[] and Secret Nonce <see cref="byte"/>[]
        /// </summary>
        /// <param name="InputData">Data <see cref="byte"/>[] to encrypt.</param>
        /// <param name="SecretKey">Secret key to be used.</param>
        /// <param name="SecretNonce">Secret nonce to be used.</param>
        /// <returns>Returns encrypted ChaCha20 <see cref="byte"/>[].</returns>
        public static byte[] EncryptWithChaCha20(byte[] InputData, byte[] SecretKey, byte[] SecretNonce)
        {
            uint counter = 1;
            Notus.Core.Encryption.ChaCha20 forEncrypting = new Notus.Core.Encryption.ChaCha20(SecretKey, SecretNonce, counter);
            byte[] encryptedContent = new byte[InputData.Length];
            forEncrypting.EncryptBytes(encryptedContent, InputData);
            return encryptedContent;
        }

        /// <summary>
        /// Decrypts Input Data <see cref="string"/> with Secret Key <see cref="string"/> and Secret Nonce <see cref="string"/>
        /// </summary>
        /// <param name="InputData">Data <see cref="string"/> to decrypt.</param>
        /// <param name="SecretKey">Secret key to be used.</param>
        /// <param name="SecretNonce">Secret nonce to be used.</param>
        /// <returns>Returns decrpyted <see cref="byte"/>[].</returns>
        public static byte[] DecryptWithChaCha20(string InputData, string SecretKey, string SecretNonce)
        {
            return DecryptWithChaCha20(Encoding.UTF8.GetBytes(InputData), Notus.Core.Convert.Hex2Byte(SecretKey), Notus.Core.Convert.Hex2Byte(SecretNonce));
        }

        /// <summary>
        /// Decrypts Input Data <see cref="byte"/>[] with Secret Key <see cref="string"/> and Secret Nonce <see cref="string"/>
        /// </summary>
        /// <param name="InputData">Data <see cref="byte"/>[] to decrypt.</param>
        /// <param name="SecretKey">Secret key to be used.</param>
        /// <param name="SecretNonce">Secret nonce to be used.</param>
        /// <returns>Returns decrypted <see cref="byte"/>[].</returns>
        public static byte[] DecryptWithChaCha20(byte[] InputData, string SecretKey, string SecretNonce)
        {
            return DecryptWithChaCha20(InputData, Notus.Core.Convert.Hex2Byte(SecretKey), Notus.Core.Convert.Hex2Byte(SecretNonce));
        }

        /// <summary>
        /// Decrypts Input Data <see cref="byte"/>[] with Secret Key <see cref="byte"/>[] and Secret Nonce <see cref="byte"/>[]
        /// </summary>
        /// <param name="InputData">Data <see cref="byte"/>[] to encrypt.</param>
        /// <param name="SecretKey">Secret key to be used.</param>
        /// <param name="SecretNonce">Secret nonce to be used.</param>
        /// <returns>Returns decrypted <see cref="byte"/>[].</returns>
        public static byte[] DecryptWithChaCha20(byte[] InputData, byte[] SecretKey, byte[] SecretNonce)
        {
            uint counter = 1;
            Notus.Core.Encryption.ChaCha20 forEncrypting = new Notus.Core.Encryption.ChaCha20(SecretKey, SecretNonce, counter);
            byte[] encryptedContent = new byte[InputData.Length];
            forEncrypting.DecryptBytes(encryptedContent, InputData);
            return encryptedContent;
        }
    }
}
