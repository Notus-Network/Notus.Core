// Copyright (C) 2020-2022 Notus Network
// 
// Notus Network is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Notus
{
    /// <summary>
    /// A helper class related to convert.
    /// </summary>
    public static class Convert
    {
        /// <summary>
        /// Converts the specified <see cref="byte"/>[] to Base32 <see cref="string"/>
        /// </summary>
        /// <param name="bytes"><see cref="byte"/>[] to convert</param>
        /// <param name="DefinedValidCharList">Selected Base32 sorted letters (optional)</param>
        /// <returns>Returns Base32 <see cref="string"/></returns>
        public static string ToBase32(byte[] bytes, string DefinedValidCharList = "")
        {
            if (DefinedValidCharList.Length != 32)
            {
                DefinedValidCharList = Notus.Variable.Constant.DefaultBase32AlphabetString;
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

        /// <summary>
        /// Converts the specified Base32 <see cref="string"/> to <see cref="byte"/>[]
        /// </summary>
        /// <param name="str">Base32 <see cref="string"/> to convert</param>
        /// <param name="DefinedValidCharList">Your Base32 sorted letters (optional)</param>
        /// <returns>Returns <see cref="byte"/>[]</returns>
        public static byte[] FromBase32(string str, string DefinedValidCharList = "")
        {
            if (DefinedValidCharList.Length != 32)
            {
                DefinedValidCharList = Notus.Variable.Constant.DefaultBase32AlphabetString;
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

        /// <summary>
        /// Converts the specified plain <see cref="string"/> to Base64 <see cref="string"/>
        /// </summary>
        /// <param name="plainText">Plain <see cref="string"/> to convert</param>
        /// <returns>Returns Base64 <see cref="string"/></returns>
        public static string ToBase64(string plainText)
        {
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainText));
        }

        /// <summary>
        /// Converts the specified Base64 <see cref="string"/> to plain <see cref="string"/>
        /// </summary>
        /// <param name="base64EncodedData">Base64 <see cref="string"/> to convert</param>
        /// <returns>Returns plain <see cref="string"/></returns>
        public static string FromBase64(string base64EncodedData)
        {
            return System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(base64EncodedData));
        }

        /// <summary>
        /// Converts the plain <see cref="string"/> to Base64 <see cref="string"/> with selected alphabet
        /// </summary>
        /// <param name="sourceStr">Plain <see cref="string"/> to convert</param>
        /// <param name="newAlphabet">Selected Base64 sorted letters</param>
        /// <returns>Returns Base64 <see cref="string"/></returns>
        public static string ConvertNewAlphabet(string sourceStr, string newAlphabet)
        {
            char[] sourceArray = sourceStr.ToCharArray();
            char[] newAlphabetArray = newAlphabet.ToCharArray();

            for (int a = 0; a < sourceArray.Length; a++)
            {
                bool exitLoop = false;
                for (int b = 0; b < 64 && exitLoop == false; b++)
                {
                    if (sourceArray[a] == Notus.Variable.Constant.DefaultBase64AlphabetCharArray[b])
                    {
                        sourceArray[a] = newAlphabetArray[b];
                        exitLoop = true;
                    }
                }
            }
            return new string(sourceArray);
        }

        /// <summary>
        /// Converts the converted(with selected alphabet) Base64 <see cref="string"/> to Normal Base64 <see cref="string"/>
        /// </summary>
        /// <param name="sourceStr">Conveted Base64 <see cref="string"/> to convert</param>
        /// <param name="oldAlphabet">Selected Base64 sorted letters</param>
        /// <returns>Returns Normal Base64 <see cref="string"/></returns>
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
                        sourceArray[a] = Notus.Variable.Constant.DefaultBase64AlphabetCharArray[b];
                        exitLoop = true;
                    }
                }
            }
            return new string(sourceArray);
        }

        /// <summary>
        /// Converts the specified plain <see cref="string"/> to Base35 <see cref="string"/>
        /// </summary>
        /// <param name="incomeHex">Plain <see cref="string"/> to convert</param>
        /// <returns>Returns Base35 <see cref="string"/></returns>
        public static string ToBase35(string incomeHex)
        {
            string resultStr = "";
            
            string[] strArray = Notus.Toolbox.Text.SplitByLength(incomeHex, 10).ToArray();
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

        /// <summary>
        /// Converts the specified Base35 <see cref="string"/> to plain <see cref="string"/>
        /// </summary>
        /// <param name="incomeBase">Base35 <see cref="string"/> to convert</param>
        /// <returns>Returns plain <see cref="string"/></returns>
        public static string FromBase35(string incomeBase)
        {
            string[] strArray = Notus.Toolbox.Text.SplitByLength(incomeBase, 8).ToArray();
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

        /// <summary>
        /// Converts the specified Base58 <see cref="string"/> to plain <see cref="BigInteger"/>
        /// </summary>
        /// <param name="incomeWalletId">Base58 <see cref="string"/> to convert</param>
        /// <returns>Returns plain <see cref="BigInteger"/></returns>
        public static BigInteger FromBase58(string incomeWalletId)
        {
            // this function fork from
            // https://gist.github.com/micli/c242edd2a81a8f0d9f7953842bcc24f1
            char[] _alphabet = Notus.Variable.Constant.DefaultBase58AlphabetString.ToCharArray();
            int[] _indexes = new int[128];

            for (int i = 0; i < _indexes.Length; i++)
            {
                _indexes[i] = -1;
            }
            for (int i = 0; i < _alphabet.Length; i++)
            {
                _indexes[_alphabet[i]] = i;
            }

            if (0 == incomeWalletId.Length)
            {
                return new BigInteger(new byte[0]);
            }
            byte[] input58 = new byte[incomeWalletId.Length];
            // Transform the String to a base58 byte sequence
            for (int i = 0; i < incomeWalletId.Length; i++)
            {
                char c = incomeWalletId[i];

                int digit58 = -1;
                if (c >= 0 && c < 128)
                {
                    digit58 = _indexes[c];
                }
                if (digit58 < 0)
                {
                    // throw new ArgumentException("Illegal character " + c + " at " + i);
                }
                else
                {
                    input58[i] = (byte)digit58;
                }
            }
            // Count leading zeroes
            int zeroCount = 0;
            while (zeroCount < input58.Length && input58[zeroCount] == 0)
            {
                zeroCount++;
            }
            // The encoding
            byte[] temp = new byte[incomeWalletId.Length];
            int j = temp.Length;

            int startAt = zeroCount;
            while (startAt < input58.Length)
            {
                byte[] innerNumber58 = input58;
                int innerRemainder = 0;
                for (int innerC3 = startAt; innerC3 < innerNumber58.Length; innerC3++)
                {
                    int innerTemp2 = innerRemainder * 58 + ((int)innerNumber58[innerC3] & 0xFF);
                    innerNumber58[innerC3] = (byte)(innerTemp2 / 256);
                    innerRemainder = innerTemp2 % 256;
                }

                if (input58[startAt] == 0)
                {
                    ++startAt;
                }
                temp[--j] = (byte)innerRemainder;
            }
            // Do no add extra leading zeroes, move j to first non null byte.
            while (j < temp.Length && temp[j] == 0)
            {
                j++;
            }

            int innerFrom = j - zeroCount;
            int innerTo = temp.Length;
            byte[] innerRange = new byte[(innerTo - innerFrom) + 1];
            for (int innerC2 = 0; innerC2 < innerTo - innerFrom; innerC2++)
            {
                innerRange[innerC2] = temp[innerFrom + innerC2];
            }
            innerRange[innerTo - innerFrom] = 0;

            return new BigInteger(innerRange);
        }

        /// <summary>
        /// Converts <see cref="byte"/>[] to plain <see cref="string"/>
        /// </summary>
        /// <param name="inputArray"><see cref="byte"/>[] to convert</param>
        /// <returns>Returns plain <see cref="string"/></returns>
        public static string Byte2String(byte[] inputArray)
        {
            return System.Text.Encoding.UTF8.GetString(inputArray);
        }

        /// <summary>
        /// Converts plain <see cref="string"/> to <see cref="byte"/>[]
        /// </summary>
        /// <param name="inputText"><see cref="string"/> to convert</param>
        /// <returns>Returns <see cref="byte"/>[]</returns>
        public static byte[] String2Byte(string inputText)
        {
            return Encoding.UTF8.GetBytes(inputText);
        }

        /// <summary>
        /// Converts <see cref="BigInteger"/> number to hex <see cref="string"/>
        /// </summary>
        /// <param name="BigNumber"><see cref="BigInteger"/> to convert</param>
        /// <returns>Returns hex <see cref="string"/></returns>
        public static string BigInteger2Hex(BigInteger BigNumber)
        {
            return BigNumber.ToString("x");
        }

        /// <summary>
        /// Converts hex <see cref="string"/> to <see cref="BigInteger"/> number
        /// </summary>
        /// <param name="HexStr">Hex <see cref="string"/> to convert</param>
        /// <returns>Returns <see cref="BigInteger"/> number</returns>
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

        /// <summary>
        /// Converts <see cref="byte"/>[] to hex <see cref="string"/>
        /// </summary>
        /// <param name="inputArray"><see cref="byte"/>[] to convert</param>
        /// <returns>Returns hex <see cref="string"/></returns>
        public static string Byte2Hex(byte[] inputArray)
        {
            StringBuilder sb = new StringBuilder(inputArray.Length * 2);
            foreach (byte b in inputArray)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Converts hex <see cref="string"/> to <see cref="byte"/>[]
        /// </summary>
        /// <param name="HexStr">Hex <see cref="string"/> to convert</param>
        /// <returns>Returns <see cref="byte"/>[]</returns>
        public static byte[] Hex2Byte(string HexStr)
        {
            string[] HexArray = Notus.Toolbox.Text.SplitByLength(HexStr, 2).ToArray();
            byte[] YedArray = new byte[HexArray.Length];
            for (int a = 0; a < HexArray.Length; a++)
            {
                YedArray[a] = Byte.Parse(HexArray[a], System.Globalization.NumberStyles.HexNumber);
            }
            return YedArray;
        }

        private static string EncodeBase_subFunc(Int64 incomeIntVal)
        {
            int base_count = Notus.Variable.Constant.DefaultBase35AlphabetString.Length;
            string encoded = "";
            while (incomeIntVal >= base_count)
            {
                Int64 div = incomeIntVal / base_count;
                int mod = (int)(incomeIntVal - (base_count * div));
                encoded = Notus.Variable.Constant.DefaultBase35AlphabetString[mod] + encoded;
                incomeIntVal = div;
            }

            if (incomeIntVal > 0)
            {
                return Notus.Variable.Constant.DefaultBase35AlphabetString[(int)incomeIntVal] + encoded;
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
                decoded += multi * Notus.Variable.Constant.DefaultBase35AlphabetString.IndexOf(incomeText[i]);
                multi *= Notus.Variable.Constant.DefaultBase35AlphabetString.Length;
            }
            return decoded;
        }
    }
}
