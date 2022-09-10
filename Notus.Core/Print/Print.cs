using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Notus
{
    public static class Print
    {
        public static void Log(
            Notus.Variable.Enum.LogLevel logType,
            int logNo,
            string messageText,
            string blockRowNo,
            Notus.Variable.Common.ClassSetting? objSettings,
            Exception? objException
        )
        {
            Notus.Variable.Struct.LogStruct logObject = new Notus.Variable.Struct.LogStruct()
            {
                BlockRowNo = blockRowNo,
                LogNo = logNo,
                LogType = logType,
                Message = messageText,
                WalletKey = "",
                StackTrace = "",
                ExceptionType = ""
            };
            if (objSettings != null)
            {
                if (objSettings.NodeWallet != null)
                {
                    logObject.WalletKey = objSettings.NodeWallet.WalletKey;
                }
            }
            if (objException != null)
            {
                if (objException.StackTrace != null)
                {
                    logObject.StackTrace = objException.StackTrace;
                }
            }

            (bool _, string _) = Notus.Communication.Request.PostSync(
                "http://3.121.218.78:3000/log",
                new Dictionary<string, string>()
                {
                    { "data", JsonSerializer.Serialize(logObject) }
                },
                0,
                true,
                true
            );
        }
        public static void ReadLine(Notus.Variable.Common.ClassSetting NodeSettings)
        {
            Info(NodeSettings, "Press Enter To Continue");
            Console.ReadLine();
        }
        public static void Info(Notus.Variable.Common.ClassSetting NodeSettings, string DetailsStr = "", bool PrintAsync = true)
        {
            subPrint(NodeSettings.Layer, NodeSettings.Network, NodeSettings.InfoMode, ConsoleColor.Cyan, DetailsStr, PrintAsync);
        }
        public static void Danger(Notus.Variable.Common.ClassSetting NodeSettings, string DetailsStr = "", bool PrintAsync = true)
        {
            subPrint(NodeSettings.Layer, NodeSettings.Network, NodeSettings.DebugMode, ConsoleColor.Red, DetailsStr, PrintAsync);
        }
        public static void Warning(Notus.Variable.Common.ClassSetting NodeSettings, string DetailsStr = "", bool PrintAsync = true)
        {
            subPrint(NodeSettings.Layer, NodeSettings.Network, NodeSettings.InfoMode, ConsoleColor.Yellow, DetailsStr, PrintAsync);
        }
        public static void Status(Notus.Variable.Common.ClassSetting NodeSettings, string DetailsStr = "", bool PrintAsync = true)
        {
            subPrint(NodeSettings.Layer, NodeSettings.Network, NodeSettings.InfoMode, ConsoleColor.Blue, DetailsStr, PrintAsync);
        }
        public static void Basic(Notus.Variable.Common.ClassSetting NodeSettings, string DetailsStr = "", bool PrintAsync = true)
        {
            subPrint(NodeSettings.Layer, NodeSettings.Network, NodeSettings.InfoMode, ConsoleColor.Gray, DetailsStr, PrintAsync);
        }
        public static void Success(Notus.Variable.Common.ClassSetting NodeSettings, string DetailsStr = "", bool PrintAsync = true)
        {
            subPrint(NodeSettings.Layer, NodeSettings.Network, NodeSettings.InfoMode, ConsoleColor.DarkGreen, DetailsStr, PrintAsync);
        }
        public static void Danger(bool ShowOnScreen, string DetailsStr = "", bool PrintAsync = true)
        {
            subPrint(Notus.Variable.Enum.NetworkLayer.Unknown, Notus.Variable.Enum.NetworkType.Unknown, ShowOnScreen, ConsoleColor.Red, DetailsStr, PrintAsync);
        }
        public static void Basic(bool ShowOnScreen, string DetailsStr = "", bool PrintAsync = true)
        {
            subPrint(Notus.Variable.Enum.NetworkLayer.Unknown, Notus.Variable.Enum.NetworkType.Unknown, ShowOnScreen, ConsoleColor.Gray, DetailsStr, PrintAsync);
        }

        private static void PrintFunction(
            Notus.Variable.Enum.NetworkLayer tmpLayer,
            Notus.Variable.Enum.NetworkType tmpType,
            ConsoleColor TextColor,
            string DetailsStr
        )
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(DateTime.Now.ToString("HH:mm:ss"));
            if (tmpLayer != Notus.Variable.Enum.NetworkLayer.Unknown && tmpType != Notus.Variable.Enum.NetworkType.Unknown)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                if (tmpLayer == Variable.Enum.NetworkLayer.Layer1)
                    Console.Write(" L1");
                if (tmpLayer == Variable.Enum.NetworkLayer.Layer2)
                    Console.Write(" L2");
                if (tmpLayer == Variable.Enum.NetworkLayer.Layer3)
                    Console.Write(" L3");
                if (tmpLayer == Variable.Enum.NetworkLayer.Layer4)
                    Console.Write(" L4");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("-");
                Console.ForegroundColor = ConsoleColor.Magenta;
                if (tmpType == Notus.Variable.Enum.NetworkType.DevNet)
                    Console.Write("Dev ");
                if (tmpType == Notus.Variable.Enum.NetworkType.MainNet)
                    Console.Write("Main");
                if (tmpType == Notus.Variable.Enum.NetworkType.TestNet)
                    Console.Write("Test");
            }
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" -> ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = TextColor;
            Console.WriteLine(DetailsStr);
        }
        private static void subPrint(
            Notus.Variable.Enum.NetworkLayer tmpLayer,
            Notus.Variable.Enum.NetworkType tmpType,
            bool ShowOnScreen,
            ConsoleColor TextColor,
            string DetailsStr,
            bool PrintAsync
        )
        {
            PrintAsync = false;
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
                            PrintFunction(tmpLayer, tmpType, TextColor, DetailsStr);
                        });
                    }
                    else
                    {
                        PrintFunction(tmpLayer, tmpType, TextColor, DetailsStr);
                    }
                }
            }
        }
    }
}