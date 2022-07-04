using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Notus.Toolbox
{
    public static class Text
    {
        public static string ShrinkHex(string inputHexData, byte howManyByte)
        {
            if ((2 * howManyByte) >= inputHexData.Length)
            {
                return inputHexData;
            }

            byte[] hexArray = Notus.Convert.Hex2Byte(inputHexData);
            byte xorValue = 0;
            for (int a = howManyByte; a < hexArray.Length; a++)
            {
                xorValue = (byte)(xorValue ^ hexArray[a]);
            }

            byte[] resultArray = new byte[howManyByte];
            for (int x = 0; x < howManyByte; x++)
            {
                resultArray[x] = (byte)(xorValue ^ hexArray[x]);
            }

            return Notus.Convert.Byte2Hex(resultArray);
        }
        public static string HexAlphabetIteration(string KeyForIteration)
        {
            return BaseAlphabetIteration(16, KeyForIteration);
        }
        public static string BaseAlphabetIteration(byte numericBase, string KeyForIteration)
        {
            return Iteration(numericBase, KeyForIteration);
        }
        public static string Iteration(byte numericBase, string KeyForIteration)
        {
            if (numericBase == 16)
            {
                string currentHex = Notus.Variable.Constant.DefaultHexAlphabetString;
                foreach (char pattern in KeyForIteration.ToCharArray())
                {
                    byte startFrom = Notus.Variable.Constant.DefaultHexMapCharDictionary[pattern];
                    if (startFrom < 3)
                    {
                        startFrom = 3;
                    }
                    else
                    {
                        if (startFrom > 10)
                        {
                            startFrom = 10;
                        }
                    }
                    currentHex = currentHex.Substring(startFrom) + ReverseString(currentHex.Substring(0, startFrom));
                }
                return currentHex;
            }

            if (numericBase == 64)
            {
                string baseAlfabe = "ABCDEFGHIJKLMNOPQRSTUVWXYZ" + "abcdefghijklmnopqrstuvwxyz" + "0123456789" + "+/";

                string[] pattern = SplitByLength(KeyForIteration, 2).ToArray();
                for (byte a = 0; a < pattern.Length - 1; a++)
                {
                    char[] harfler = pattern[a].ToCharArray();
                    byte parcaKonum = Notus.Variable.Constant.DefaultHexMapCharDictionary[harfler[0]];
                    byte uzunluk = Notus.Variable.Constant.DefaultHexMapCharDictionary[harfler[1]];
                    baseAlfabe =
                        ReverseString(baseAlfabe.Substring(parcaKonum + uzunluk + 16)) +
                        ReverseString(baseAlfabe.Substring(parcaKonum + uzunluk, 16)) +
                        baseAlfabe.Substring(0, parcaKonum) +
                        ReverseString(baseAlfabe.Substring(parcaKonum, uzunluk));
                }
                return baseAlfabe;
            }

            return string.Empty;
        }
        public static string ReverseString(string inputString)
        {
            char[] charArray = inputString.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
        private static string GenerateEncKey_subFunc(int startingPoint)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                return Notus.Convert.Byte2Hex(
                    md5.ComputeHash(
                        System.Text.Encoding.ASCII.GetBytes(
                            DateTime.Now.AddMilliseconds(startingPoint - 123456).Ticks.ToString("x") + "-" +
                            DateTime.Now.AddSeconds(startingPoint - 123456).ToUniversalTime().ToLongTimeString() + "-" +
                            new Random().Next(startingPoint, startingPoint + 20000000).ToString("x")
                        )
                    )
                );
            }
        }
        public static string GenerateEncKey()
        {
            string newHexPattern = Iteration(16, GenerateEncKey_subFunc(10000000));
            return
                ReplaceChar(GenerateEncKey_subFunc(15000000), Notus.Variable.Constant.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(20000000), Notus.Variable.Constant.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(25000000), Notus.Variable.Constant.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(30000000), Notus.Variable.Constant.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(35000000), Notus.Variable.Constant.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(40000000), Notus.Variable.Constant.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(45000000), Notus.Variable.Constant.DefaultHexAlphabetString, newHexPattern);
        }
        public static string RepeatString(int HowManyTimes, string TextForRepeat)
        {
            string tmpResult = string.Empty;
            for (int i = 0; i < HowManyTimes; i++)
            {
                tmpResult = tmpResult + TextForRepeat;
            }
            return tmpResult;
        }
        public static string GenerateEnryptKey()
        {
            return new Notus.Hash().CommonSign("sasha", GenerateText(29) + GenerateText(14, Notus.Variable.Constant.DefaultHexAlphabetString));
        }
        public static string GenerateSalt()
        {
            string newHexPattern = Iteration(16, GenerateEncKey_subFunc(10000000));
            return
                ReplaceChar(GenerateEncKey_subFunc(15000000), Notus.Variable.Constant.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(20000000), Notus.Variable.Constant.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(25000000), Notus.Variable.Constant.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(30000000), Notus.Variable.Constant.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(35000000), Notus.Variable.Constant.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(40000000), Notus.Variable.Constant.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(45000000), Notus.Variable.Constant.DefaultHexAlphabetString, newHexPattern);
        }
        public static string GenerateText(int outputStringLength)
        {
            string sonucStr = "";
            Random sayi = new Random();
            while (outputStringLength > sonucStr.Length)
            {
                sonucStr += Notus.Variable.Constant.DefaultBase64AlphabetCharArray[sayi.Next(0, 64)];
            }
            return sonucStr.Substring(0, outputStringLength);
        }
        public static string GenerateText(int outputStringLength, string randomTextAlphabet)
        {
            Random rastgele = new Random();
            string uret = "";
            for (int i = 0; i < outputStringLength; i++)
            {
                uret += randomTextAlphabet[rastgele.Next(randomTextAlphabet.Length)];
            }
            return uret;
        }
        public static string AddRightPad(string inputText, int StrLength, string PadString)
        {
            if (inputText.Length >= StrLength)
            {
                return inputText;
            }
            bool cikis = false;
            for (; cikis == false;)
            {
                inputText = inputText + PadString;
                if (inputText.Length >= StrLength)
                {
                    cikis = true;
                }
            }
            return inputText;
        }
        public static string ReplaceChar(string SourceText, string FromText, string ToText)
        {
            char[] input = SourceText.ToCharArray();
            bool[] replaced = new bool[input.Length];

            for (int j = 0; j < input.Length; j++)
                replaced[j] = false;

            for (int i = 0; i < FromText.Length; i++)
            {
                for (int j = 0; j < input.Length; j++)
                    if (replaced[j] == false && input[j] == FromText[i])
                    {
                        input[j] = ToText[i];
                        replaced[j] = true;
                    }
            }
            return new string(input);
        }
        
        public static float[] ProtectionType(Notus.Variable.Enum.ProtectionLevel protectionLevel, bool isLight = false)
        {
            // FontSize, XSpace, YSpace, Opacity
            if (protectionLevel == Notus.Variable.Enum.ProtectionLevel.Low)
                return new float[] { 8, 50, 50, isLight ? 120 : 100 };
            if (protectionLevel == Notus.Variable.Enum.ProtectionLevel.Medium)
                return new float[] { 12, 40, 40, isLight ? 120 : 100 };
            if (protectionLevel == Notus.Variable.Enum.ProtectionLevel.High)
                return new float[] { 14, 40, 40, isLight ? 120 : 100 };

            return new float[] { 0, 0, 0, 0 };
        }
      
        public static IEnumerable<string> SplitByLength(this string str, int maxLength)
        {
            for (int index = 0; index < str.Length; index += maxLength)
            {
                yield return str.Substring(index, Math.Min(maxLength, str.Length - index));
            }
        }
      
        public static string CurrencyName2Hex(string CurrencyNameText)
        {
            return
                System.Convert.ToHexString(
                    System.Text.Encoding.ASCII.GetBytes(
                        CurrencyNameText.ToLower()
                    )
                )
                .ToLower()
                .PadRight(60, 'x');
        }
        public static string Hex2CurrencyName(string CurrencyNameHex)
        {
            return System.Text.Encoding.ASCII.GetString(
                Notus.Convert.Hex2Byte(
                    CurrencyNameHex
                    .ToLower()
                    .Replace('x', ' ')
                    .Trim()
                )
            );
        }
        public static string RawCipherData2String(string Block_Cipher_Data)
        {
            return System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(Block_Cipher_Data));
        }
        public static string BoolToStr(bool tmpBoolVal)
        {
            return (tmpBoolVal == true ? "1" : "0");
        }
    }
}
