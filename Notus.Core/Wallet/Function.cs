using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Notus.Core.Wallet
{
    public static class Function
    {
        public static string[] SeedPhraseList()
        {
            string[] wordList = new string[Notus.Core.Variable.Default_WordListArrayCount];
            for (int i = 0; i < Notus.Core.Variable.Default_WordListArrayCount; i++)
            {
                wordList[i] = Bip39Keyword(
                    int.Parse(
                        new Notus.Hash().CommonHash(
                            "sha1",
                            DateTime.Now.ToLongTimeString() +
                            new Notus.Hash().CommonHash(
                                "md5",
                                DateTime.Now.ToUniversalTime() +
                                new Random().Next(10000000, int.MaxValue - 1).ToString()
                            )
                        ).Substring(0, 4),
                        System.Globalization.NumberStyles.HexNumber
                    )
                );
            }
            return wordList;
        }

        public static string Bip39Keyword(int Bip39WordIndexNo)
        {
            Bip39WordIndexNo = Math.Abs(Bip39WordIndexNo);
            bool exitWhileLoop = false;
            while (exitWhileLoop == false)
            {
                if (Notus.Core.Variable.Bip39WordArray.Length > Bip39WordIndexNo)
                {
                    exitWhileLoop = true;
                }
                else
                {
                    Bip39WordIndexNo = Bip39WordIndexNo - Notus.Core.Variable.Bip39WordArray.Length;
                }
            }
            return Notus.Core.Variable.Bip39WordArray[Bip39WordIndexNo];
        }

        public static bool Bip39KeywordDoesExist(string keyword, int indexNo = -1 )
        {
            if (indexNo != -1)
            {
                return string.Equals(keyword, Bip39Keyword(indexNo));
            }
            else
            {
                if (Array.IndexOf(Notus.Core.Variable.Bip39WordArray, keyword) != -1)
                    return true;
            }

            return false;
        }

        public static string EncodeBase58(BigInteger numberToShorten, int resultSize = 32)
        {
            // WARNING: Beware of bignumber implementations that clip leading 0x00 bytes, or prepend extra 0x00 
            // bytes to indicate sign - your code must handle these cases properly or else you may generate valid-looking
            // addresses which can be sent to, but cannot be spent from - which would lead to the permanent loss of coins.)


            // Base58Check encoding is also used for encoding private keys in the Wallet Import Format. This is formed exactly
            // the same as a Bitcoin address, except that 0x80 is used for the version/application byte, and the payload is 32 bytes
            // instead of 20 (a private key in Bitcoin is a single 32-byte unsigned big-endian integer). Such encodings will always
            // yield a 51-character string that starts with '5', or more specifically, either '5H', '5J', or '5K'.   https://en.bitcoin.it/wiki/Base58Check_encoding
            //const int sizeWalletImportFormat = 51;

            char[] result = new char[resultSize];

            Int32 iAlphabetLength = Notus.Core.Variable.DefaultBase58AlphabetString.Length;
            BigInteger iAlphabetLength2 = BigInteger.Parse(iAlphabetLength.ToString());

            int i = 0;
            while (numberToShorten >= 0 && result.Length > i)
            {
                BigInteger lNumberRemainder = BigInteger.Remainder(numberToShorten, iAlphabetLength2);
                numberToShorten = numberToShorten / iAlphabetLength;
                result[result.Length - 1 - i] = Notus.Core.Variable.DefaultBase58AlphabetString[(int)lNumberRemainder];
                i++;
            }

            return new string(result);
        }

        public static string String_substring(string str, int index, int length)
        {
            if (str.Length > index + length)
            {
                return str.Substring(index, length);
            }
            if (str.Length > index)
            {
                return str.Substring(index);
            }
            return "";
        }

        public static BigInteger Integer_modulo(BigInteger dividend, BigInteger divisor)
        {
            BigInteger remainder = BigInteger.Remainder(dividend, divisor);

            if (remainder < 0)
            {
                return remainder + divisor;
            }

            return remainder;
        }

        public static BigInteger Integer_randomBetween(BigInteger minimum, BigInteger maximum)
        {
            if (maximum < minimum)
            {
                throw new ArgumentException("maximum must be greater than minimum");
            }

            BigInteger range = maximum - minimum;

            Tuple<int, BigInteger> response = Integer_calculateParameters(range);
            int bytesNeeded = response.Item1;
            BigInteger mask = response.Item2;

            byte[] randomBytes = new byte[bytesNeeded];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create())
            {
                random.GetBytes(randomBytes);
            }

            BigInteger randomValue = new BigInteger(randomBytes);

            randomValue &= mask;

            if (randomValue <= range)
            {
                return minimum + randomValue;
            }

            return Integer_randomBetween(minimum, maximum);

        }

        private static Tuple<int, BigInteger> Integer_calculateParameters(BigInteger range)
        {
            int bitsNeeded = 0;
            int bytesNeeded = 0;
            BigInteger mask = new BigInteger(1);

            while (range > 0)
            {
                if (bitsNeeded % 8 == 0)
                {
                    bytesNeeded += 1;
                }

                bitsNeeded++;

                mask = (mask << 1) | 1;

                range >>= 1;
            }

            return Tuple.Create(bytesNeeded, mask);

        }

        public static string File_read(string path)
        {
            return System.IO.File.ReadAllText(path);
        }

        public static byte[] File_readBytes(string path)
        {
            return System.IO.File.ReadAllBytes(path);
        }

        public static byte[] Bytes_sliceByteArray(byte[] bytes, int start)
        {
            int newLength = bytes.Length - start;
            byte[] result = new byte[newLength];
            Array.Copy(bytes, start, result, 0, newLength);
            return result;
        }

        public static byte[] Bytes_sliceByteArray(byte[] bytes, int start, int length)
        {
            int newLength = Math.Min(bytes.Length - start, length);
            byte[] result = new byte[newLength];
            Array.Copy(bytes, start, result, 0, newLength);
            return result;
        }

        public static byte[] Bytes_intToCharBytes(int num)
        {
            return new byte[] { (byte)num };
        }

        public static string BinaryAscii_hexFromBinary(byte[] bytes)
        {
            return Notus.Core.Convert.Byte2Hex(bytes);
        }

        public static byte[] BinaryAscii_binaryFromHex(string hex)
        {
            int numberChars = hex.Length;
            if ((numberChars % 2) == 1)
            {
                hex = "0" + hex;
                numberChars++;
            }
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = System.Convert.ToByte(String_substring(hex, i, 2), 16);
            }
            return bytes;
        }

        public static BigInteger BinaryAscii_numberFromHex(string hex)
        {
            if (((hex.Length % 2) == 1) || hex[0] != '0')
            {
                hex = "0" + hex; // if the hex string doesnt start with 0, the parse will assume its negative
            }
            return BigInteger.Parse(hex, NumberStyles.HexNumber);
        }

        public static string BinaryAscii_hexFromNumber(BigInteger number, int length)
        {
            string hex = number.ToString("X");

            if (hex.Length <= 2 * length)
            {
                hex = (new string('0', 2 * length - hex.Length)) + hex;
            }
            else if (hex[0] == '0')
            {
                hex = hex.Substring(1);
            }
            else
            {
                throw new ArgumentException("number hex length is bigger than 2*length: " + number + ", length=" + length);
            }
            return hex;
        }

        public static byte[] BinaryAscii_stringFromNumber(BigInteger number, int length)
        {
            string hex = BinaryAscii_hexFromNumber(number, length);

            return BinaryAscii_binaryFromHex(hex);
        }

        public static BigInteger BinaryAscii_numberFromString(byte[] bytes)
        {
            string hex = BinaryAscii_hexFromBinary(bytes);
            return BinaryAscii_numberFromHex(hex);
        }

        public static byte[] Base64_decode(string base64String)
        {
            return System.Convert.FromBase64String(base64String);
        }

        public static string Base64_encode(byte[] bytes)
        {
            return System.Convert.ToBase64String(bytes);
        }

        private static readonly int hex31 = 0x1f;
        private static readonly int hex127 = 0x7f;
        private static readonly int hex128 = 0x80;
        private static readonly int hex160 = 0xa0;
        private static readonly int hex224 = 0xe0;

        private static readonly string hexAt = "00";
        private static readonly string hexB = "02";
        private static readonly string hexC = "03";
        private static readonly string hexD = "04";
        private static readonly string hexF = "06";
        private static readonly string hex0 = "30";

        private static readonly byte[] bytesHexAt = BinaryAscii_binaryFromHex(hexAt);
        private static readonly byte[] bytesHexB = BinaryAscii_binaryFromHex(hexB);
        private static readonly byte[] bytesHexC = BinaryAscii_binaryFromHex(hexC);
        private static readonly byte[] bytesHexD = BinaryAscii_binaryFromHex(hexD);
        private static readonly byte[] bytesHexF = BinaryAscii_binaryFromHex(hexF);
        private static readonly byte[] bytesHex0 = BinaryAscii_binaryFromHex(hex0);

        public static byte[] Der_encodeSequence(List<byte[]> encodedPieces)
        {
            int totalLengthLen = 0;
            foreach (byte[] piece in encodedPieces)
            {
                totalLengthLen += piece.Length;
            }
            List<byte[]> sequence = new List<byte[]> { bytesHex0, Der_encodeLength(totalLengthLen) };
            sequence.AddRange(encodedPieces);
            return Der_combineByteArrays(sequence);
        }

        public static byte[] Der_encodeInteger(BigInteger x)
        {
            if (x < 0)
            {
                throw new ArgumentException("x cannot be negative");
            }

            string t = x.ToString("X");

            if (t.Length % 2 == 1)
            {
                t = "0" + t;
            }

            byte[] xBytes = BinaryAscii_binaryFromHex(t);

            int num = xBytes[0];

            if (num <= hex127)
            {
                return Der_combineByteArrays(new List<byte[]> {
                        bytesHexB,
                        Bytes_intToCharBytes(xBytes.Length),
                        xBytes
                    });
            }

            return Der_combineByteArrays(new List<byte[]> {
                    bytesHexB,
                    Bytes_intToCharBytes(xBytes.Length + 1),
                    bytesHexAt,
                    xBytes
                });

        }

        public static byte[] Der_encodeOid(int[] oid)
        {
            int first = oid[0];
            int second = oid[1];

            if (first > 2)
            {
                throw new ArgumentException("first has to be <= 2");
            }

            if (second > 39)
            {
                throw new ArgumentException("second has to be <= 39");
            }

            List<byte[]> bodyList = new List<byte[]> {
                    Bytes_intToCharBytes(40 * first + second)
                };

            for (int i = 2; i < oid.Length; i++)
            {
                bodyList.Add(Der_encodeNumber(oid[i]));
            }

            byte[] body = Der_combineByteArrays(bodyList);

            return Der_combineByteArrays(new List<byte[]> {
                    bytesHexF,
                    Der_encodeLength(body.Length),
                    body
                });

        }

        public static byte[] Der_encodeBitString(byte[] t)
        {
            return Der_combineByteArrays(new List<byte[]> {
                    bytesHexC,
                    Der_encodeLength(t.Length),
                    t
                });
        }

        public static byte[] Der_encodeOctetString(byte[] t)
        {
            return Der_combineByteArrays(new List<byte[]> {
                    bytesHexD,
                    Der_encodeLength(t.Length),
                    t
                });
        }

        public static byte[] Der_encodeConstructed(int tag, byte[] value)
        {
            return Der_combineByteArrays(new List<byte[]> {
                    Bytes_intToCharBytes(hex160 + tag),
                    Der_encodeLength(value.Length),
                    value
                });
        }

        public static Tuple<byte[], byte[]> Der_removeSequence(byte[] bytes)
        {
            Der_checkSequenceError(bytes, hex0, "30");

            Tuple<int, int> readLengthResult = Der_readLength(Bytes_sliceByteArray(bytes, 1));
            int length = readLengthResult.Item1;
            int lengthLen = readLengthResult.Item2;

            int endSeq = 1 + lengthLen + length;

            return new Tuple<byte[], byte[]>(
                Bytes_sliceByteArray(bytes, 1 + lengthLen, length),
                Bytes_sliceByteArray(bytes, endSeq)
            );
        }

        public static Tuple<BigInteger, byte[]> Der_removeInteger(byte[] bytes)
        {
            Der_checkSequenceError(bytes, hexB, "02");

            Tuple<int, int> readLengthResult = Der_readLength(Bytes_sliceByteArray(bytes, 1));
            int length = readLengthResult.Item1;
            int lengthLen = readLengthResult.Item2;

            byte[] numberBytes = Bytes_sliceByteArray(bytes, 1 + lengthLen, length);
            byte[] rest = Bytes_sliceByteArray(bytes, 1 + lengthLen + length);
            int nBytes = numberBytes[0];

            if (nBytes >= hex128)
            {
                throw new ArgumentException("first byte of integer must be < 128");
            }

            return new Tuple<BigInteger, byte[]>(
                BinaryAscii_numberFromHex(BinaryAscii_hexFromBinary(numberBytes)),
                rest
            );

        }

        public static Tuple<int[], byte[]> Der_removeObject(byte[] bytes)
        {
            Der_checkSequenceError(bytes, hexF, "06");

            Tuple<int, int> readLengthResult = Der_readLength(Bytes_sliceByteArray(bytes, 1));
            int length = readLengthResult.Item1;
            int lengthLen = readLengthResult.Item2;

            byte[] body = Bytes_sliceByteArray(bytes, 1 + lengthLen, length);
            byte[] rest = Bytes_sliceByteArray(bytes, 1 + lengthLen + length);

            List<int> numbers = new List<int>();
            Tuple<int, int> readNumberResult;
            while (body.Length > 0)
            {
                readNumberResult = Der_readNumber(body);
                numbers.Add(readNumberResult.Item1);
                body = Bytes_sliceByteArray(body, readNumberResult.Item2);
            }

            int n0 = numbers[0];
            numbers.RemoveAt(0);

            int first = n0 / 40;
            int second = n0 - (40 * first);
            numbers.Insert(0, first);
            numbers.Insert(1, second);

            return new Tuple<int[], byte[]>(
                numbers.ToArray(),
                rest
            );
        }

        public static Tuple<byte[], byte[]> Der_removeBitString(byte[] bytes)
        {
            Der_checkSequenceError(bytes, hexC, "03");

            Tuple<int, int> readLengthResult = Der_readLength(Bytes_sliceByteArray(bytes, 1));
            int length = readLengthResult.Item1;
            int lengthLen = readLengthResult.Item2;

            byte[] body = Bytes_sliceByteArray(bytes, 1 + lengthLen, length);
            byte[] rest = Bytes_sliceByteArray(bytes, 1 + lengthLen + length);

            return new Tuple<byte[], byte[]>(body, rest);
        }

        public static Tuple<byte[], byte[]> Der_removeOctetString(byte[] bytes)
        {
            Der_checkSequenceError(bytes, hexD, "04");

            Tuple<int, int> readLengthResult = Der_readLength(Bytes_sliceByteArray(bytes, 1));
            int length = readLengthResult.Item1;
            int lengthLen = readLengthResult.Item2;

            byte[] body = Bytes_sliceByteArray(bytes, 1 + lengthLen, length);
            byte[] rest = Bytes_sliceByteArray(bytes, 1 + lengthLen + length);

            return new Tuple<byte[], byte[]>(body, rest);
        }

        public static Tuple<int, byte[], byte[]> Der_removeConstructed(byte[] bytes)
        {
            int s0 = Der_extractFirstInt(bytes);

            if ((s0 & hex224) != hex160)
            {
                throw new ArgumentException("wanted constructed tag (0xa0-0xbf), got " + s0);
            }

            int tag = s0 & hex31;

            Tuple<int, int> readLengthResult = Der_readLength(Bytes_sliceByteArray(bytes, 1));
            int length = readLengthResult.Item1;
            int lengthLen = readLengthResult.Item2;

            byte[] body = Bytes_sliceByteArray(bytes, 1 + lengthLen, length);
            byte[] rest = Bytes_sliceByteArray(bytes, 1 + lengthLen + length);

            return new Tuple<int, byte[], byte[]>(tag, body, rest);
        }

        public static byte[] Der_fromPem(string pem)
        {
            string[] split = pem.Split(new string[] { "\n" }, StringSplitOptions.None);
            List<string> stripped = new List<string>();

            for (int i = 0; i < split.Length; i++)
            {
                string line = split[i].Trim();
                if (String_substring(line, 0, 5) != "-----")
                {
                    stripped.Add(line);
                }
            }

            return Base64_decode(string.Join("", stripped));
        }

        public static string Der_toPem(byte[] der, string name)
        {
            string b64 = Base64_encode(der);
            List<string> lines = new List<string> { "-----BEGIN " + name + "-----" };

            int strLength = b64.Length;
            for (int i = 0; i < strLength; i += 64)
            {
                lines.Add(String_substring(b64, i, 64));
            }
            lines.Add("-----END " + name + "-----");

            return string.Join("\n", lines);
        }

        public static byte[] Der_combineByteArrays(List<byte[]> byteArrayList)
        {
            int totalLength = 0;
            foreach (byte[] bytes in byteArrayList)
            {
                totalLength += bytes.Length;
            }

            byte[] combined = new byte[totalLength];
            int consumedLength = 0;

            foreach (byte[] bytes in byteArrayList)
            {
                Array.Copy(bytes, 0, combined, consumedLength, bytes.Length);
                consumedLength += bytes.Length;
            }

            return combined;
        }

        private static byte[] Der_encodeLength(int length)
        {
            if (length < 0)
            {
                throw new ArgumentException("length cannot be negative");
            }

            if (length < hex128)
            {
                return Bytes_intToCharBytes(length);
            }

            string s = length.ToString("X");
            if ((s.Length % 2) == 1)
            {
                s = "0" + s;
            }

            byte[] bytes = BinaryAscii_binaryFromHex(s);
            int lengthLen = bytes.Length;

            return Der_combineByteArrays(new List<byte[]> {
                    Bytes_intToCharBytes(hex128 | lengthLen),
                    bytes
                });
        }

        private static byte[] Der_encodeNumber(int n)
        {
            List<int> b128Digits = new List<int>();

            while (n > 0)
            {
                b128Digits.Insert(0, (n & hex127) | hex128);
                n >>= 7;
            }

            int b128DigitsCount = b128Digits.Count;

            if (b128DigitsCount == 0)
            {
                b128Digits.Add(0);
                b128DigitsCount++;
            }

            b128Digits[b128DigitsCount - 1] &= hex127;

            List<byte[]> byteList = new List<byte[]>();

            foreach (int digit in b128Digits)
            {
                byteList.Add(Bytes_intToCharBytes(digit));
            }

            return Der_combineByteArrays(byteList);
        }

        private static Tuple<int, int> Der_readLength(byte[] bytes)
        {
            int num = Der_extractFirstInt(bytes);

            if ((num & hex128) == 0)
            {
                return new Tuple<int, int>(num & hex127, 1);
            }

            int lengthLen = num & hex127;

            if (lengthLen > bytes.Length - 1)
            {
                throw new ArgumentException("ran out of length bytes");
            }

            return new Tuple<int, int>(
                int.Parse(
                    BinaryAscii_hexFromBinary(Bytes_sliceByteArray(bytes, 1, lengthLen)),
                    System.Globalization.NumberStyles.HexNumber
                ),
                1 + lengthLen
            );
        }

        private static Tuple<int, int> Der_readNumber(byte[] str)
        {
            int number = 0;
            int lengthLen = 0;
            int d;

            while (true)
            {
                if (lengthLen > str.Length)
                {
                    throw new ArgumentException("ran out of length bytes");
                }

                number <<= 7;
                d = str[lengthLen];
                number += (d & hex127);
                lengthLen += 1;
                if ((d & hex128) == 0)
                {
                    break;
                }
            }

            return new Tuple<int, int>(number, lengthLen);
        }

        private static void Der_checkSequenceError(byte[] bytes, string start, string expected)
        {
            if (BinaryAscii_hexFromBinary(bytes).Substring(0, start.Length) != start)
            {
                throw new ArgumentException(
                    "wanted sequence " +
                    expected.Substring(0, 2) +
                    ", got " +
                    Der_extractFirstInt(bytes).ToString("X")
                );
            }
        }

        private static int Der_extractFirstInt(byte[] str)
        {
            return str[0];
        }

    }
}
