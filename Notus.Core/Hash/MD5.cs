using System;
using System.Linq;
using System.Text;

namespace Notus.HashLib
{
    /// <summary>
    /// Helper methods for MD5 hashing.
    /// </summary>
    public class MD5
    {
        int[] s = new int[64] {
            7, 12, 17, 22,  7, 12, 17, 22,  7, 12, 17, 22,  7, 12, 17, 22,
            5,  9, 14, 20,  5,  9, 14, 20,  5,  9, 14, 20,  5,  9, 14, 20,
            4, 11, 16, 23,  4, 11, 16, 23,  4, 11, 16, 23,  4, 11, 16, 23,
            6, 10, 15, 21,  6, 10, 15, 21,  6, 10, 15, 21,  6, 10, 15, 21
        };

        uint[] K = new uint[64] {
            0xd76aa478, 0xe8c7b756, 0x242070db, 0xc1bdceee,
            0xf57c0faf, 0x4787c62a, 0xa8304613, 0xfd469501,
            0x698098d8, 0x8b44f7af, 0xffff5bb1, 0x895cd7be,
            0x6b901122, 0xfd987193, 0xa679438e, 0x49b40821,
            0xf61e2562, 0xc040b340, 0x265e5a51, 0xe9b6c7aa,
            0xd62f105d, 0x02441453, 0xd8a1e681, 0xe7d3fbc8,
            0x21e1cde6, 0xc33707d6, 0xf4d50d87, 0x455a14ed,
            0xa9e3e905, 0xfcefa3f8, 0x676f02d9, 0x8d2a4c8a,
            0xfffa3942, 0x8771f681, 0x6d9d6122, 0xfde5380c,
            0xa4beea44, 0x4bdecfa9, 0xf6bb4b60, 0xbebfbc70,
            0x289b7ec6, 0xeaa127fa, 0xd4ef3085, 0x04881d05,
            0xd9d4d039, 0xe6db99e5, 0x1fa27cf8, 0xc4ac5665,
            0xf4292244, 0x432aff97, 0xab9423a7, 0xfc93a039,
            0x655b59c3, 0x8f0ccc92, 0xffeff47d, 0x85845dd1,
            0x6fa87e4f, 0xfe2ce6e0, 0xa3014314, 0x4e0811a1,
            0xf7537e82, 0xbd3af235, 0x2ad7d2bb, 0xeb86d391
        };

        public uint leftRotate(uint x, int c)
        {
            return (x << c) | (x >> (32 - c));
        }

        /// <summary>
        /// Converts the specified plain <see cref="string"/> to MD5 Hash <see cref="string"/>
        /// </summary>
        /// <param name="inputText">Plain <see cref="string"/> to convert.</param>
        /// <returns>Returns MD5 Hash <see cref="string"/>.</returns>
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
        /// Converts the specified <see cref="byte"/>[] to MD5 Hash <see cref="string"/>
        /// </summary>
        /// <param name="input"><see cref="byte"/>[] to convert.</param>
        /// <returns>Returns MD5 Hash <see cref="string"/>.</returns>
        public string ComputeHash(byte[] input)
        {
            return Calculate(input);
        }

        /// <inheritdoc cref="ComputeHash(byte[])"/>
        public string Calculate(byte[] input)
        {
            byte[] hashBytes = System.Security.Cryptography.MD5.Create().ComputeHash(input);

            // Step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }
            return sb.ToString();

            uint a0 = 0x67452301;
            uint b0 = 0xefcdab89;
            uint c0 = 0x98badcfe;
            uint d0 = 0x10325476;

            var addLength = (56 - ((input.Length + 1) % 64)) % 64;
            var processedInput = new byte[input.Length + 1 + addLength + 8];
            Array.Copy(input, processedInput, input.Length);
            processedInput[input.Length] = 0x80;

            byte[] length = BitConverter.GetBytes(input.Length * 8);
            Array.Copy(length, 0, processedInput, processedInput.Length - 8, 4);

            for (int i = 0; i < processedInput.Length / 64; ++i)
            {
                uint[] M = new uint[16];
                for (int j = 0; j < 16; ++j)
                    M[j] = BitConverter.ToUInt32(processedInput, (i * 64) + (j * 4));
                uint A = a0, B = b0, C = c0, D = d0, F = 0, g = 0;
                for (uint k = 0; k < 64; ++k)
                {
                    if (k <= 15)
                    {
                        F = (B & C) | (~B & D);
                        g = k;
                    }
                    else if (k >= 16 && k <= 31)
                    {
                        F = (D & B) | (~D & C);
                        g = ((5 * k) + 1) % 16;
                    }
                    else if (k >= 32 && k <= 47)
                    {
                        F = B ^ C ^ D;
                        g = ((3 * k) + 5) % 16;
                    }
                    else if (k >= 48)
                    {
                        F = C ^ (B | ~D);
                        g = (7 * k) % 16;
                    }

                    uint dtemp = D;
                    D = C;
                    C = B;
                    B = B + leftRotate((A + F + K[k] + M[g]), s[k]);
                    A = dtemp;
                }

                a0 += A;
                b0 += B;
                c0 += C;
                d0 += D;
            }

            return GetByteString(a0) + GetByteString(b0) + GetByteString(c0) + GetByteString(d0);
        }

        /// <summary>
        /// Converts the specified <see cref="string"/> to MD5 Signature <see cref="string"/>
        /// </summary>
        /// <param name="input">Plain <see cref="string"/> to convert.</param>
        /// <returns>Returns MD5 Signature <see cref="string"/>.</returns>
        public string Sign(string input)
        {
            return SignWithHashMethod("", input);
        }

        /// <summary>
        /// Converts the specified <see cref="byte"/>[] to MD5 Signature <see cref="string"/>
        /// </summary>
        /// <param name="inputArr"><see cref="byte"/>[] to convert.</param>
        /// <returns>Returns MD5 Signature <see cref="string"/>.</returns>
        public string Sign(byte[] inputArr)
        {
            return SignWithHashMethod("", Encoding.UTF8.GetString(inputArr));
        }

        /// <summary>
        /// Converts the specified key <see cref="string"/> and specified <see cref="string"/> to MD5 Signature <see cref="string"/>
        /// </summary>
        /// <param name="keyText"><see cref="string"/> MD5 Key</param>
        /// <param name="input"><see cref="string"/> to convert.</param>
        /// <returns>Returns MD5 Signature <see cref="string"/>.</returns>
        public string SignWithHashMethod(string keyText, string input)
        {
            int keySize = 64;
            int b = keySize;
            if (keyText.Length > b)
            {
                keyText = Calculate(Encoding.UTF8.GetBytes(keyText)).ToLower();
            }

            byte[] iPadDizi = Encoding.UTF8.GetBytes(
                Notus.Toolbox.Text.AddRightPad("", b, "6")
            );
            byte[] oPadDizi = Encoding.UTF8.GetBytes(
                Notus.Toolbox.Text.AddRightPad("", b, System.Convert.ToChar(92).ToString())
            );
            byte[] keyDizi = Encoding.UTF8.GetBytes(
                Notus.Toolbox.Text.AddRightPad(keyText, b, System.Convert.ToChar(0).ToString())
            );

            string k_ipad = "";
            string k_opad = "";
            for (int a = 0; a < keySize; a++)
            {
                k_ipad = k_ipad + ((char)(keyDizi[a] ^ iPadDizi[a])).ToString();
                k_opad = k_opad + ((char)(keyDizi[a] ^ oPadDizi[a])).ToString();
            }

            return Calculate(
                Encoding.UTF8.GetBytes(
                    k_opad + Calculate(
                        Encoding.UTF8.GetBytes(
                            k_ipad +
                            input
                        )
                    )
                )
            ).ToLower();
        }

        private string GetByteString(uint x)
        {
            return String.Join("", BitConverter.GetBytes(x).Select(y => y.ToString("x2")));
        }
    }
}