using System;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;

namespace Notus.Toolbox
{
    public class Network
    {
        private static bool Error_TestIpAddress = true;
        private static readonly string DefaultControlTestData = "notus-network-test-result-data";

        public static Notus.Variable.Class.BlockData? GetBlockFromNode(
            Variable.Struct.IpInfo? ipNode,
            long blockNo, Notus.Variable.Common.ClassSetting? objSettings = null
        )
        {
            return GetBlockFromNode(ipNode.IpAddress, ipNode.Port, blockNo, objSettings);
        }
        public static Notus.Variable.Class.BlockData? GetBlockFromNode(
            string ipAddress, int portNo,
            long blockNo, Notus.Variable.Common.ClassSetting? objSettings = null
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
        public static Notus.Variable.Class.BlockData? GetLastBlock(Notus.Variable.Struct.IpInfo NodeIp, Notus.Variable.Common.ClassSetting? objSettings=null)
        {
            return GetLastBlock(Notus.Network.Node.MakeHttpListenerPath(NodeIp.IpAddress, NodeIp.Port), objSettings);
        }
        public static Notus.Variable.Class.BlockData? GetLastBlock(string NodeAddress, Notus.Variable.Common.ClassSetting? objSettings = null)
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
            catch(Exception err)
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

        public static int GetNetworkPort(Notus.Variable.Common.ClassSetting Obj_Settings)
        {
            if (Obj_Settings.Network == Variable.Enum.NetworkType.TestNet)
                return Obj_Settings.Port.TestNet;

            if (Obj_Settings.Network == Variable.Enum.NetworkType.DevNet)
                return Obj_Settings.Port.DevNet;

            return Obj_Settings.Port.MainNet;
        }
        public static Notus.Variable.Common.ClassSetting IdentifyNodeType(Notus.Variable.Common.ClassSetting Obj_Settings, int Timeout = 5)
        {
            Obj_Settings.IpInfo = Notus.Toolbox.Network.GetNodeIP();
            if (Obj_Settings.LocalNode == true)
            {
                Notus.Print.Basic(Obj_Settings, "Starting As Main Node");
                Obj_Settings.NodeType = Notus.Variable.Enum.NetworkNodeType.Main;
                return Obj_Settings;
            }
            Obj_Settings.UTCTime = Notus.Time.GetNtpTime();

            if (Notus.Variable.Constant.ListMainNodeIp.IndexOf(Obj_Settings.IpInfo.Public) >= 0)
            {
                Notus.Print.Basic(Obj_Settings, "Starting As Main Node");
                if (PublicIpIsConnectable(Obj_Settings, Timeout))
                {
                    Obj_Settings.NodeType = Notus.Variable.Enum.NetworkNodeType.Main;
                    return Obj_Settings;
                }
                else
                {
                    Notus.Print.Basic(Obj_Settings, "Main Node Port Error");
                }
            }
            Notus.Print.Basic(Obj_Settings, "Not Main Node");

            if (PublicIpIsConnectable(Obj_Settings, Timeout))
            {
                Notus.Print.Basic(Obj_Settings, "Starting As Master Node");
                Obj_Settings.NodeType = Notus.Variable.Enum.NetworkNodeType.Master;
                return Obj_Settings;
            }
            Notus.Print.Basic(Obj_Settings, "Not Master Node");
            Notus.Print.Basic(Obj_Settings, "Starting As Replicant Node");
            Obj_Settings.NodeType = Notus.Variable.Enum.NetworkNodeType.Replicant;
            return Obj_Settings;
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

        public static string GetPublicIPAddress()
        {
            try
            {
                string address = "";
                WebRequest request = WebRequest.Create("https://api.ipify.org");
                using (WebResponse response = request.GetResponse())
                using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                {
                    address = stream.ReadToEnd();
                }
                return address;
            }
            catch
            {
                try
                {
                    string address = "";
                    WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");
                    using (WebResponse response = request.GetResponse())
                    using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                    {
                        address = stream.ReadToEnd();
                    }

                    int first = address.IndexOf("Address: ") + 9;
                    int last = address.LastIndexOf("</body>");
                    address = address.Substring(first, last - first);
                    return address;
                }
                catch
                {

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
                //Console.WriteLine(JsonSerializer.Serialize(host, new JsonSerializerOptions() { WriteIndented = true }));
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
                //Console.WriteLine(err.Message);
            }
            return "127.0.0.1";
        }

        private static bool PublicIpIsConnectable(Notus.Variable.Common.ClassSetting objSettings, int Timeout)
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
                    tmp_HttpObj.Settings = objSettings;
                    tmp_HttpObj.OnReceive(Fnc_TestLinkData);
                    IPAddress testAddress = IPAddress.Parse(objSettings.IpInfo.Public);
                    tmp_HttpObj.Start(testAddress, ControlPortNo);
                    DateTime twoSecondsLater = DateTime.Now.AddSeconds(Timeout);
                    while (twoSecondsLater > DateTime.Now && tmp_HttpObj.Started == false)
                    {
                        try
                        {
                            string MainResultStr = Notus.Communication.Request.Get(
                                Notus.Network.Node.MakeHttpListenerPath(objSettings.IpInfo.Public, ControlPortNo) + "block/hash/1",
                                5,
                                true
                            ).GetAwaiter().GetResult();
                        }
                        catch (Exception errInner)
                        {
                            Notus.Print.Basic(objSettings, "Error [75fde6374]: " + errInner.Message);
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
                Notus.Print.Danger(objSettings, "Error [065]: " + err.Message);
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
