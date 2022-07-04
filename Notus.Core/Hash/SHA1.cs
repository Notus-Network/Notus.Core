using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Notus.HashLib
{
    /// <summary>
    /// Helper methods for SHA1 hashing.
    /// </summary>
    public class SHA1
    {
        /// <summary>
        /// Converts the specified plain <see cref="string"/> to SHA1 Hash <see cref="string"/>
        /// </summary>
        /// <param name="inputText">Plain <see cref="string"/> to convert.</param>
        /// <returns>Returns SHA1 Hash <see cref="string"/>.</returns>
        public string ComputeHash(string inputText)
        {
            return Calculate(inputText);
        }

        /// <inheritdoc cref="ComputeHash(string)"/>
        public string Calculate(string inputText)
        {
            return Calculate(Encoding.UTF8.GetBytes(inputText));
        }

        /// <summary>
        /// Converts the specified <see cref="byte"/>[] to SHA1 Hash <see cref="string"/>
        /// </summary>
        /// <param name="input"><see cref="byte"/>[] to convert.</param>
        /// <returns>Returns SHA1 Hash <see cref="string"/>.</returns>
        public string ComputeHash(byte[] input)
        {
            return Calculate(input);
        }

        /// <summary>
        /// Converts the specified Plain <see cref="string"/> to SHA1 Hash <see cref="byte"/>[]
        /// </summary>
        /// <param name="inputText"><see cref="string"/> to convert.</param>
        /// <returns>Returns SHA1 Hash <see cref="byte"/>[].</returns>
        public byte[] Compute(string inputText)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                return sha1.ComputeHash(Encoding.UTF8.GetBytes(inputText));
            }
        }

        /// <summary>
        /// Converts the specified Plain text's <see cref="byte"/>[] to SHA1 Hash <see cref="byte"/>[]
        /// </summary>
        /// <param name="inputData"><see cref="byte"/>[] to convert.</param>
        /// <returns>Returns SHA1 Hash <see cref="byte"/>[].</returns>
        public byte[] Compute(byte[] inputData)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                return sha1.ComputeHash(inputData);
            }
        }
        /// <inheritdoc cref="ComputeHash(byte[])"/>
        public string Calculate(byte[] input)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                return Notus.Convert.Byte2Hex(sha1.ComputeHash(input));
            }
        }

        /// <summary>
        /// Converts the specified <see cref="string"/> to SHA1 Signature <see cref="string"/>
        /// </summary>
        /// <param name="input">Plain <see cref="string"/> to convert.</param>
        /// <returns>Returns SHA1 Signature <see cref="string"/>.</returns>
        public string Sign(string input)
        {
            return SignWithHashMethod("", input);
        }

        /// <summary>
        /// Converts the specified <see cref="byte"/>[] to SHA1 Signature <see cref="string"/>
        /// </summary>
        /// <param name="inputArr"><see cref="byte"/>[] to convert.</param>
        /// <returns>Returns SHA1 Signature <see cref="string"/>.</returns>
        public string Sign(byte[] inputArr)
        {
            return SignWithHashMethod("", Encoding.UTF8.GetString(inputArr));
        }

        /// <summary>
        /// Converts the specified key <see cref="string"/> and specified <see cref="string"/> to SHA1 Signature <see cref="string"/>
        /// </summary>
        /// <param name="keyText"><see cref="string"/> MD5 Key</param>
        /// <param name="input"><see cref="string"/> to convert.</param>
        /// <returns>Returns SHA1 Signature <see cref="string"/>.</returns>
        public string SignWithHashMethod(string keyText, string input)
        {
            int keySize = 80;
            int b = keySize;
            if (keyText.Length > b)
            {
                keyText = Calculate(Encoding.UTF8.GetBytes(keyText)).ToLower();
            }

            byte[] iPadDizi = Encoding.ASCII.GetBytes(
                Notus.Toolbox.Text.AddRightPad("", b, "6")
            );
            byte[] oPadDizi = Encoding.ASCII.GetBytes(
                Notus.Toolbox.Text.AddRightPad("", b, System.Convert.ToChar(92).ToString())
            );
            byte[] keyDizi = Encoding.ASCII.GetBytes(
                Notus.Toolbox.Text.AddRightPad(keyText, b, System.Convert.ToChar(0).ToString())
            );

            string k_ipad = "";
            string k_opad = "";
            for (int a = 0; a < keySize; a++)
            {
                k_ipad = k_ipad + ((char)(keyDizi[a] ^ iPadDizi[a])).ToString();
                k_opad = k_opad + ((char)(keyDizi[a] ^ oPadDizi[a])).ToString();
            }
            return Calculate(Encoding.UTF8.GetBytes(
                k_opad +
                Calculate(Encoding.UTF8.GetBytes(k_ipad + input)).ToLower()
            )).ToLower();
        }
        private byte[] SHA1Algorithm(byte[] input)
        {
            Block512[] blocks = ConvertPaddedTextToBlockArray(PadPlainText(input));
            uint[] H = { 0x67452301, 0xefcdab89, 0x98badcfe, 0x10325476, 0xc3d2e1f0 };
            uint[] K = DefineK();

            for (int i = 0; i < blocks.Length; i++)
            {
                uint[] W = CreateMessageScheduleSha1(blocks[i]);

                uint a = H[0];
                uint b = H[1];
                uint c = H[2];
                uint d = H[3];
                uint e = H[4];

                for (int t = 0; t < 80; t++)
                {
                    uint T = RotL(5, a) + F(t, b, c, d) + e + K[t] + W[t];
                    e = d;
                    d = c;
                    c = RotL(30, b);
                    b = a;
                    a = T;
                }

                H[0] += a;
                H[1] += b;
                H[2] += c;
                H[3] += d;
                H[4] += e;
            }

            return UIntArrayToByteArray(H);
        }

        private static uint[] DefineK()
        {
            uint[] k = new uint[80];

            for (int i = 0; i < 80; i++)
            {
                if (i <= 19)
                {
                    k[i] = 0x5a827999;
                }
                else if (i <= 39)
                {
                    k[i] = 0x6ed9eba1;
                }
                else if (i <= 59)
                {
                    k[i] = 0x8f1bbcdc;
                }
                else
                {
                    k[i] = 0xca62c1d6;
                }
            }

            return k;
        }

        static uint F(int t, uint x, uint y, uint z)
        {
            if (t >= 0 && t <= 19)
            {
                return F1(x, y, z);
            }
            else if (t >= 20 && t <= 39)
            {
                return F3(x, y, z);
            }
            else if (t >= 40 && t <= 59)
            {
                return F2(x, y, z);
            }
            else if (t >= 60 && t <= 79)
            {
                return F3(x, y, z);
            }
            else
            {
                return 0;
            }
        }

        static uint F1(uint x, uint y, uint z)
        {
            return (x & y) ^ (~x & z);
        }

        static uint F2(uint x, uint y, uint z)
        {
            return (x & y) ^ (x & z) ^ (y & z);
        }

        static uint F3(uint x, uint y, uint z)
        {
            return x ^ y ^ z;
        }

        private static byte[] PadPlainText(byte[] plaintext)
        {
            int numberBits = plaintext.Length * 8;
            int t = (numberBits + 8 + 64) / 512;
            int k = 512 * (t + 1) - (numberBits + 8 + 64);
            int n = k / 8;

            List<byte> paddedtext = plaintext.ToList();
            paddedtext.Add(0x80);
            for (int i = 0; i < n; i++)
            {
                paddedtext.Add(0);
            }

            byte[] b = BitConverter.GetBytes((ulong)numberBits);
            Array.Reverse(b);

            for (int i = 0; i < b.Length; i++)
            {
                paddedtext.Add(b[i]);
            }

            return paddedtext.ToArray();
        }

        private static Block512[] ConvertPaddedTextToBlockArray(byte[] paddedtext)
        {
            int numberBlocks = (paddedtext.Length * 8) / 512;
            Block512[] blocks = new Block512[numberBlocks];

            for (int i = 0; i < numberBlocks; i++)
            {
                byte[] b = new byte[64];
                for (int j = 0; j < 64; j++)
                {
                    b[j] = paddedtext[i * 64 + j];
                }

                uint[] words = ByteArrayToUIntArray(b);
                blocks[i] = new Block512(words);
            }

            return blocks;
        }

        private static uint[] ByteArrayToUIntArray(byte[] b)
        {
            int numberBytes = b.Length;
            int n = numberBytes / 4;
            uint[] uintArray = new uint[n];

            for (int i = 0; i < n; i++)
            {
                uintArray[i] = ByteArrayToUInt(b, 4 * i);
            }

            return uintArray;
        }

        private static uint ByteArrayToUInt(byte[] b, int startIndex)
        {
            uint c = 256;
            uint output = 0;

            for (int i = startIndex; i < startIndex + 4; i++)
            {
                output = output * c + b[i];
            }

            return output;
        }

        private static uint[] CreateMessageScheduleSha1(Block512 block)
        {
            uint[] W = new uint[80];
            for (int t = 0; t < 80; t++)
            {
                if (t < 16)
                {
                    W[t] = block.words[t];
                }
                else
                {
                    W[t] = RotL(1, W[t - 3] ^ W[t - 8] ^ W[t - 14] ^ W[t - 16]);
                }
            }

            return W;
        }

        private static uint RotL(int n, uint x)
        {
            return (x << n) | (x >> 32 - n);
        }

        private static byte[] UIntArrayToByteArray(uint[] words)
        {
            List<byte> b = new List<byte>();

            for (int i = 0; i < words.Length; i++)
            {
                b.AddRange(UIntToByteArray(words[i]));
            }

            return b.ToArray();
        }

        private static byte[] UIntToByteArray(uint x)
        {
            byte[] b = BitConverter.GetBytes(x);
            Array.Reverse(b);
            return b;
        }
    }

    public class Block512
    {
        public uint[] words;

        public Block512(uint[] words)
        {
            if (words.Length == 16)
            {
                this.words = words;
            }
            else
            {
                Console.WriteLine("ERROR: A block must be 16 words");
                this.words = null;
            }
        }
    }
}
