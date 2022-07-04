using System;
using System.Linq;
using System.Text;

namespace Notus.Encryption
{
    public class Common2
    {
        public static byte[] ArraySplit(string GenKey, int ArrayPart, bool xorWithRestOfThem = true)
        {
            return ArraySplit(Notus.Convert.Hex2Byte(GenKey), ArrayPart, xorWithRestOfThem);
        }
        public static byte[] ArraySplit(byte[] GenKeyArray, int ArrayPart,bool xorWithRestOfThem=true)
        {
            byte[] copydizi1 = new byte[ArrayPart];
            Array.Copy(GenKeyArray, copydizi1, ArrayPart);
            if (xorWithRestOfThem == true)
            {
                for (int a = GenKeyArray.Length - 1; a > ArrayPart - 1; a--)
                {
                    for (int b = 0; b < ArrayPart; b++)
                    {
                        copydizi1[b] = (byte)(copydizi1[b] ^ GenKeyArray[a]);
                        if (copydizi1[b] == 0)
                        {
                            copydizi1[b] = 127;
                        }
                    }
                }
            }
            return copydizi1;
        }

        // generate KEY and IV for AES, DES, ChaCha20
        public static string GenerateKey(string BlockKey, string SecretKey, string SecretIV)
        {
            return GenerateKey_SubRoutine(BlockKey, SecretKey, SecretIV);
        }
        public static byte[] GenerateKeyByte(string BlockKey, string SecretKey, string SecretIV)
        {
            return Notus.Convert.Hex2Byte(GenerateKey_SubRoutine(BlockKey, SecretKey, SecretIV));
        }

        public static byte[] EncryptWithAes(string InputData, string BlockKey, string SecretKey, string SecretIV)
        {
            return EncryptWithAes_SubRoutine(Encoding.UTF8.GetBytes(InputData), BlockKey, SecretKey, SecretIV);
        }
        public static byte[] EncryptWithAes(byte[] InputData, string BlockKey, string SecretKey, string SecretIV)
        {
            return EncryptWithAes_SubRoutine(InputData, BlockKey, SecretKey, SecretIV);
        }
        private static byte[] EncryptWithAes_SubRoutine(byte[] InputData, string BlockKey, string SecretKey, string SecretIV)
        {
            byte[] key = new byte[32];
            byte[] iv = new byte[16];
            byte[] byteResult = GenerateKeyByte(BlockKey, SecretKey, SecretIV);
            Array.Copy(byteResult, 0, key, 0, key.Length);
            Array.Copy(byteResult, 48, iv, 0, iv.Length);
            Notus.Encryption.AES aesObj = new Notus.Encryption.AES(key, iv);
            return aesObj.Encrypt(InputData);
        }

        public static byte[] DecryptWithAes(string InputData, string BlockKey, string SecretKey, string SecretIV)
        {
            return DecryptWithAes_SubRoutine(Encoding.UTF8.GetBytes(InputData), BlockKey, SecretKey, SecretIV);
        }
        public static byte[] DecryptWithAes(byte[] InputData, string BlockKey, string SecretKey, string SecretIV)
        {
            return DecryptWithAes_SubRoutine(InputData, BlockKey, SecretKey, SecretIV);
        }
        private static byte[] DecryptWithAes_SubRoutine(byte[] InputData, string BlockKey, string SecretKey, string SecretIV)
        {
            byte[] key = new byte[32];
            byte[] iv = new byte[16];
            byte[] byteResult = GenerateKeyByte(BlockKey, SecretKey, SecretIV);
            Array.Copy(byteResult, 0, key, 0, key.Length);
            Array.Copy(byteResult, 48, iv, 0, iv.Length);
            Notus.Encryption.AES aesObj = new Notus.Encryption.AES(key, iv);
            return aesObj.DecryptToByte(InputData);
        }


        public static byte[] EncryptWithDes(string InputData, string BlockKey, string SecretKey, string SecretIV)
        {
            return EncryptWithDes_SubRoutine(Encoding.UTF8.GetBytes(InputData), BlockKey, SecretKey, SecretIV);
        }
        public static byte[] EncryptWithDes(byte[] InputData, string BlockKey, string SecretKey, string SecretIV)
        {
            return EncryptWithDes_SubRoutine(InputData, BlockKey, SecretKey, SecretIV);
        }
        private static byte[] EncryptWithDes_SubRoutine(byte[] InputData, string BlockKey, string SecretKey, string SecretIV)
        {
            byte[] key = new byte[8];
            byte[] iv = new byte[8];
            byte[] byteResult = GenerateKeyByte(BlockKey, SecretKey, SecretIV);
            Array.Copy(byteResult, 0, key, 0, key.Length);
            Array.Copy(byteResult, 48, iv, 0, iv.Length);
            return Notus.Encryption.DES.Encrypt(InputData, key, iv);
        }
        public static byte[] DecryptWithDes(string InputData, string BlockKey, string SecretKey, string SecretIV)
        {
            return DecryptWithDes_SubRoutine(Encoding.UTF8.GetBytes(InputData), BlockKey, SecretKey, SecretIV);
        }
        public static byte[] DecryptWithDes(byte[] InputData, string BlockKey, string SecretKey, string SecretIV)
        {
            return DecryptWithDes_SubRoutine(InputData, BlockKey, SecretKey, SecretIV);
        }
        public static byte[] DecryptDesWithString(string InputData, string BlockKey, string SecretKey, string SecretIV)
        {
            return DecryptWithDes_SubRoutine(Encoding.UTF8.GetBytes(InputData), BlockKey, SecretKey, SecretIV);
        }
        public static byte[] DecryptDesWithByte(byte[] InputData, string BlockKey, string SecretKey, string SecretIV)
        {
            return DecryptWithDes_SubRoutine(InputData, BlockKey, SecretKey, SecretIV);
        }
        private static byte[] DecryptWithDes_SubRoutine(byte[] InputData, string BlockKey, string SecretKey, string SecretIV)
        {
            byte[] key = new byte[8];
            byte[] iv = new byte[8];
            byte[] byteResult = GenerateKeyByte(BlockKey, SecretKey, SecretIV);
            Array.Copy(byteResult, 0, key, 0, key.Length);
            Array.Copy(byteResult, 48, iv, 0, iv.Length);
            
            return Notus.Encryption.DES.Decrypt(InputData, key, iv);
        }


        public static byte[] EncryptWithChaCha20(string InputData, string BlockKey, string SecretKey, string SecretIV)
        {
            return EncryptWithChaCha20_SubRoutine(Encoding.UTF8.GetBytes(InputData), BlockKey, SecretKey, SecretIV);
        }
        public static byte[] EncryptWithChaCha20(byte[] InputData, string BlockKey, string SecretKey, string SecretIV)
        {
            return EncryptWithChaCha20_SubRoutine(InputData, BlockKey, SecretKey, SecretIV);
        }
        private static byte[] EncryptWithChaCha20_SubRoutine(byte[] InputData, string BlockKey, string SecretKey, string SecretIV)
        {
            byte[] key = new byte[32];
            byte[] nonce = new byte[12];
            uint counter = 1;
            byte[] byteResult = GenerateKeyByte(BlockKey, SecretKey, SecretIV);
            Array.Copy(byteResult, 0, key, 0, key.Length);
            Array.Copy(byteResult, 48, nonce, 0, nonce.Length);
            Notus.Encryption.ChaCha20 forEncrypting = new Notus.Encryption.ChaCha20(key, nonce, counter);
            byte[] encryptedContent = new byte[InputData.Length];
            forEncrypting.EncryptBytes(encryptedContent, InputData);
            return encryptedContent;
        }
        
        public static byte[] DecryptWithChaCha20(string InputData, string BlockKey, string SecretKey, string SecretIV)
        {
            return DecryptWithChaCha20_SubRoutine(Encoding.UTF8.GetBytes(InputData), BlockKey, SecretKey, SecretIV);
        }
        public static byte[] DecryptWithChaCha20(byte[] InputData, string BlockKey, string SecretKey, string SecretIV)
        {
            return DecryptWithChaCha20_SubRoutine(InputData, BlockKey, SecretKey, SecretIV);
        }
        private static byte[] DecryptWithChaCha20_SubRoutine(byte[] InputData, string BlockKey, string SecretKey, string SecretIV)
        {
            byte[] key = new byte[32];
            byte[] nonce = new byte[12];
            uint counter = 1;
            byte[] byteResult = GenerateKeyByte(BlockKey, SecretKey, SecretIV);
            Array.Copy(byteResult, 0, key, 0, key.Length);
            Array.Copy(byteResult, 48, nonce, 0, nonce.Length);
            Notus.Encryption.ChaCha20 forEncrypting = new Notus.Encryption.ChaCha20(key, nonce, counter);
            byte[] encryptedContent = new byte[InputData.Length];
            forEncrypting.DecryptBytes(encryptedContent, InputData);
            return encryptedContent;
        }
        
        
        private static string GenerateKey_SubRoutine(string BlockKey,string SecretKey,string SecretIV)
        {
            
            string BlockKeyBase64Str = Notus.Convert.ToBase64(BlockKey + SecretKey + SecretIV);

            string Md5_Ozet = Notus.Variable.Constant.NonceDelimeterChar,
                Sha1_Ozet = Notus.Variable.Constant.NonceDelimeterChar;

            for(int a = 0; a < 4; a++)
            {
                Md5_Ozet = new Notus.Hash().CommonHash("md5", 
                    Md5_Ozet +
                    Notus.Variable.Constant.NonceDelimeterChar + 
                    BlockKeyBase64Str +
                    Notus.Variable.Constant.NonceDelimeterChar +
                    SecretKey
                );
            }
            for(int a = 0; a < 4; a++)
            {
                Sha1_Ozet = new Notus.Hash().CommonHash("sha1", 
                    Sha1_Ozet +
                    Notus.Variable.Constant.NonceDelimeterChar +
                    BlockKeyBase64Str +
                    Notus.Variable.Constant.NonceDelimeterChar +
                    SecretKey
                );
            }

            
            string[] MD5_Dizi = Notus.Toolbox.Text.SplitByLength(Md5_Ozet, 4).ToArray();
            string[] Sha1_Dizi = Notus.Toolbox.Text.SplitByLength(Sha1_Ozet, 5).ToArray();
            string[] Sha256_Dizi = Notus.Toolbox.Text.SplitByLength(
                new Notus.Hash().CommonHash("sha256", 
                Sha1_Ozet +
                Notus.Variable.Constant.NonceDelimeterChar + 
                Md5_Ozet +
                Notus.Variable.Constant.NonceDelimeterChar + 
                BlockKeyBase64Str +
                Notus.Variable.Constant.NonceDelimeterChar +
                SecretKey
            ), 8).ToArray();
            string[] Sha512_Dizi = Notus.Toolbox.Text.SplitByLength(
                new Notus.Hash().CommonHash("sha512", 
                Md5_Ozet +
                Notus.Variable.Constant.NonceDelimeterChar + 
                Sha1_Ozet +
                Notus.Variable.Constant.NonceDelimeterChar + 
                BlockKeyBase64Str +
                Notus.Variable.Constant.NonceDelimeterChar +
                SecretKey
            ), 16).ToArray();

            string YedSonuc = "";
            for (int a = 0; a < 8; a++)
            {
                if (a % 2 == 0)
                {
                    YedSonuc = YedSonuc + Notus.Toolbox.Text.ReverseString(MD5_Dizi[a]) + 
                        Sha1_Dizi[a] +
                        Notus.Toolbox.Text.ReverseString(Sha256_Dizi[a]) + 
                        Sha512_Dizi[a];
                }
                else
                {
                    YedSonuc = YedSonuc + 
                        MD5_Dizi[a] +
                        Notus.Toolbox.Text.ReverseString(Sha1_Dizi[a]) + 
                        Sha256_Dizi[a] +
                        Notus.Toolbox.Text.ReverseString(Sha512_Dizi[a]);
                }
            }
            string NihaiSonuc = "";
            for (int a = 0; a < YedSonuc.Length - 2; a++)
            {
                if (a == 0)
                {
                    NihaiSonuc = YedSonuc.Substring(a, 1);
                }
                else
                {
                    if (NihaiSonuc.Substring(NihaiSonuc.Length - 1, 1) != YedSonuc.Substring(a, 1))
                    {
                        NihaiSonuc = NihaiSonuc + 
                            YedSonuc.Substring(a, 1);
                    }
                }
            }
            
            return Notus.Toolbox.Text.ReplaceChar(
                NihaiSonuc,
                Notus.Variable.Constant.DefaultHexAlphabetString,
                Notus.Toolbox.Text.Iteration(16,
                    new Notus.Hash().CommonHash("md5", SecretIV) +
                    new Notus.Hash().CommonHash("sha1", SecretIV)
                )
            );
        }
    }
}
