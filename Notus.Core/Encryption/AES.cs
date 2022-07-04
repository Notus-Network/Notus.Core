using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Notus.Encryption
{
    public class AES
    {
        //firmaya göre değiştirilebilir
        //firmaya göre değiştirilebilir
        //firmaya göre değiştirilebilir
        private static readonly int[] ModulusOrder = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
        private const int SHAKE_ARRAY_SIZE = 255;
        private const int IV_STARTING_POINT = 25;
        private const int KEY_STARTING_POINT = 190;

        private const int IV_STARTING_POINT_BYTE = 15;
        private const int KEY_STARTING_POINT_BYTE = 70;


        private static RijndaelManaged rijndael = new RijndaelManaged();
        private static System.Text.UnicodeEncoding unicodeEncoding = new UnicodeEncoding();

        private const int CHUNK_SIZE = 128;
        private const int KEY_SIZE = 32;
        private const int IV_SIZE = 16;
        private static int[] NumberArray = new int[SHAKE_ARRAY_SIZE];
        public static string GetIvValue(int[] nArray)
        {
            byte[] keyArray = new byte[IV_SIZE];
            for (byte a = 0; a < IV_SIZE; a++)
            {
                keyArray[a] = (byte)nArray[a + IV_STARTING_POINT];
            }
            return System.Convert.ToBase64String(keyArray);
        }
        public static string GetKeyValue(int[] nArray)
        {
            byte[] keyArray = new byte[KEY_SIZE];
            for (int a = 0; a < KEY_SIZE; a++)
            {
                keyArray[a] = (byte)nArray[a + KEY_STARTING_POINT];
            }
            return System.Convert.ToBase64String(keyArray);
        }
        public static byte[] GetKeyValueByte(byte[] nArray)
        {
            byte[] keyArray = new byte[KEY_SIZE];
            for (int a = 0; a < KEY_SIZE; a++)
            {
                keyArray[a] = nArray[a + KEY_STARTING_POINT_BYTE];
            }
            return keyArray;
            //return Convert.ToBase64String(keyArray);
        }
        public static byte[] GetIvValueByte(byte[] nArray)
        {
            byte[] keyArray = new byte[IV_SIZE];
            for (byte a = 0; a < IV_SIZE; a++)
            {
                keyArray[a] = nArray[a + IV_STARTING_POINT_BYTE];
            }
            return keyArray;
            //return Convert.ToBase64String(keyArray);
        }
        private static int DefineNumberToArray(int arrayPosition, int divider, int result)
        {
            for (int i = 0; i < SHAKE_ARRAY_SIZE; i++)
            {
                if (i % divider == result)
                {
                    NumberArray[arrayPosition] = (byte)i;
                    arrayPosition++;
                }
            }
            return arrayPosition;
        }
        public static int[] FillArray(string hexValue)
        {
            int arrayPosition = 0;
            int dividerNumber = ModulusOrder.Length;
            for (int i = 0; i < ModulusOrder.Length; i++)
            {
                int mValue = ModulusOrder[i];
                arrayPosition = DefineNumberToArray(arrayPosition, dividerNumber, mValue);
            }
            for (int a = 0; a < hexValue.Length; a += 4)
            {
                int baslangic = System.Convert.ToInt32(hexValue.Substring(a + 0, 1), 16);
                int kacinci = System.Convert.ToInt32(hexValue.Substring(a + 1, 1), 16);
                int kacTane = System.Convert.ToInt32(hexValue.Substring(a + 2, 1), 16);
                int dongu = System.Convert.ToInt32(hexValue.Substring(a + 3, 1), 16);
                baslangic = (baslangic == 0 ? 5 : baslangic);
                kacinci = (kacinci == 0 ? 5 : kacinci);
                kacTane = (kacTane == 0 ? 5 : kacTane);
                dongu = (dongu == 0 ? 5 : dongu);

                for (int i = 0; i < dongu; i++)
                {
                    int[] rightArray1 = NumberArray.Skip(baslangic).Take(SHAKE_ARRAY_SIZE).ToArray();
                    int[] leftArray1 = NumberArray.Take(baslangic).Reverse().ToArray();
                    int[] step1 = rightArray1.Concat(leftArray1).ToArray();

                    int[] leftArray2 = step1.Take(kacinci).ToArray();
                    int[] middleArray2 = step1.Skip(kacinci).Take(kacTane).ToArray();
                    int[] rightArray2 = step1.Skip(kacinci + kacTane).Take(SHAKE_ARRAY_SIZE).ToArray();

                    int[] step2 = rightArray2.Concat(middleArray2).ToArray();
                    int[] lastStep = step2.Concat(leftArray2).ToArray();

                    NumberArray = lastStep;
                }
            }
            return NumberArray;
        }
        public static int[] FillArray2(string hexValue)
        {
            int arrayPosition = 0;
            int dividerNumber = ModulusOrder.Length;
            for (int i = 0; i < ModulusOrder.Length; i++)
            {
                int mValue = ModulusOrder[i];
                arrayPosition = DefineNumberToArray(arrayPosition, dividerNumber, mValue);
            }
            for (int a = 0; a < hexValue.Length; a += 4)
            {
                int baslangic = System.Convert.ToInt32(hexValue.Substring(a + 0, 1), 16);
                int kacinci = System.Convert.ToInt32(hexValue.Substring(a + 1, 1), 16);
                int kacTane = System.Convert.ToInt32(hexValue.Substring(a + 2, 1), 16);
                int dongu = System.Convert.ToInt32(hexValue.Substring(a + 3, 1), 16);

                baslangic = (baslangic == 0 ? 5 : baslangic);
                kacinci = (kacinci == 0 ? 5 : kacinci);
                kacTane = (kacTane == 0 ? 5 : kacTane);
                dongu = (dongu == 0 ? 5 : dongu);

                for (int i = 0; i < dongu; i++)
                {
                    int[] numberBuffer1 = new int[SHAKE_ARRAY_SIZE];
                    int outCount = 0;
                    for (int c = baslangic; c < SHAKE_ARRAY_SIZE; c++)
                    {
                        numberBuffer1[outCount] = NumberArray[c];
                        outCount++;
                    }

                    //burayi kontrol et
                    //burayi kontrol et
                    //burayi kontrol et
                    //burayi kontrol et
                    //burayi kontrol et
                    //burayi kontrol et
                    for (int c = baslangic; c > 0; c--)
                    {
                        numberBuffer1[outCount] = NumberArray[c];
                        outCount++;
                    }

                    NumberArray = numberBuffer1;

                    //ikinci sıralama işlemi
                    int[] numberBuffer2 = new int[SHAKE_ARRAY_SIZE];
                    //numberBuffer2.ta
                    outCount = 0;
                    //sonu
                    for (int c = kacinci + kacTane; c < SHAKE_ARRAY_SIZE; c++)
                    {
                        numberBuffer2[outCount] = NumberArray[c];
                        outCount++;
                    }

                    //ortasi
                    for (int c = kacinci; c < kacinci + kacTane; c++)
                    {
                        numberBuffer2[outCount] = NumberArray[c];
                        outCount++;
                    }

                    //baslangici
                    for (int c = 0; c < kacinci; c++)
                    {
                        numberBuffer2[outCount] = NumberArray[c];
                        outCount++;
                    }

                    NumberArray = numberBuffer2;
                }
            }
            return NumberArray;
        }
        private void InitializeRijndael()
        {
            rijndael.Mode = CipherMode.CBC;
            rijndael.Padding = PaddingMode.PKCS7;
        }

        public AES()
        {
            InitializeRijndael();

            rijndael.KeySize = CHUNK_SIZE;
            rijndael.BlockSize = CHUNK_SIZE;

            rijndael.GenerateKey();
            rijndael.GenerateIV();
        }

        public AES(String base64key, String base64iv)
        {
            InitializeRijndael();

            rijndael.Key = System.Convert.FromBase64String(base64key);
            rijndael.IV = System.Convert.FromBase64String(base64iv);
        }

        public AES(byte[] key, byte[] iv)
        {
            InitializeRijndael();

            rijndael.Key = key;
            rijndael.IV = iv;
        }

        public string Decrypt(byte[] cipher)
        {
            ICryptoTransform transform = rijndael.CreateDecryptor();
            return unicodeEncoding.GetString(transform.TransformFinalBlock(cipher, 0, cipher.Length));
        }
        public byte[] DecryptToByte(byte[] cipher)
        {
            ICryptoTransform transform = rijndael.CreateDecryptor();
            return transform.TransformFinalBlock(cipher, 0, cipher.Length);
        }

        public string DecryptFromBase64String(string base64cipher)
        {
            return Decrypt(System.Convert.FromBase64String(base64cipher));
        }

        public byte[] EncryptToByte(string plain)
        {
            ICryptoTransform encryptor = rijndael.CreateEncryptor();
            byte[] cipher = unicodeEncoding.GetBytes(plain);
            return encryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        }
        public byte[] Encrypt(byte[] plainData)
        {
            ICryptoTransform encryptor = rijndael.CreateEncryptor();
            return encryptor.TransformFinalBlock(plainData, 0, plainData.Length);
        }

        public string EncryptToBase64String(string plain)
        {
            return System.Convert.ToBase64String(EncryptToByte(plain));
        }

        public string GetKey()
        {
            return System.Convert.ToBase64String(rijndael.Key);
        }

        public string GetIV()
        {
            return System.Convert.ToBase64String(rijndael.IV);
        }

        public override string ToString()
        {
            return "KEY:" + GetKey() + Environment.NewLine + "IV:" + GetIV();
        }
    }
}
