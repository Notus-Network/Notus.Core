using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
namespace Notus.Core
{
    public static class Convert
    {
        public static string ToBase32(byte[] bytes, string DefinedValidCharList = "")
        {
            if (DefinedValidCharList.Length != 32)
            {
                DefinedValidCharList = Notus.Core.Variable.DefaultBase32AlphabetString;
            }
            StringBuilder sb = new StringBuilder();
            byte index;
            int hi = 5;
            int currentByte = 0;

            while (currentByte < bytes.Length)
            {
                if (hi > 8)
                {
                    index = (byte)(bytes[currentByte++] >> (hi - 5));
                    if (currentByte != bytes.Length)
                    {
                        index = (byte)(((byte)(bytes[currentByte] << (16 - hi)) >> 3) | index);
                    }

                    hi -= 3;
                }
                else if (hi == 8)
                {
                    index = (byte)(bytes[currentByte++] >> 3);
                    hi -= 3;
                }
                else
                {
                    index = (byte)((byte)(bytes[currentByte] << (8 - hi)) >> 3);
                    hi += 5;
                }

                sb.Append(DefinedValidCharList[index]);
            }

            return sb.ToString();
        }
        public static byte[] FromBase32(string str, string DefinedValidCharList = "")
        {
            if (DefinedValidCharList.Length != 32)
            {
                DefinedValidCharList = Notus.Core.Variable.DefaultBase32AlphabetString;
            }

            int numBytes = str.Length * 5 / 8;
            byte[] bytes = new Byte[numBytes];

            str = str.ToUpper();
            int bit_buffer;
            int currentCharIndex;
            int bits_in_buffer;

            if (str.Length < 3)
            {
                bytes[0] = (byte)(DefinedValidCharList.IndexOf(str[0]) | DefinedValidCharList.IndexOf(str[1]) << 5);
                return bytes;
            }

            bit_buffer = (DefinedValidCharList.IndexOf(str[0]) | DefinedValidCharList.IndexOf(str[1]) << 5);
            bits_in_buffer = 10;
            currentCharIndex = 2;
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)bit_buffer;
                bit_buffer >>= 8;
                bits_in_buffer -= 8;
                while (bits_in_buffer < 8 && currentCharIndex < str.Length)
                {
                    bit_buffer |= DefinedValidCharList.IndexOf(str[currentCharIndex++]) << bits_in_buffer;
                    bits_in_buffer += 5;
                }
            }

            return bytes;
        }

        public static string ToBase64(string plainText)
        {
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainText));
        }
        public static string FromBase64(string base64EncodedData)
        {
            return System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(base64EncodedData));
        }


        public static string ConvertNewAlphabet(string sourceStr, string newAlphabet)
        {
            char[] sourceArray = sourceStr.ToCharArray();
            char[] newAlphabetArray = newAlphabet.ToCharArray();

            for (int a = 0; a < sourceArray.Length; a++)
            {
                bool exitLoop = false;
                for (int b = 0; b < 64 && exitLoop == false; b++)
                {
                    if (sourceArray[a] == Notus.Core.Variable.DefaultBase64AlphabetCharArray[b])
                    {
                        sourceArray[a] = newAlphabetArray[b];
                        exitLoop = true;
                    }
                }
            }
            return new string(sourceArray);
        }
        public static string ConvertOriginalAlphabet(string sourceStr, string oldAlphabet)
        {
            char[] sourceArray = sourceStr.ToCharArray();
            char[] newAlphabetArray = oldAlphabet.ToCharArray();

            for (int a = 0; a < sourceArray.Length; a++)
            {
                bool exitLoop = false;
                for (int b = 0; b < 64 && exitLoop == false; b++)
                {
                    if (sourceArray[a] == newAlphabetArray[b])
                    {
                        sourceArray[a] = Notus.Core.Variable.DefaultBase64AlphabetCharArray[b];
                        exitLoop = true;
                    }
                }
            }
            return new string(sourceArray);
        }


        public static string ToBase35(string incomeHex)
        {
            string resultStr = "";
            
            string[] strArray = Notus.Core.Function.SplitByLength(incomeHex, 10).ToArray();
            for (int a = 0; a < strArray.Length; a++)
            {
                string incomeStr = EncodeBase_subFunc(
                    System.Convert.ToInt64(
                        strArray[a].ToUpper(),
                        16
                    )
                );
                while (incomeStr.Length < 8)
                {
                    incomeStr = "0" + incomeStr;
                }
                resultStr += incomeStr;
            }
            return resultStr;
        }
        public static string FromBase35(string incomeBase)
        {
            string[] strArray = Notus.Core.Function.SplitByLength(incomeBase, 8).ToArray();
            string hexStr = "";
            for (int a = 0; a < strArray.Length; a++)
            {
                Int64 resultIntVal = Decode_subRoutine(strArray[a]);
                string convertedHexStr = resultIntVal.ToString("x");
                if (strArray.Length - 1 > a)
                {
                    while (convertedHexStr.Length < 10)
                    {
                        convertedHexStr = "0" + convertedHexStr;
                    }
                }
                hexStr += convertedHexStr;
            }
            return hexStr;
        }

        private static string EncodeBase_subFunc(Int64 incomeIntVal)
        {
            int base_count = Notus.Core.Variable.DefaultBase35AlphabetString.Length;
            string encoded = "";
            while (incomeIntVal >= base_count)
            {
                Int64 div = incomeIntVal / base_count;
                int mod = (int)(incomeIntVal - (base_count * div));
                encoded = Notus.Core.Variable.DefaultBase35AlphabetString[mod] + encoded;
                incomeIntVal = div;
            }

            if (incomeIntVal > 0)
            {
                return Notus.Core.Variable.DefaultBase35AlphabetString[(int)incomeIntVal] + encoded;
            }
            return encoded;
        }
        private static Int64 Decode_subRoutine(string incomeText)
        {
            incomeText = incomeText.Replace("0", "");
            Int64 decoded = 0;
            Int64 multi = 1;
            for (int i = incomeText.Length - 1; i >= 0; i--)
            {
                decoded += multi * Notus.Core.Variable.DefaultBase35AlphabetString.IndexOf(incomeText[i]);
                multi *= Notus.Core.Variable.DefaultBase35AlphabetString.Length;
            }
            return decoded;
        }
        public static string Byte2String(byte[] inputArray)
        {
            return System.Text.Encoding.UTF8.GetString(inputArray);
        }
        public static byte[] String2Byte(string inputText)
        {
            return Encoding.UTF8.GetBytes(inputText);
        }


        public static string BigInteger2Hex(BigInteger BigNumber)
        {
            return BigNumber.ToString("x");
        }
        public static BigInteger Hex2BigInteger(string HexStr)
        {
            if (((HexStr.Length % 2) == 1) || HexStr[0] != '0')
            {
                HexStr = "0" + HexStr; // if the hex string doesnt start with 0, the parse will assume its negative
            }
            return BigInteger.Parse(
                HexStr,
                NumberStyles.HexNumber
            );
        }
        public static string Byte2Hex(byte[] inputArray)
        {
            StringBuilder sb = new StringBuilder(inputArray.Length * 2);
            foreach (byte b in inputArray)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
        public static byte[] Hex2Byte(string HexStr)
        {
            //SplitByLength(HexStr, 2);
            string[] HexArray = Notus.Core.Function.SplitByLength(HexStr, 2).ToArray();
            byte[] YedArray = new byte[HexArray.Length];
            for (int a = 0; a < HexArray.Length; a++)
            {
                YedArray[a] = Byte.Parse(HexArray[a], System.Globalization.NumberStyles.HexNumber);
            }
            return YedArray;
        }

    }
}
