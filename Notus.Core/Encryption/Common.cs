using System;
using System.Linq;
using System.Text;

namespace Notus.Core.Encryption
{
    public class Common
    {
        public static byte[] EncryptWithChaCha20(string InputData, string SecretKey, string SecretNonce)
        {
            return EncryptWithChaCha20(Encoding.UTF8.GetBytes(InputData), Notus.Core.Convert.Hex2Byte(SecretKey), Notus.Core.Convert.Hex2Byte(SecretNonce));
        }
        public static byte[] EncryptWithChaCha20(byte[] InputData, string SecretKey, string SecretNonce)
        {
            return EncryptWithChaCha20(InputData, Notus.Core.Convert.Hex2Byte(SecretKey), Notus.Core.Convert.Hex2Byte(SecretNonce));
        }
        public static byte[] EncryptWithChaCha20(byte[] InputData, byte[] SecretKey, byte[] SecretNonce)
        {
            //byte[] key = new byte[32];
            //byte[] nonce = new byte[12];
            uint counter = 1;
            //byte[] byteResult = Notus.Kernel.Encryption.Common.GenerateKeyByte(BlockKey, SecretKey, SecretIV);
            //Array.Copy(byteResult, 0, key, 0, key.Length);
            //Array.Copy(byteResult, 48, nonce, 0, nonce.Length);
            Notus.Core.Encryption.ChaCha20 forEncrypting = new Notus.Core.Encryption.ChaCha20(SecretKey, SecretNonce, counter);
            byte[] encryptedContent = new byte[InputData.Length];
            forEncrypting.EncryptBytes(encryptedContent, InputData);
            return encryptedContent;
        }
        public static byte[] DecryptWithChaCha20(string InputData, string SecretKey, string SecretNonce)
        {
            return DecryptWithChaCha20(Encoding.UTF8.GetBytes(InputData), Notus.Core.Convert.Hex2Byte(SecretKey), Notus.Core.Convert.Hex2Byte(SecretNonce));
        }
        public static byte[] DecryptWithChaCha20(byte[] InputData, string SecretKey, string SecretNonce)
        {
            return DecryptWithChaCha20(InputData, Notus.Core.Convert.Hex2Byte(SecretKey), Notus.Core.Convert.Hex2Byte(SecretNonce));
        }
        public static byte[] DecryptWithChaCha20(byte[] InputData, byte[] SecretKey, byte[] SecretNonce)
        {
            uint counter = 1;
            /*
            byte[] key = new byte[32];
            byte[] nonce = new byte[12];
            byte[] byteResult = Notus.Kernel.Encryption.Common.GenerateKeyByte(BlockKey, SecretKey, SecretIV);
            Array.Copy(byteResult, 0, key, 0, key.Length);
            Array.Copy(byteResult, 48, nonce, 0, nonce.Length);
            */
            Notus.Core.Encryption.ChaCha20 forEncrypting = new Notus.Core.Encryption.ChaCha20(SecretKey, SecretNonce, counter);
            byte[] encryptedContent = new byte[InputData.Length];
            forEncrypting.DecryptBytes(encryptedContent, InputData);
            return encryptedContent;
        }
    }
}
