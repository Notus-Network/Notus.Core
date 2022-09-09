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
            Notus.Variable.Enum.NetworkLayer networkLayer,
            bool sslActive = false
        )
        {
            string MainResultStr = string.Empty;
            if (sslActive == false)
            {
                bool exitInnerLoop = false;
                while (exitInnerLoop == false)
                {
                    for (int a = 0; a < Notus.Variable.Constant.ListMainNodeIp.Count && exitInnerLoop == false; a++)
                    {
                        try
                        {
                            MainResultStr = await Notus.Communication.Request.Get(MakeHttpListenerPath(
                                Notus.Variable.Constant.ListMainNodeIp[a],
                                GetNetworkPort(currentNetwork, networkLayer)) + UrlText, 10, true);
                        }
                        catch (Exception err)
                        {
                            Notus.Print.Log(
                                Notus.Variable.Enum.LogLevel.Info,
                                600444004,
                                err.Message,
                                "BlockRowNo",
                                null,
                                err
                            );

                            Console.WriteLine(err.Message);
                            Notus.Date.SleepWithoutBlocking(5, true);
                        }
                        exitInnerLoop = (MainResultStr.Length > 0);
                    }
                }
            }
            else
            {
                try
                {
                    MainResultStr = await Notus.Communication.Request.Get(
                        MakeHttpListenerPath(
                            Notus.Variable.Constant.DefaultNetworkUrl[currentNetwork],
                            0,
                            true
                        ) +
                        UrlText, 10, true);
                }
                catch (Exception err)
                {
                    Notus.Print.Log(
                        Notus.Variable.Enum.LogLevel.Info,
                        77007700,
                        err.Message,
                        "BlockRowNo",
                        null,
                        err
                    );

                    Console.WriteLine(err.Message);
                }
            }
            return MainResultStr;
        }
        public static async Task<string> FindAvailable(
            string UrlText,
            Dictionary<string, string> PostData,
            Notus.Variable.Enum.NetworkType currentNetwork,
            Notus.Variable.Enum.NetworkLayer networkLayer,
            bool sslActive = false
        )
        {
            string MainResultStr = string.Empty;
            if (sslActive == false)
            {
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
                            Notus.Print.Log(
                                Notus.Variable.Enum.LogLevel.Info,
                                9000877,
                                err.Message,
                                "BlockRowNo",
                                null,
                                err
                            );

                            Console.WriteLine(err.Message);
                            Notus.Date.SleepWithoutBlocking(5, true);
                        }
                        exitInnerLoop = (MainResultStr.Length > 0);
                    }
                }
            }
            else
            {
                try
                {
                    MainResultStr = await Notus.Communication.Request.Post(
                        MakeHttpListenerPath(
                            Notus.Variable.Constant.DefaultNetworkUrl[currentNetwork],
                            0, true
                        ) +
                        UrlText, PostData);
                }
                catch (Exception err)
                {
                    Notus.Print.Log(
                        Notus.Variable.Enum.LogLevel.Info,
                        90778400,
                        err.Message,
                        "BlockRowNo",
                        null,
                        err
                    );

                    Console.WriteLine(err.Message);
                }
            }
            return MainResultStr;
        }

        public static string FindAvailableSync(
            string UrlText,
            Notus.Variable.Enum.NetworkType currentNetwork,
            Notus.Variable.Enum.NetworkLayer networkLayer,
            bool showError = true,
            Notus.Variable.Common.ClassSetting objSettings = null
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
                            MakeHttpListenerPath(Notus.Variable.Constant.ListMainNodeIp[a],
                            GetNetworkPort(currentNetwork, networkLayer)) + UrlText,
                            10,
                            true,
                            showError,
                            objSettings
                        );
                    }
                    catch (Exception err)
                    {
                        Notus.Print.Log(
                            Notus.Variable.Enum.LogLevel.Info,
                            77700000,
                            err.Message,
                            "BlockRowNo",
                            objSettings,
                            err
                        );

                        Notus.Print.Danger(objSettings, "Notus.Network.Node.FindAvailableSync -> Line 92 -> " + err.Message);
                        Notus.Date.SleepWithoutBlocking(5, true);
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
            Notus.Variable.Enum.NetworkLayer networkLayer,
            Notus.Variable.Common.ClassSetting objSettings = null
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
                        Notus.Print.Log(
                            Notus.Variable.Enum.LogLevel.Info,
                            80000888,
                            err.Message,
                            "BlockRowNo",
                            objSettings,
                            err
                        );

                        Console.WriteLine(err.Message);
                        Notus.Date.SleepWithoutBlocking(5, true);
                    }
                    exitInnerLoop = (MainResultStr.Length > 0);
                }
            }
            return MainResultStr;
        }

        public static int GetNetworkPort(Notus.Variable.Common.ClassSetting objSetting)
        {
            return GetNetworkPort(objSetting.Network, objSetting.Layer);
        }
        public static int GetNetworkPort(Notus.Variable.Enum.NetworkType currentNetwork, Notus.Variable.Enum.NetworkLayer currentLayer)
        {
            return Notus.Variable.Constant.PortNo[currentLayer][currentNetwork];
        }
        public static string MakeHttpListenerPath(string IpAddress, int PortNo = 0, bool UseSSL = false)
        {
            if (PortNo == 0)
            {
                return "http" + (UseSSL == true ? "s" : "") + "://" + IpAddress + "/";
            }
            return "http" + (UseSSL == true ? "s" : "") + "://" + IpAddress + ":" + PortNo + "/";
        }
    }
}
