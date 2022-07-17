using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;

namespace Notus.Communication
{
    public class Http : IDisposable
    {
        private Notus.Variable.Common.ClassSetting Obj_Settings;
        public Notus.Variable.Common.ClassSetting Settings
        {
            get { return Obj_Settings; }
            set { Obj_Settings = value; }
        }

        private int Val_Timeout = 30;
        private IPAddress Val_NodeIPAddress;
        private int Val_PortNo;
        public int Timeout
        {
            set
            {
                Val_Timeout = value;
            }
            get
            {
                return Val_Timeout;
            }
        }

        private Notus.Mempool Mp_UrlList;
        private bool Val_StoreUrl = false;
        public bool StoreUrl
        {
            set
            {
                Val_StoreUrl = value;
            }
            get
            {
                return Val_StoreUrl;
            }
        }
        private string Val_DefaultResult_OK = "OK";
        public string DefaultResult_OK
        {
            set
            {
                Val_DefaultResult_OK = value;
            }
            get
            {
                return Val_DefaultResult_OK;
            }
        }
        private string Val_DefaultResult_ERR = "ERR";
        public string DefaultResult_ERR
        {
            set
            {
                Val_DefaultResult_ERR = value;
            }
            get
            {
                return Val_DefaultResult_ERR;
            }
        }
        private bool DebugModeActivated = false;
        public bool DebugMode
        {
            set
            {
                DebugModeActivated = value;
            }
            get
            {
                return DebugModeActivated;
            }
        }
        private bool InfoModeActivated = false;
        public bool InfoMode
        {
            set
            {
                InfoModeActivated = value;
            }
            get
            {
                return InfoModeActivated;
            }
        }

        private bool Value_ServerStarted = false;
        public bool Started
        {
            get
            {
                return Value_ServerStarted;
            }
        }
        private string Value_ResponseType = "text/html";
        public string ResponseType
        {
            get
            {
                return Value_ResponseType;
            }
            set
            {
                Value_ResponseType = value;
            }
        }
        private Notus.Communication.Listener ListenerObj;

        private bool Value_ServerStoped = false;
        public bool Stoped
        {
            get
            {
                return Value_ServerStoped;
            }
        }
        private bool OnReceiveFunctionDefined = false;
        private System.Func<Notus.Variable.Struct.HttpRequestDetails, string> OnReceiveFunction;
        public void Stop()
        {
            if (ListenerObj != null)
            {
                ListenerObj.Stop();
            }
            Value_ServerStoped = true;
        }

        public void OnReceive(System.Func<Notus.Variable.Struct.HttpRequestDetails, string> OnReceiveFunc)
        {
            OnReceiveFunctionDefined = true;
            OnReceiveFunction = OnReceiveFunc;
        }

        private byte[] IncomeTextFunction(byte[] incomeArray, System.Net.IPEndPoint RemoteEndPoint, System.Net.IPEndPoint LocalEndPoint)
        {

            Console.WriteLine("Notus.Communication.Http.IncomeTextFunction -> Line 145");
            Console.WriteLine(System.Text.Encoding.ASCII.GetString(incomeArray));
            Console.WriteLine("Notus.Communication.Http.IncomeTextFunction -> Line 145");

            Notus.Variable.Struct.HttpRequestDetails incomeData = ParseString(incomeArray);
            if (Val_StoreUrl == true)
            {
                Mp_UrlList.Add(
                    Notus.Toolbox.Date.ToString(DateTime.Now) + new Random().Next(10000000, 42949295).ToString(),
                    JsonSerializer.Serialize(incomeData)
                );
            }

            incomeData.RemoteEP = RemoteEndPoint.ToString();
            incomeData.RemoteIP = RemoteEndPoint.Address.ToString();
            incomeData.RemotePort = RemoteEndPoint.Port;

            incomeData.LocalEP = LocalEndPoint.ToString();
            incomeData.LocalIP = LocalEndPoint.Address.ToString();
            incomeData.LocalPort = LocalEndPoint.Port;

            string ResponseStr = Val_DefaultResult_OK;
            if (OnReceiveFunctionDefined == false)
            {
                Notus.Print.Basic(DebugModeActivated, "Url Doesn't Exist -> " + incomeData.Url);
            }
            else
            {
                Notus.Print.Basic(DebugModeActivated, "Url Call : " + incomeData.RawUrl);
                ResponseStr = OnReceiveFunction(incomeData);
            }
            byte[] headerArray = Encoding.ASCII.GetBytes(
                "HTTP/1.1 200 OK\r\n" +
                "Access-Control-Allow-Origin: *" + "\r\n" +
                "Access-Control-Allow-Credentials: true" + "\r\n" +
                "Connection: close" + "\r\n" +
                "Server-Version: " + "1.0.0.a" + "\r\n" +
                "Server: " + "Notus.Network" + "\r\n" +
                "Content-Type: " + Value_ResponseType + "\r\n" +
                "Accept-Ranges: bytes\r\n" +
                "Content-Length: " + ResponseStr.Length + "\r\n\r\n"
            );
            byte[] bodyArray = Encoding.ASCII.GetBytes(ResponseStr);

            byte[] combined = new byte[headerArray.Length + bodyArray.Length];
            Array.Copy(headerArray, combined, headerArray.Length);
            Array.Copy(bodyArray, 0, combined, headerArray.Length, bodyArray.Length);

            Value_ServerStarted = true;
            return combined;
        }
        public void Start(IPAddress NodeIPAddress, int PortNo)
        {
            if (Val_StoreUrl == true)
            {
                Mp_UrlList = new Notus.Mempool(Notus.Toolbox.IO.GetFolderName(Obj_Settings.Network, Obj_Settings.Layer, Notus.Variable.Constant.StorageFolderName.Common) +
                    "url_visit"
                );
                Mp_UrlList.DebugMode = DebugModeActivated;
                Mp_UrlList.InfoMode = InfoModeActivated;
            }

            Val_NodeIPAddress = NodeIPAddress;
            Val_PortNo = PortNo;

            Value_ServerStarted = false;
            try
            {
                ListenerObj = new Notus.Communication.Listener();
                ListenerObj.KeepAlive = false;
                ListenerObj.DataEndTextIsActive = false;
                ListenerObj.SynchronousSocketIsActive = Obj_Settings.SynchronousSocketIsActive;
                ListenerObj.PortNo = PortNo;
                ListenerObj.IPAddress = NodeIPAddress.ToString();
                ListenerObj.DebugMode = DebugModeActivated;
                ListenerObj.ReturnByteArray = true;
                ListenerObj.Begin(true);
                ListenerObj.OnError((int errorCode, string errorText) =>
                {
                    Notus.Print.Basic(DebugModeActivated, "Error Code : " + errorCode.ToString());
                    Notus.Print.Basic(DebugModeActivated, "Error Text : " + errorText);
                });
                ListenerObj.OnReceive(IncomeTextFunction);
            }
            catch (Exception e)
            {
                Notus.Print.Basic(DebugModeActivated, "An Exception Occurred while Listening :" + e.ToString());
            }
        }

        private Notus.Variable.Struct.HttpRequestDetails ParseString(byte[] rawArray)
        {
            Dictionary<string, string> KeyNameList = new Dictionary<string, string>();
            List<string> queryList = new List<string>();
            string[] splitStr = Encoding.Default.GetString(rawArray).Split('\n');
            for (int i = 0; i < splitStr.Length; i++)
            {
                if (splitStr[i].Trim().Length > 0)
                {
                    queryList.Add(splitStr[i].Trim());
                }
            }
            /*
            int startingPoint = 0;
            for (int a = 1; a < rawArray.Length; a++)
            {
                if ((rawArray[a - 1] == '\r' && rawArray[a] == '\n') || rawArray[a] == 0)
                {
                    char[] tmpCharArray = new char[(a - startingPoint) + 1];
                    Array.Copy(rawArray, startingPoint, tmpCharArray, 0, tmpCharArray.Length);
                    if (tmpCharArray.Length > 1)
                    {
                        string tmpRes = new string(tmpCharArray).Trim();
                        if (tmpRes.Length > 0)
                        {
                            queryList.Add(tmpRes);
                        }
                    }
                    else
                    {

                    }
                    startingPoint = a + 1;
                }
            }
            Console.WriteLine("====================================");
            */
            for (int a = 0; a < queryList.Count; a++)
            {
                if (a > 0)
                {
                    if (queryList[a].Length > 0)
                    {
                        string[] tmpKeyLine = queryList[a].Split(": ");
                        if (tmpKeyLine.Length == 2)
                        {
                            KeyNameList.Add(tmpKeyLine[0].ToLower(), tmpKeyLine[1]);
                        }
                    }
                }
            }
            string requestType = "get";
            string versionNo = "1.1";
            string urlLine = "/";
            if (queryList[0].IndexOf(' ') > 0)
            {
                string[] reqLineArray = queryList[0].Split(" ");
                if (reqLineArray.Length > 2)
                {
                    requestType = reqLineArray[0].ToLower();
                    urlLine = reqLineArray[1];
                    string versionLine = reqLineArray[2];

                    if (reqLineArray[2].IndexOf('/') > 0)
                    {
                        versionNo = versionLine.Split("/")[1];
                    }
                }
            }

            Dictionary<string, string> PostDataList = new Dictionary<string, string>();
            Dictionary<string, string> GetDataList = new Dictionary<string, string>();

            string tmpExactUrl = "";
            string tmpRawUrl = urlLine;
            bool exitLoopVal = false;
            while (exitLoopVal == false)
            {
                if (tmpRawUrl.Length > 0)
                {
                    if (tmpRawUrl[0] == '?')
                    {
                        exitLoopVal = true;
                    }
                    else
                    {
                        tmpExactUrl = tmpExactUrl + tmpRawUrl[0];
                    }
                    tmpRawUrl = tmpRawUrl.Substring(1);
                }
                else
                {
                    exitLoopVal = true;
                }
            }
            if (tmpRawUrl.Length > 0)
            {
                string[] rawParams = tmpRawUrl.Split('&');
                foreach (string param in rawParams)
                {
                    string[] kvPair = param.Split('=');
                    string key = kvPair[0];
                    if (kvPair.Length > 1)
                    {
                        GetDataList.Add(key, HttpUtility.UrlDecode(kvPair[1]));
                    }
                    else
                    {
                        GetDataList.Add(key, "");
                    }
                }
            }

            if (requestType == "post")
            {
                string RawPostDataStr = queryList[queryList.Count - 1];

                if (RawPostDataStr.IndexOf("&") > 0)
                {
                    string[] PostDataArray = RawPostDataStr.Split("&");
                    for (int i = 0; i < PostDataArray.Length; i++)
                    {
                        string[] tmpPostDataArray = PostDataArray[i].Split("=");
                        PostDataList.Add(tmpPostDataArray[0], System.Uri.UnescapeDataString(tmpPostDataArray[1]));
                    }
                }
                else
                {
                    if (RawPostDataStr.IndexOf("=") > 0)
                    {
                        string[] PostDataArray = RawPostDataStr.Split("=");
                        PostDataList.Add(PostDataArray[0], System.Uri.UnescapeDataString(PostDataArray[1]));
                    }
                }
            }

            Notus.Print.Basic(DebugModeActivated, urlLine);

            return new Notus.Variable.Struct.HttpRequestDetails()
            {
                KeepAlive = (KeyNameList.ContainsKey("connection") ? (KeyNameList["connection"] == "keep-alive" ? true : false) : false),
                IsSecureConnection = false,
                IsAuthenticated = false,
                ProtocolVersion = versionNo,
                HttpMethod = requestType.ToUpper(),
                UserAgent = (KeyNameList.ContainsKey("user-agent") ? KeyNameList["user-agent"] : ""),
                UserHostName = (KeyNameList.ContainsKey("host") ? KeyNameList["host"] : ""),

                LocalEP = "",
                LocalIP = "",
                LocalPort = 0,
                RemoteEP = "",
                RemoteIP = "",
                RemotePort = 500,

                RawUrl = urlLine,
                Url = tmpExactUrl,  // buda raw url öncesi
                UrlList = (tmpExactUrl[0] == '/' ? tmpExactUrl.Substring(1) : tmpExactUrl).Split('/'),  // buda raw url öncesi
                PostParams = PostDataList,
                GetParams = GetDataList
            };
        }

        public Http(bool TlsActive = false)
        {
            if (TlsActive == true)
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            }
        }
        ~Http()
        {

        }
        public void Dispose()
        {
            try
            {
                Stop();
            }
            catch { }

            try
            {
                if (Val_StoreUrl == true && Mp_UrlList != null)
                {
                    Mp_UrlList.Dispose();
                }
            }
            catch { }
            try
            {
                if (ListenerObj != null)
                {
                    ListenerObj.Dispose();
                }
            }
            catch { }

        }
    }
}
