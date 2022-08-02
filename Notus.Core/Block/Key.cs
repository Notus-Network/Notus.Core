using System;

namespace Notus.Block
{
    public static class Key
    {
        private static string SubGenerateBlockKey(DateTime ExactTimeVal, string SeedForKey = "", string PreText = "")
        {
            string tmpTimeHexStr =
                int.Parse(ExactTimeVal.ToString("yyyyMMdd")).ToString("x") +
                int.Parse(ExactTimeVal.ToString("HHmmss")).ToString("x").PadLeft(5, '0') +
                int.Parse(ExactTimeVal.ToString("ffffff")).ToString("x").PadLeft(6, '0');

            if (SeedForKey == "")
            {
                SeedForKey = "#a;s<c>4.t,j8s4j[a]q";
            }

            SeedForKey = SeedForKey + Notus.Variable.Constant.CommonDelimeterChar + new Random().Next(1, 42949295).ToString();

            string RandomHashStr1 = new Notus.Hash().CommonHash("ripemd160",
                tmpTimeHexStr +
                Notus.Variable.Constant.CommonDelimeterChar +
                SeedForKey +
                Notus.Variable.Constant.CommonDelimeterChar +
                tmpTimeHexStr
            );

            string RandomHashStr2 = new Notus.Hash().CommonHash("ripemd160",
                SeedForKey +
                Notus.Variable.Constant.CommonDelimeterChar +
                RandomHashStr1 +
                Notus.Variable.Constant.CommonDelimeterChar +
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
        public static string Generate(DateTime currentUtcTime, string nodeWalletKey)
        {
            return SubGenerateBlockKey(currentUtcTime, nodeWalletKey, "");
        }
        /*
        public static string Generate()
        {
            return Notus.Convert.ToBase35(SubGenerateBlockKey(DateTime.Now, "", ""));
        }
        public static string Generate(bool ResultAsHex)
        {
            return Generate(ResultAsHex, "", "");
        }
        public static string Generate(bool ResultAsHex = false, string SeedForKey = "", string PreText = "")
        {
            if (ResultAsHex == true)
            {
                return SubGenerateBlockKey(DateTime.Now, SeedForKey, PreText);
            }
            return Notus.Convert.ToBase35(SubGenerateBlockKey(DateTime.Now, SeedForKey, PreText));
        }

        public static string Generate(bool ResultAsHex = false, string SeedForKey = "")
        {
            if (ResultAsHex == true)
            {
                return SubGenerateBlockKey(DateTime.Now, SeedForKey, "");
            }
            return Notus.Convert.ToBase35(SubGenerateBlockKey(DateTime.Now, SeedForKey, ""));
            //DateTime.Now.ToString("yyyyMMddHHmmssffffff")
        }
        */
        public static int CalculateStorageNumber(string timeKey)
        {
            return int.Parse(timeKey.Substring(8, 6)) % Notus.Variable.Constant.BlockStorageMonthlyGroupCount;
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
                return TimeKey.Substring(0, 6) + "-" + CalculateStorageNumber(TimeKey).ToString().PadLeft(2, '0');
            }
        }
        public static string GetTimeFromKey(string TimeKey, bool ProcessKeyAsHex = false)
        {
            //Console.Write("TimeKey : ");
            //Console.WriteLine(TimeKey);
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
                TimeKey = Notus.Convert.FromBase35(TimeKey);
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

    }
}
