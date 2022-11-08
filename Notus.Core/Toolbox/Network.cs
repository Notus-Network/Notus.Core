using System;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Net.Http;
using NVG = Notus.Variable.Globals;
using NGF = Notus.Variable.Globals.Functions;

namespace Notus.Toolbox
{
    public class Network
    {
        private static bool Error_TestIpAddress = true;
        private static readonly string DefaultControlTestData = "notus-network-test-result-data";

        public static Notus.Variable.Class.BlockData? GetBlockFromNode(
            Variable.Struct.IpInfo? ipNode,
            long blockNo, Notus.Globals.Variable.Settings? objSettings = null
        )
        {
            return GetBlockFromNode(ipNode.IpAddress, ipNode.Port, blockNo, objSettings);
        }
        public static bool PingToNode(Notus.Variable.Struct.IpInfo NodeIp)
        {
            return PingToNode(NodeIp.IpAddress, NodeIp.Port);
        }
        public static bool PingToNode(string ipAddress,int portNo)
        {
            return string.Equals(
                Notus.Communication.Request.GetSync(
                    Notus.Network.Node.MakeHttpListenerPath(ipAddress, portNo) + "ping/", 
                    2, true, false
                ), 
                "pong"
            );
        }
        public static string IpAndPortToHex(Notus.Variable.Struct.NodeInfo NodeIp)
        {
            return IpAndPortToHex(NodeIp.IpAddress, NodeIp.Port);
        }
        public static string IpAndPortToHex(Notus.Variable.Struct.IpInfo NodeIp)
        {
            return IpAndPortToHex(NodeIp.IpAddress, NodeIp.Port);
        }
        public static string IpAndPortToHex(string ipAddress, int portNo)
        {
            string resultStr = "";
            foreach (string byteStr in ipAddress.Split("."))
            {
                resultStr += int.Parse(byteStr).ToString("x").PadLeft(2, '0');
            }
            return resultStr.ToLower() + portNo.ToString("x").PadLeft(5, '0').ToLower();
        }

        public static Notus.Variable.Class.BlockData? GetBlockFromNode(
            string ipAddress, int portNo,
            long blockNo, Notus.Globals.Variable.Settings? objSettings = null
        )
        {
            string urlPath = Notus.Network.Node.MakeHttpListenerPath(ipAddress, portNo) + "block/" + blockNo.ToString() + "/raw";
            string incodeResponse = Notus.Communication.Request.GetSync(
                urlPath, 2, true, false, objSettings
            );
            try
            {
                if (incodeResponse != null && incodeResponse != string.Empty && incodeResponse.Length > 0)
                {
                    Notus.Variable.Class.BlockData? tmpResultBlock =
                        JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(incodeResponse);
                    if (tmpResultBlock != null)
                    {
                        return tmpResultBlock;
                    }
                }
            }
            catch (Exception err)
            {
                if (objSettings != null)
                {
                    Notus.Print.Danger(objSettings, err.Message);
                }
            }
            return null;
        }
        public static Notus.Variable.Class.BlockData? GetLastBlock(Notus.Variable.Struct.IpInfo NodeIp, Notus.Globals.Variable.Settings? objSettings = null)
        {
            return GetLastBlock(Notus.Network.Node.MakeHttpListenerPath(NodeIp.IpAddress, NodeIp.Port), objSettings);
        }
        public static Notus.Variable.Class.BlockData? GetLastBlock(string NodeAddress, Notus.Globals.Variable.Settings? objSettings = null)
        {
            try
            {
                string MainResultStr = Notus.Communication.Request.GetSync(
                    NodeAddress + "block/last/raw",
                    10,
                    true,
                    true,
                    objSettings
                );
                Notus.Variable.Class.BlockData? PreBlockData =
                    JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(MainResultStr);
                //Console.WriteLine(JsonSerializer.Serialize(PreBlockData));
                if (PreBlockData != null)
                {
                    return PreBlockData;
                }
            }
            catch (Exception err)
            {
                if (objSettings == null)
                {
                    Console.WriteLine("err : " + err.Message);
                }
                else
                {
                    Notus.Print.Danger(objSettings, "Error Point (GetLastBlock) : " + err.Message);
                }
            }
            return null;
        }

        public static int GetNetworkPort()
        {
            if (NVG.Settings.Network == Variable.Enum.NetworkType.TestNet)
                return NVG.Settings.Port.TestNet;

            if (NVG.Settings.Network == Variable.Enum.NetworkType.DevNet)
                return NVG.Settings.Port.DevNet;

            return NVG.Settings.Port.MainNet;
        }
        public static void IdentifyNodeType(int Timeout = 5)
        {
            NVG.Settings.IpInfo = Notus.Toolbox.Network.GetNodeIP();
            if (NVG.Settings.LocalNode == true)
            {
                Notus.Print.Basic(NVG.Settings, "Starting As Main Node");
                NVG.Settings.NodeType = Notus.Variable.Enum.NetworkNodeType.Main;
                NVG.Settings.Nodes.My.IP.IpAddress = NVG.Settings.IpInfo.Local;
            }
            else
            {
                NVG.Settings.Nodes.My.IP.IpAddress = NVG.Settings.IpInfo.Public;
            }
            NVG.Settings.Nodes.My.HexKey=Notus.Toolbox.Network.IpAndPortToHex(NVG.Settings.Nodes.My.IP.IpAddress, NVG.Settings.Nodes.My.IP.Port);
            if (Notus.Variable.Constant.ListMainNodeIp.IndexOf(NVG.Settings.IpInfo.Public) >= 0)
            {
                //NVG.Settings.Nodes.My.InTheCode = true;
                Notus.Print.Basic(NVG.Settings, "Starting As Main Node");
                if (PublicIpIsConnectable(Timeout))
                {
                    NVG.Settings.NodeType = Notus.Variable.Enum.NetworkNodeType.Main;
                }
                else
                {
                    Notus.Print.Basic(NVG.Settings, "Main Node Port Error");
                }
            }
            else
            {
                //NVG.Settings.Nodes.My.InTheCode = false;
                Notus.Print.Basic(NVG.Settings, "Not Main Node");

                if (PublicIpIsConnectable(Timeout))
                {
                    Notus.Print.Basic(NVG.Settings, "Starting As Master Node");
                    NVG.Settings.NodeType = Notus.Variable.Enum.NetworkNodeType.Master;
                }
                else
                {
                    Notus.Print.Basic(NVG.Settings, "Not Master Node");
                    Notus.Print.Basic(NVG.Settings, "Starting As Replicant Node");
                    NVG.Settings.NodeType = Notus.Variable.Enum.NetworkNodeType.Replicant;
                }
            }
        }

        public static int FindFreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        public static void WaitUntilPortIsAvailable(int PortNo)
        {
            bool PortAvailable = false;
            while (PortAvailable == false)
            {
                PortAvailable = PortIsAvailable(PortNo);
                Thread.Sleep(150);
            }
        }

        public static bool PortIsAvailable(int PortNo)
        {
            bool isAvailable = true;
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
            foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
            {
                if (tcpi.LocalEndPoint.Port == PortNo)
                {
                    isAvailable = false;
                    break;
                }
            }
            return isAvailable;
        }

        public static Notus.Variable.Struct.NodeIpInfo GetNodeIP()
        {
            return new Notus.Variable.Struct.NodeIpInfo()
            {
                Local = GetLocalIPAddress(false),
                Public = GetPublicIPAddress()
            };
        }

        private static string ReadFromNet(string urlPath)
        {
            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage response = client.GetAsync(urlPath).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            catch (Exception err)
            {
            }
            return string.Empty;
        }
        public static string GetPublicIPAddress()
        {
            string address = ReadFromNet("https://api.ipify.org");
            if (address.Length > 0)
            {
                return address;
            }

            address = ReadFromNet("http://checkip.dyndns.org/");
            if (address.Length > 0)
            {
                if (address.Contains("</body>") == true && address.Contains("Address: ") == true)
                {
                    int first = address.IndexOf("Address: ") + 9;
                    return address.Substring(
                        first,
                        address.LastIndexOf("</body>") - first
                    );
                }
            }

            return string.Empty;
        }

        public static string GetLocalIPAddress(bool returnLocalIp)
        {
            bool tmpNetworkAvailable = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            /*
            if (tmpNetworkAvailable == true)
            {
                Console.WriteLine("available");
            }
            else
            {
                Console.WriteLine("un available");
            }
            Console.ReadLine();
            */
            if (returnLocalIp == true)
            {
                return "127.0.0.1";
            }
            try
            {
                string dnsResult = Dns.GetHostName();
                IPHostEntry host = Dns.GetHostEntry(dnsResult);
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception err)
            {
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Info,
                    98798700,
                    err.Message,
                    "BlockRowNo",
                    null,
                    err
                );
            }
            return "127.0.0.1";
        }

        private static bool PublicIpIsConnectable(int Timeout)
        {
            Error_TestIpAddress = false;
            try
            {
                int ControlPortNo = Notus.Toolbox.Network.FindFreeTcpPort();
                using (Notus.Communication.Http tmp_HttpObj = new Notus.Communication.Http())
                {
                    tmp_HttpObj.ResponseType = "text/html";
                    tmp_HttpObj.StoreUrl = false;
                    tmp_HttpObj.Timeout = 5;
                    tmp_HttpObj.DefaultResult_OK = DefaultControlTestData;
                    tmp_HttpObj.DefaultResult_ERR = DefaultControlTestData;
                    tmp_HttpObj.OnReceive(Fnc_TestLinkData);
                    IPAddress testAddress = IPAddress.Parse(NVG.Settings.IpInfo.Public);
                    tmp_HttpObj.Start(testAddress, ControlPortNo);
                    DateTime twoSecondsLater = NVG.NOW.Obj.AddSeconds(Timeout);
                    while (twoSecondsLater > NVG.NOW.Obj && tmp_HttpObj.Started == false)
                    {
                        try
                        {
                            string MainResultStr = Notus.Communication.Request.Get(
                                Notus.Network.Node.MakeHttpListenerPath(NVG.Settings.IpInfo.Public, ControlPortNo) + "block/hash/1",
                                5,
                                true
                            ).GetAwaiter().GetResult();
                        }
                        catch (Exception errInner)
                        {
                            Notus.Print.Log(
                                Notus.Variable.Enum.LogLevel.Info,
                                50000005,
                                errInner.Message,
                                "BlockRowNo",
                                NVG.Settings,
                                errInner
                            );
                            Notus.Print.Basic(NVG.Settings, "Error [75fde6374]: " + errInner.Message);
                        }
                    }
                    if (tmp_HttpObj.Started == false)
                    {
                        Error_TestIpAddress = true;
                    }
                    tmp_HttpObj.Stop();
                }
            }
            catch (Exception err)
            {
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Info,
                    88800880,
                    err.Message,
                    "BlockRowNo",
                    NVG.Settings,
                    err
                );
                Notus.Print.Danger(NVG.Settings, "Error [065]: " + err.Message);
                Error_TestIpAddress = true;
            }
            if (Error_TestIpAddress == true)
            {
                return false;
            }
            return true;
        }
        private static string Fnc_TestLinkData(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            return DefaultControlTestData;
        }

    }
}
