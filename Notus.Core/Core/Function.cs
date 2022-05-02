using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Notus.Core
{
    public static class Function
    {
        public static string ShrinkHex(string inputHexData, byte howManyByte)
        {
            if ((2 * howManyByte) >= inputHexData.Length)
            {
                return inputHexData;
            }

            byte[] hexArray = Notus.Core.Convert.Hex2Byte(inputHexData);
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
            
            return Notus.Core.Convert.Byte2Hex(resultArray);
        }
        public static string BaseAlphabetIteration(byte numericBase, string KeyForIteration)
        {
            return Iteration(numericBase, KeyForIteration);
        }
        public static string Iteration(byte numericBase, string KeyForIteration)
        {
            if (numericBase == 16)
            {
                string currentHex = Notus.Core.Variable.DefaultHexAlphabetString;
                foreach (char pattern in KeyForIteration.ToCharArray())
                {
                    byte startFrom = Notus.Core.Variable.DefaultHexMapCharDictionary[pattern];
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
                    byte parcaKonum = Notus.Core.Variable.DefaultHexMapCharDictionary[harfler[0]];
                    byte uzunluk = Notus.Core.Variable.DefaultHexMapCharDictionary[harfler[1]];
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
                return Notus.Core.Convert.Byte2Hex(
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
                ReplaceChar(GenerateEncKey_subFunc(15000000), Notus.Core.Variable.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(20000000), Notus.Core.Variable.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(25000000), Notus.Core.Variable.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(30000000), Notus.Core.Variable.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(35000000), Notus.Core.Variable.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(40000000), Notus.Core.Variable.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(45000000), Notus.Core.Variable.DefaultHexAlphabetString, newHexPattern);
        }
        public static string RepeatString(int HowManyTimes, string TextForRepeat)
        {
            string tmpResult = string.Empty;
            for(int i = 0; i < HowManyTimes; i++)
            {
                tmpResult = tmpResult + TextForRepeat;
            }
            return tmpResult;
        }
        public static string GenerateEnryptKey()
        {
            return new Notus.Hash().CommonSign("sasha", GenerateText(29) + GenerateText(14, Notus.Core.Variable.DefaultHexAlphabetString));
        }
        public static string GenerateSalt()
        {
            string newHexPattern = Iteration(16, GenerateEncKey_subFunc(10000000));
            return 
                ReplaceChar(GenerateEncKey_subFunc(15000000), Notus.Core.Variable.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(20000000), Notus.Core.Variable.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(25000000), Notus.Core.Variable.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(30000000), Notus.Core.Variable.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(35000000), Notus.Core.Variable.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(40000000), Notus.Core.Variable.DefaultHexAlphabetString, newHexPattern) +
                ReplaceChar(GenerateEncKey_subFunc(45000000), Notus.Core.Variable.DefaultHexAlphabetString, newHexPattern);
        }
        public static string GenerateText(int outputStringLength)
        {
            string sonucStr = "";
            Random sayi = new Random();
            while (outputStringLength > sonucStr.Length)
            {
                sonucStr += Notus.Core.Variable.DefaultBase64AlphabetCharArray[sayi.Next(0, 64)];
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
        public static string DateTimeToString(DateTime DateTimeObj)
        {
            try
            {
                return DateTimeObj.ToString("yyyyMMddHHmmssfff");
            }
            catch
            {
                return "19810125020000000";
            }
        }
        public static DateTime StringToDateTime(string DateTimeStr)
        {
            try
            {
                return DateTime.ParseExact(DateTimeStr.Substring(0, 17), "yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return new DateTime(1981, 01, 25, 2, 00, 00);
            }
        }
        public static void PrintInfo(bool ShowOnScreen, string DetailsStr = "", bool PrintAsync = true)
        {
            if (ShowOnScreen == true)
            {
                if (DetailsStr == "")
                {
                    if (PrintAsync == true)
                    {
                        Task.Run(() =>
                        {
                            Console.WriteLine();
                        });
                    }
                    else
                    {
                        Console.WriteLine();
                    }
                }
                else
                {
                    if (PrintAsync == true)
                    {
                        Task.Run(() =>
                        {
                            ConsoleColor currentColor = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(DateTime.Now.ToLongTimeString() + " -> " + DetailsStr);
                            Console.ForegroundColor = currentColor;
                        });
                    }
                    else
                    {
                        ConsoleColor currentColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(DateTime.Now.ToLongTimeString() + " -> " + DetailsStr);
                        Console.ForegroundColor = currentColor;
                    }
                }
            }

        }
        public static void PrintDanger(bool ShowOnScreen, string DetailsStr = "", bool PrintAsync = true)
        {
            if (ShowOnScreen == true)
            {
                if (DetailsStr == "")
                {
                    if (PrintAsync == true)
                    {
                        Task.Run(() =>
                        {
                            Console.WriteLine();
                        });
                    }
                    else
                    {
                        Console.WriteLine();
                    }
                }
                else
                {
                    if (PrintAsync == true)
                    {
                        Task.Run(() =>
                        {
                            ConsoleColor currentColor = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(DateTime.Now.ToLongTimeString() + " -> " + DetailsStr);
                            Console.ForegroundColor = currentColor;
                        });
                    }
                    else
                    {
                        ConsoleColor currentColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(DateTime.Now.ToLongTimeString() + " -> " + DetailsStr);
                        Console.ForegroundColor = currentColor;
                    }
                }
            }
        }
        public static void PrintWarning(bool ShowOnScreen, string DetailsStr = "", bool PrintAsync = true)
        {
            if (ShowOnScreen == true)
            {
                if (DetailsStr == "")
                {
                    if (PrintAsync == true)
                    {
                        Task.Run(() =>
                        {
                            Console.WriteLine();
                        });
                    }
                    else
                    {
                        Console.WriteLine();
                    }
                }
                else
                {
                    if (PrintAsync == true)
                    {
                        Task.Run(() =>
                        {
                            ConsoleColor currentColor = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine(DateTime.Now.ToLongTimeString() + " -> " + DetailsStr);
                            Console.ForegroundColor = currentColor;
                        });
                    }
                    else
                    {
                        ConsoleColor currentColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(DateTime.Now.ToLongTimeString() + " -> " + DetailsStr);
                        Console.ForegroundColor = currentColor;
                    }
                }
            }
        }
        public static void Print(bool ShowOnScreen, string DetailsStr = "", bool PrintAsync = true)
        {
            if (ShowOnScreen == true)
            {
                if (DetailsStr == "")
                {
                    if (PrintAsync == true)
                    {
                        Task.Run(() =>
                        {
                            Console.WriteLine();
                        });
                    }
                    else
                    {
                        Console.WriteLine();
                    }
                }
                else
                {
                    if (PrintAsync == true)
                    {
                        Task.Run(() =>
                        {
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine(DateTime.Now.ToLongTimeString() + " -> " + DetailsStr);
                        });
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine(DateTime.Now.ToLongTimeString() + " -> " + DetailsStr);
                    }
                }
            }
        }
        public static string NetworkTypeStr(Notus.Core.Variable.NetworkType networkType)
        {
            return (networkType == Notus.Core.Variable.NetworkType.MainNet ? "main_" : "test_");
        }
        public static string NetworkTypeText(Notus.Core.Variable.NetworkType networkType)
        {
            if (networkType == Notus.Core.Variable.NetworkType.MainNet) {
                return "main-net";
            }
            if (networkType == Notus.Core.Variable.NetworkType.TestNet) {
                return "test-net";
            }
            if (networkType == Notus.Core.Variable.NetworkType.DevNet) {
                return "dev-net";
            }
            return "unknown-net";
        }
        public static IEnumerable<string> SplitByLength(this string str, int maxLength)
        {
            for (int index = 0; index < str.Length; index += maxLength)
            {
                yield return str.Substring(index, Math.Min(maxLength, str.Length - index));
            }
        }
        public static int GetNetworkPort(Notus.Core.Variable.NetworkType currentNetwork)
        {
            if (currentNetwork == Variable.NetworkType.TestNet)
            {
                return Notus.Core.Variable.PortNo_TestNet;
            }
            if (currentNetwork == Variable.NetworkType.DevNet)
            {
                return Notus.Core.Variable.PortNo_DevNet;
            }

            return Notus.Core.Variable.PortNo_MainNet;
        }
        public static async Task<string> FindAvailableNode(string UrlText, Notus.Core.Variable.NetworkType currentNetwork)
        {
            string MainResultStr = string.Empty;
            bool exitInnerLoop = false;
            while (exitInnerLoop == false)
            {
                for (int a = 0; a < Notus.Core.Variable.ListMainNodeIp.Count && exitInnerLoop == false; a++)
                {
                    try
                    {
                        MainResultStr = await GetRequest(MakeHttpListenerPath(Notus.Core.Variable.ListMainNodeIp[a], GetNetworkPort(currentNetwork)) + UrlText, 10, true);
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine(err.Message);
                        SleepWithoutBlocking(5, true);
                    }
                    exitInnerLoop = (MainResultStr.Length > 0);
                }
            }
            return MainResultStr;
        }
        public static async Task<string> PostRequest(string UrlAddress, Dictionary<string, string> PostData)
        {
            using (var client = new HttpClient())
            {
                HttpResponseMessage response = await client .PostAsync(UrlAddress, new FormUrlEncodedContent(PostData));
                if (response.IsSuccessStatusCode)
                {
                    HttpContent responseContent = response.Content;
                    return await responseContent .ReadAsStringAsync();
                }
            }
            return string.Empty;
        }
        public static async Task<string> GetRequest(string UrlAddress, int TimeOut = 0, bool UseTimeoutAsSecond = true)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    if (TimeOut > 0)
                    {
                        client.Timeout = (UseTimeoutAsSecond == true ? TimeSpan.FromSeconds(TimeOut * 1000) : TimeSpan.FromMilliseconds(TimeOut));
                    }
                    HttpResponseMessage response = await client.GetAsync(UrlAddress);
                    if (response.IsSuccessStatusCode)
                    {
                        HttpContent responseContent = response.Content;
                        return await responseContent.ReadAsStringAsync();
                    }
                }
            } 
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
            return string.Empty;
        }
        public static void Sleep(int SleepTime, bool UseAsSecond = false)
        {
            SleepWithoutBlocking(SleepTime, UseAsSecond);
        }
        public static void SleepWithoutBlocking(int SleepTime, bool UseAsSecond = false)
        {
            DateTime NextTime = (UseAsSecond == true ? DateTime.Now.AddMilliseconds(SleepTime) : DateTime.Now.AddSeconds(SleepTime));
            while (NextTime > DateTime.Now)
            {

            }
        }
        public static string SubGenerateBlockKey(string SeedForKey = "", string PreText = "")
        {
            DateTime ExactTimeVal = DateTime.Now;
            string tmpTimeHexStr =
                int.Parse(ExactTimeVal.ToString("yyyyMMdd")).ToString("x") +
                int.Parse(ExactTimeVal.ToString("HHmmss")).ToString("x").PadLeft(5, '0') +
                int.Parse(ExactTimeVal.ToString("ffffff")).ToString("x").PadLeft(6, '0');

            if (SeedForKey == "")
            {
                SeedForKey = "#a;s<c>4.t,j8s4j[a]q";
            }

            SeedForKey = SeedForKey + Notus.Core.Variable.CommonDelimeterChar + new Random().Next(1, 42949295).ToString();

            string RandomHashStr1 = new Notus.Hash().CommonHash("ripemd160",
                tmpTimeHexStr +
                Notus.Core.Variable.CommonDelimeterChar +
                SeedForKey +
                Notus.Core.Variable.CommonDelimeterChar +
                tmpTimeHexStr
            );

            string RandomHashStr2 = new Notus.Hash().CommonHash("ripemd160",
                SeedForKey +
                Notus.Core.Variable.CommonDelimeterChar +
                RandomHashStr1 +
                Notus.Core.Variable.CommonDelimeterChar +
                tmpTimeHexStr
            );

            if (PreText == "")
            {
                return tmpTimeHexStr + RandomHashStr1.Substring(0, 36) + RandomHashStr2.Substring(0, 36);
            }

            return tmpTimeHexStr +
                new Notus.Hash().CommonHash("ripemd160", PreText).Substring(0, 10) +
                RandomHashStr1.Substring(0, 31) + RandomHashStr2.Substring(0, 31);
        }
        public static string GenerateBlockKey()
        {
            return GenerateBlockKey(false, "", "");
        }
        public static string GenerateBlockKey(bool ResultAsHex)
        {
            return GenerateBlockKey(ResultAsHex, "", "");
        }
        public static string GenerateBlockKey(bool ResultAsHex = false, string SeedForKey = "", string PreText = "")
        {
            if (ResultAsHex == true)
            {
                return SubGenerateBlockKey(SeedForKey, PreText);
            }
            return Notus.Core.Convert.ToBase35(SubGenerateBlockKey(SeedForKey, PreText));
        }
        public static string GenerateBlockKey(bool ResultAsHex = false, string SeedForKey = "")
        {
            if (ResultAsHex == true)
            {
                return SubGenerateBlockKey(SeedForKey, "");
            }
            return Notus.Core.Convert.ToBase35(SubGenerateBlockKey(SeedForKey, ""));
            //DateTime.Now.ToString("yyyyMMddHHmmssffffff")
        }
        private static int CalculateBlockStorageNumber(string timeKey)
        {
            return int.Parse(timeKey.Substring(8, 6)) % Notus.Core.Variable.BlockStorageMonthlyGroupCount;
        }
        public static string GetBlockStorageFileName(string BlockKey, bool ProcessKeyAsHex = false)
        {
            if (60 > BlockKey.Length)
            {
                return "000000-00";
            }
            else
            {
                string TimeKey = GetTimeFromKey(BlockKey, ProcessKeyAsHex);
                return TimeKey.Substring(0, 6) + "-" + CalculateBlockStorageNumber(TimeKey).ToString().PadLeft(2, '0');
            }
        }
        public static string GetTimeFromKey(string TimeKey, bool ProcessKeyAsHex = false)
        {
            if (TimeKey.Length == 90)
            {
                ProcessKeyAsHex = true;
            }
            if (TimeKey.Length == 72)
            {
                ProcessKeyAsHex = false;
            }

            if (ProcessKeyAsHex == false)
            {
                TimeKey = Notus.Core.Convert.FromBase35(TimeKey);
            }
            string tarihStr = Int64.Parse(TimeKey.Substring(0, 7), System.Globalization.NumberStyles.HexNumber).ToString().PadLeft(8, '0');
            string saatStr = Int64.Parse(TimeKey.Substring(7, 5), System.Globalization.NumberStyles.HexNumber).ToString().PadLeft(6, '0');
            string mikroStr = Int64.Parse(TimeKey.Substring(12, 6), System.Globalization.NumberStyles.HexNumber).ToString().PadLeft(7, '0');
            string timeStr = tarihStr + saatStr + mikroStr;
            if (timeStr.Length > 21)
            {
                return timeStr.Substring(0, 21);
            }
            if (timeStr.Length < 21)
            {
                return timeStr.PadRight(21, '0');
            }
            return timeStr;
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
                Notus.Core.Convert.Hex2Byte(
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
        public static string MakeHttpListenerPath(string IpAddress, int PortNo, bool UseSSL = false)
        {
            return "http" + (UseSSL == true ? "s" : "") + "://" + IpAddress + ":" + PortNo + "/";
        }
    }
}
