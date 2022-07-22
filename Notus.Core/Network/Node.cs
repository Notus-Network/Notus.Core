using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Notus.Network
{
    public static class Node
    {
        public static async Task<string> FindAvailable(
            string UrlText,
            Notus.Variable.Enum.NetworkType currentNetwork,
            Notus.Variable.Enum.NetworkLayer networkLayer
        )
        {
            string MainResultStr = string.Empty;
            bool exitInnerLoop = false;
            while (exitInnerLoop == false)
            {
                for (int a = 0; a < Notus.Variable.Constant.ListMainNodeIp.Count && exitInnerLoop == false; a++)
                {
                    try
                    {
                        MainResultStr = await Notus.Communication.Request.Get(MakeHttpListenerPath(Notus.Variable.Constant.ListMainNodeIp[a],
                            GetNetworkPort(currentNetwork, networkLayer)) + UrlText, 10, true);
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
        public static async Task<string> FindAvailable(
            string UrlText,
            Dictionary<string, string> PostData,
            Notus.Variable.Enum.NetworkType currentNetwork,
            Notus.Variable.Enum.NetworkLayer networkLayer
        )
        {
            string MainResultStr = string.Empty;
            bool exitInnerLoop = false;
            while (exitInnerLoop == false)
            {
                for (int a = 0; a < Notus.Variable.Constant.ListMainNodeIp.Count && exitInnerLoop == false; a++)
                {
                    try
                    {
                        MainResultStr = await Notus.Communication.Request.Post(
                            MakeHttpListenerPath(Notus.Variable.Constant.ListMainNodeIp[a],
                            GetNetworkPort(currentNetwork, networkLayer)) + UrlText,
                            PostData
                        );
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

        public static string FindAvailableSync(
            string UrlText,
            Notus.Variable.Enum.NetworkType currentNetwork,
            Notus.Variable.Enum.NetworkLayer networkLayer,
            bool showError=true
        )
        {
            string MainResultStr = string.Empty;
            bool exitInnerLoop = false;
            while (exitInnerLoop == false)
            {
                for (int a = 0; a < Notus.Variable.Constant.ListMainNodeIp.Count && exitInnerLoop == false; a++)
                {
                    try
                    {
                        MainResultStr = Notus.Communication.Request.GetSync(
                            MakeHttpListenerPath(Notus.Variable.Constant.ListMainNodeIp[a], GetNetworkPort(currentNetwork, networkLayer)) + UrlText, 
                            10, 
                            true,
                            showError
                        );
                    }
                    catch (Exception err)
                    {
                        Notus.Print.Danger(showError, "Notus.Network.Node.FindAvailableSync -> Line 92 -> " + err.Message);
                        SleepWithoutBlocking(5, true);
                    }
                    exitInnerLoop = (MainResultStr.Length > 0);
                }
            }
            return MainResultStr;
        }
        public static string FindAvailableSync(
            string UrlText,
            Dictionary<string, string> PostData,
            Notus.Variable.Enum.NetworkType currentNetwork,
            Notus.Variable.Enum.NetworkLayer networkLayer
        )
        {
            string MainResultStr = string.Empty;
            bool exitInnerLoop = false;
            while (exitInnerLoop == false)
            {
                for (int a = 0; a < Notus.Variable.Constant.ListMainNodeIp.Count && exitInnerLoop == false; a++)
                {
                    try
                    {
                        (bool worksCorrent, string tmpMainResultStr) = Notus.Communication.Request.PostSync(
                            MakeHttpListenerPath(Notus.Variable.Constant.ListMainNodeIp[a],
                            GetNetworkPort(currentNetwork, networkLayer)) + UrlText,
                            PostData
                        );
                        if (worksCorrent == true)
                        {
                            MainResultStr = tmpMainResultStr;
                        }
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

        public static int GetNetworkPort(Notus.Variable.Enum.NetworkType currentNetwork, Notus.Variable.Enum.NetworkLayer currentLayer)
        {
            return Notus.Variable.Constant.PortNo[currentLayer][currentNetwork];
        }
        public static void SleepWithoutBlocking(int SleepTime, bool UseAsSecond = false)
        {
            DateTime NextTime = (UseAsSecond == true ? DateTime.Now.AddMilliseconds(SleepTime) : DateTime.Now.AddSeconds(SleepTime));
            while (NextTime > DateTime.Now)
            {

            }
        }
        public static string MakeHttpListenerPath(string IpAddress, int PortNo, bool UseSSL = false)
        {
            return "http" + (UseSSL == true ? "s" : "") + "://" + IpAddress + ":" + PortNo + "/";
        }
    }
}
