using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NGF = Notus.Variable.Globals.Functions;
using NVG = Notus.Variable.Globals;
using NVC = Notus.Variable.Constant;
using NVE = Notus.Variable.Enum;
using NVS = Notus.Variable.Struct;

namespace Notus
{
    public static class Print
    {
        public static void Log(
            NVE.LogLevel logType,
            int logNo,
            string messageText,
            string blockRowNo,
            Notus.Globals.Variable.Settings? objSettings,
            Exception? objException
        )
        {
            NVS.LogStruct logObject = new NVS.LogStruct()
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
        public static void NodeCount()
        {
            Info(NVG.Settings, "Node Count : " + NVG.OnlineNodeCount.ToString() + " / " + NVG.NodeList.Count.ToString());
        }
        public static void ReadLine()
        {
            ReadLine(NVG.Settings);
        }
        public static void ReadLine(Notus.Globals.Variable.Settings NodeSettings)
        {
            Info(NodeSettings, "Press Enter To Continue");
            Console.ReadLine();
        }
        public static void Info(string DetailsStr = "", bool PrintAsync = true)
        {
            Info(NVG.Settings, DetailsStr, PrintAsync);
        }
        public static void Info(Notus.Globals.Variable.Settings NodeSettings, string DetailsStr = "", bool PrintAsync = true)
        {
            subPrint(NodeSettings.Layer, NodeSettings.Network, NodeSettings.InfoMode, ConsoleColor.Cyan, DetailsStr, PrintAsync);
        }
        public static void Danger(string DetailsStr = "", bool PrintAsync = true)
        {
            Danger(NVG.Settings, DetailsStr, PrintAsync);
        }
        public static void Danger(Notus.Globals.Variable.Settings NodeSettings, string DetailsStr = "", bool PrintAsync = true)
        {
            subPrint(NodeSettings.Layer, NodeSettings.Network, NodeSettings.DebugMode, ConsoleColor.Red, DetailsStr, PrintAsync);
        }
        public static void Warning(string DetailsStr = "", bool PrintAsync = true)
        {
            Warning(NVG.Settings, DetailsStr, PrintAsync);
        }
        public static void Warning(Notus.Globals.Variable.Settings NodeSettings, string DetailsStr = "", bool PrintAsync = true)
        {
            subPrint(NodeSettings.Layer, NodeSettings.Network, NodeSettings.InfoMode, ConsoleColor.Yellow, DetailsStr, PrintAsync);
        }
        public static void Status(string DetailsStr = "", bool PrintAsync = true)
        {
            Status(NVG.Settings, DetailsStr, PrintAsync);
        }
        public static void Status(Notus.Globals.Variable.Settings NodeSettings, string DetailsStr = "", bool PrintAsync = true)
        {
            subPrint(NodeSettings.Layer, NodeSettings.Network, NodeSettings.InfoMode, ConsoleColor.White, DetailsStr, PrintAsync);
        }
        public static void Basic(string DetailsStr = "", bool PrintAsync = true)
        {
            Basic(NVG.Settings, DetailsStr, PrintAsync);
        }
        public static void Basic(Notus.Globals.Variable.Settings NodeSettings, string DetailsStr = "", bool PrintAsync = true)
        {
            subPrint(NodeSettings.Layer, NodeSettings.Network, NodeSettings.InfoMode, ConsoleColor.Gray, DetailsStr, PrintAsync);
        }
        public static void Success(string DetailsStr = "", bool PrintAsync = true)
        {
            Success(NVG.Settings, DetailsStr, PrintAsync);
        }
        public static void Success(Notus.Globals.Variable.Settings NodeSettings, string DetailsStr = "", bool PrintAsync = true)
        {
            subPrint(NodeSettings.Layer, NodeSettings.Network, NodeSettings.InfoMode, ConsoleColor.DarkGreen, DetailsStr, PrintAsync);
        }
        public static void Danger(bool ShowOnScreen, string DetailsStr = "", bool PrintAsync = true)
        {
            subPrint(NVE.NetworkLayer.Unknown, NVE.NetworkType.Unknown, ShowOnScreen, ConsoleColor.Red, DetailsStr, PrintAsync);
        }
        public static void Basic(bool ShowOnScreen, string DetailsStr = "", bool PrintAsync = true)
        {
            subPrint(NVE.NetworkLayer.Unknown, NVE.NetworkType.Unknown, ShowOnScreen, ConsoleColor.Gray, DetailsStr, PrintAsync);
        }

        private static void PrintFunction(
            NVE.NetworkLayer tmpLayer,
            NVE.NetworkType tmpType,
            ConsoleColor TextColor,
            string DetailsStr
        )
        {
            DateTime exacTime = DateTime.UtcNow;
            try
            {
                exacTime = NVG.NOW.Obj;
            }
            catch { }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(exacTime.ToString("HH:mm:ss.fff"));
            if (tmpLayer != NVE.NetworkLayer.Unknown && tmpType != NVE.NetworkType.Unknown)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                if (tmpLayer == NVE.NetworkLayer.Layer1)
                    Console.Write(" L1");
                if (tmpLayer == NVE.NetworkLayer.Layer2)
                    Console.Write(" L2");
                if (tmpLayer == NVE.NetworkLayer.Layer3)
                    Console.Write(" L3");
                if (tmpLayer == NVE.NetworkLayer.Layer4)
                    Console.Write(" L4");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("-");
                Console.ForegroundColor = ConsoleColor.Magenta;
                if (tmpType == NVE.NetworkType.DevNet)
                    Console.Write("Dev ");
                if (tmpType == NVE.NetworkType.MainNet)
                    Console.Write("Main");
                if (tmpType == NVE.NetworkType.TestNet)
                    Console.Write("Test");
            }
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" -> ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = TextColor;
            Console.WriteLine(DetailsStr);
        }
        private static void subPrint(
            NVE.NetworkLayer tmpLayer,
            NVE.NetworkType tmpType,
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