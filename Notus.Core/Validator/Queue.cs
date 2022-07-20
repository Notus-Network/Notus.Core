using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using Notus.Variable.Struct;
namespace Notus.Validator
{
    public class Queue : IDisposable
    {
        private Notus.Variable.Common.ClassSetting Obj_Settings;
        public Notus.Variable.Common.ClassSetting Settings
        {
            get { return Obj_Settings; }
            set { Obj_Settings = value; }
        }
        private bool Val_Ready;
        public bool Ready
        {
            get { return Val_Ready; }
            set { Val_Ready = value; }
        }
        private bool MyTurn = false;
        private bool SyncReady = true;

        private SortedDictionary<string, IpInfo> MainAddressList = new SortedDictionary<string, IpInfo>();
        private string MainAddressListHash = string.Empty;

        private List<IpInfo> InTheCodeNodeList = new List<IpInfo>();
        private Dictionary<string, NodeQueueInfo> PreviousNodeList = new Dictionary<string, NodeQueueInfo>();
        public Dictionary<string, NodeQueueInfo> SyncNodeList
        {
            get { return PreviousNodeList; }
            set { PreviousNodeList = value; }
        }
        private Dictionary<string, NodeQueueInfo> NodeList = new Dictionary<string, NodeQueueInfo>();
        private Dictionary<string, DateTime> MessageTimeList = new Dictionary<string, DateTime>();
        private Dictionary<int, string> NodeOrderList = new Dictionary<int, string>();

        private readonly DateTime DefaultTime = new DateTime(2000, 01, 1, 0, 00, 00);
        private Notus.Mempool ObjMp_NodeList;
        private bool ExitFromLoop = false;
        private string LastHashForStoreList = "#####";
        private string NodeListHash = "#";

        private int MyPortNo = 6500;
        private string MyNodeHexKey = "#";
        private string MyWallet = "#";
        private string MyIpAddress = "#";

        private DateTime LastPingTime;
        private DateTime NextCheckTime = DateTime.Now;

        private DateTime NtpTime;                       // ntp server time
        private DateTime NtpCheckTime;                  // last check ntp time
        private TimeSpan NtpTimeDifference;             // time difference between NTP server and current node
        private bool NodeTimeAfterNtpTime = false;      // time difference before or after NTP Server
        private DateTime NextQueueValidNtpTime;         // New Queue will be usable after this NTP time

        //empty blok için kontrolü yapacak olan node'u seçen fonksiyon
        public Notus.Variable.Enum.ValidatorOrder EmptyTimer()
        {
            return Notus.Variable.Enum.ValidatorOrder.Primary;
        }

        //oluşturulacak blokları kimin oluşturacağını seçen fonksiyon
        public Notus.Variable.Enum.ValidatorOrder Distrubute(Notus.Variable.Class.BlockData BlockData)
        {
            return Notus.Variable.Enum.ValidatorOrder.Primary;
        }

        private void RefreshNtpTime()
        {
            DateTime tmpNtpTime = NtpTime;
            const ulong secondPointConst = 1000;
            const ulong midPointConst = 3;

            DateTime afterMiliSecondTime = tmpNtpTime.AddMilliseconds(
                secondPointConst + (secondPointConst - (Notus.Date.ToLong(NtpTime) % secondPointConst))
            );
            double secondVal = midPointConst + (midPointConst - (ulong.Parse(afterMiliSecondTime.ToString("ss")) % midPointConst));
            NextQueueValidNtpTime = afterMiliSecondTime.AddSeconds(secondVal);
        }

        private void CalculateTimeDifference(bool useLocalValue)
        {
            if ((DateTime.Now - NtpCheckTime).TotalMinutes > 10 && useLocalValue == false)
            {
                NtpTime = Notus.Toolbox.Network.GetExactTime_DateTime();
                NtpCheckTime = DateTime.Now;
                NodeTimeAfterNtpTime = (NtpCheckTime > NtpTime);
                if (NodeTimeAfterNtpTime == true)
                {
                    NtpTimeDifference = NtpCheckTime - NtpTime;
                }
                elses
                {
                    NtpTimeDifference = NtpTime - NtpCheckTime;
                }
            }
            else
            {
                NtpCheckTime = DateTime.Now;
                if (NodeTimeAfterNtpTime == true)
                {
                    NtpTime = NtpCheckTime.Subtract(NtpTimeDifference);
                }
                else
                {
                    NtpTime = NtpCheckTime.Add(NtpTimeDifference);
                }
            }
        }
        private bool MessageTimeListAvailable(string _keyName, int timeOutSecond)
        {
            if (MessageTimeList.ContainsKey(_keyName) == false)
            {
                return true;
            }
            if ((DateTime.Now - MessageTimeList[_keyName]).TotalSeconds > timeOutSecond)
            {
                return true;
            }
            return false;
        }
        private void AddToMessageTimeList(string _keyName)
        {
            if (MessageTimeList.ContainsKey(_keyName) == false)
            {
                MessageTimeList.Add(_keyName, DateTime.Now);
            }
            else
            {
                MessageTimeList[_keyName] = DateTime.Now;
            }
        }
        public string PortToHex(int PortNo)
        {
            return PortNo.ToString("x").PadLeft(5, '0');
        }
        private string CalculateMainAddressListHash()
        {
            List<UInt64> tmpAllWordlTimeList = new List<UInt64>();
            foreach (KeyValuePair<string, IpInfo> entry in MainAddressList)
            {
                tmpAllWordlTimeList.Add(UInt64.Parse(entry.Key, NumberStyles.AllowHexSpecifier));
            }
            tmpAllWordlTimeList.Sort();
            return new Notus.Hash().CommonHash("sha1", JsonSerializer.Serialize(tmpAllWordlTimeList));
        }
        private void AddToMainAddressList(string ipAddress, int portNo, bool storeToDb = true)
        {
            string tmpHexKeyStr = IpPortToKey(ipAddress, portNo);
            if (MainAddressList.ContainsKey(tmpHexKeyStr) == false)
            {
                MainAddressList.Add(tmpHexKeyStr, new IpInfo()
                {
                    IpAddress = ipAddress,
                    Port = portNo,
                });
                if (storeToDb == true)
                {
                    StoreNodeListToDb();
                }
                MainAddressListHash = CalculateMainAddressListHash();
            }
        }
        private void AddToNodeList(NodeQueueInfo NodeQueueInfo)
        {

            string tmpNodeHexStr = IpPortToKey(NodeQueueInfo.IP.IpAddress, NodeQueueInfo.IP.Port);
            if (NodeList.ContainsKey(tmpNodeHexStr))
            {
                NodeList[tmpNodeHexStr] = NodeQueueInfo;
            }
            else
            {
                NodeList.Add(tmpNodeHexStr, NodeQueueInfo);
            }

            if (Obj_Settings.LocalNode == true)
            {
                NodeList[tmpNodeHexStr].InTheCode = true;
            }
            else
            {
                NodeList[tmpNodeHexStr].InTheCode = false;
                foreach (IpInfo entry in InTheCodeNodeList)
                {
                    if (string.Equals(entry.IpAddress, NodeQueueInfo.IP.IpAddress) && NodeQueueInfo.IP.Port == entry.Port)
                    {
                        NodeList[tmpNodeHexStr].InTheCode = true;
                    }
                }
            }

            AddToMainAddressList(NodeQueueInfo.IP.IpAddress, NodeQueueInfo.IP.Port);
        }
        private string CalculateMyNodeListHash()
        {
            List<string> tmpAllAddressList = new List<string>();
            List<string> tmpAllWalletList = new List<string>();
            List<long> tmpAllWordlTimeList = new List<long>();
            Dictionary<string, NodeQueueInfo> tmpNodeList = JsonSerializer.Deserialize<Dictionary<string, NodeQueueInfo>>(JsonSerializer.Serialize(NodeList));

            foreach (KeyValuePair<string, NodeQueueInfo> entry in tmpNodeList)
            {
                string tmpAddressListHex = IpPortToKey(entry.Value.IP.IpAddress, entry.Value.IP.Port);
                if (tmpAllAddressList.IndexOf(tmpAddressListHex) < 0)
                {
                    tmpAllAddressList.Add(tmpAddressListHex);
                    if (entry.Value.Wallet.Length == 0)
                    {
                        tmpAllWalletList.Add("#");
                    }
                    else
                    {
                        tmpAllWalletList.Add(entry.Value.Wallet);
                    }
                    tmpAllWordlTimeList.Add(entry.Value.Time.World.Ticks);
                }
            }
            tmpAllAddressList.Sort();
            tmpAllWalletList.Sort();
            tmpAllWordlTimeList.Sort();

            NodeListHash = new Notus.Hash().CommonHash("sha1",
                JsonSerializer.Serialize(tmpAllAddressList) + ":" +
                JsonSerializer.Serialize(tmpAllWalletList) + ":" +
                JsonSerializer.Serialize(tmpAllWordlTimeList)
            );

            return NodeListHash;
        }
        private void StoreNodeListToDb()
        {
            bool storeList = true;
            string tmpNodeListStr = ObjMp_NodeList.Get("ip_list", string.Empty);
            if (tmpNodeListStr != string.Empty)
            {
                SortedDictionary<string, IpInfo> tmpDbNodeList = JsonSerializer.Deserialize<SortedDictionary<string, IpInfo>>(tmpNodeListStr);
                if (
                    string.Equals(
                        JsonSerializer.Serialize(tmpDbNodeList),
                        JsonSerializer.Serialize(MainAddressList)
                    )
                )
                {
                    storeList = false;
                }
            }
            if (storeList)
            {
                ObjMp_NodeList.Set("ip_list", JsonSerializer.Serialize(MainAddressList), true);
            }
        }
        private string IpPortToKey(string ipAddress, int portNo)
        {
            string resultStr = "";
            foreach (string byteStr in ipAddress.Split("."))
            {
                resultStr += int.Parse(byteStr).ToString("x").PadLeft(2, '0');
            }
            return resultStr + portNo.ToString("x").PadLeft(5, '0');
        }

        private bool CheckXmlTag(string rawDataStr, string tagName)
        {
            return ((rawDataStr.IndexOf("<" + tagName + ">") >= 0 && rawDataStr.IndexOf("</" + tagName + ">") >= 0) ? true : false);
        }
        private string GetPureText(string rawDataStr, string tagName)
        {
            rawDataStr = rawDataStr.Replace("<" + tagName + ">", "");
            return rawDataStr.Replace("</" + tagName + ">", "");
        }
        public string Process(Notus.Variable.Struct.HttpRequestDetails incomeData)
        {
            return ProcessIncomeData(incomeData.PostParams["data"]);
        }
        private string ProcessIncomeData(string incomeData)
        {
            if (CheckXmlTag(incomeData, "hash"))
            {
                incomeData = GetPureText(incomeData, "hash");
                string[] tmpHashPart = incomeData.Split(':');
                if (string.Equals(tmpHashPart[0], MainAddressListHash.Substring(0, 20)) == false)
                {
                    return "1";
                }

                if (string.Equals(tmpHashPart[1], NodeListHash.Substring(0, 20)) == false)
                {
                    return "2";
                }
                return "0";
            }
            if (CheckXmlTag(incomeData, "time"))
            {
                incomeData = GetPureText(incomeData, "time");
                NextQueueValidNtpTime = Notus.Date.ToDateTime(incomeData);
                return "ok";
            }
            if (CheckXmlTag(incomeData, "node"))
            {
                incomeData = GetPureText(incomeData, "node");
                try
                {
                    NodeQueueInfo tmpNodeQueueInfo = JsonSerializer.Deserialize<NodeQueueInfo>(incomeData);
                    AddToNodeList(tmpNodeQueueInfo);
                }
                catch
                {

                }
                return "<node>" + JsonSerializer.Serialize(NodeList[MyNodeHexKey]) + "</node>";
            }
            if (CheckXmlTag(incomeData, "list"))
            {
                incomeData = GetPureText(incomeData, "list");
                SortedDictionary<string, IpInfo> tmpNodeList = JsonSerializer.Deserialize<SortedDictionary<string, IpInfo>>(incomeData);
                foreach (KeyValuePair<string, IpInfo> entry in tmpNodeList)
                {
                    AddToMainAddressList(entry.Value.IpAddress, entry.Value.Port, true);
                }
                return "<list>" + JsonSerializer.Serialize(MainAddressList) + "</list>";
            }
            return "<err>1</err>";
        }
        private string SendMessage(string receiverIpAddress, int receiverPortNo, string messageText, bool executeErrorControl)
        {
            string urlPath=Notus.Network.Node.MakeHttpListenerPath(receiverIpAddress, receiverPortNo)+ "queue/node";
            string incodeResponse =Notus.Communication.Request.PostSync(urlPath, new Dictionary<string, string>()
            {
                { "data",messageText }
            });
            return incodeResponse;
        }
        private string Message_Hash_ViaSocket(string _ipAddress, int _portNo, string _nodeHex = "")
        {
            if (_nodeHex == "")
            {
                _nodeHex = IpPortToKey(_ipAddress, _portNo);
            }
            string _nodeKeyText = _nodeHex + "hash";
            if (MessageTimeListAvailable(_nodeKeyText, 1))
            {
                AddToMessageTimeList(_nodeKeyText);
                return SendMessage(
                    _ipAddress,
                    _portNo,
                    "<hash>" +
                        MainAddressListHash.Substring(0, 20) + ":" + NodeListHash.Substring(0, 20) +
                    "</hash>",
                    true
                );
            }
            return "b";
        }
        private void Message_Node_ViaSocket(string _ipAddress, int _portNo, string _nodeHex = "")
        {
            if (_nodeHex == "")
            {
                _nodeHex = IpPortToKey(_ipAddress, _portNo);
            }
            string _nodeKeyText = _nodeHex + "node";
            if (MessageTimeListAvailable(_nodeKeyText, 2))
            {
                AddToMessageTimeList(_nodeKeyText);
                string responseStr = SendMessage(_ipAddress, _portNo,
                    "<node>" + JsonSerializer.Serialize(NodeList[MyNodeHexKey]) + "</node>",
                    true
                );
                if (string.Equals("err", responseStr) == false)
                {
                    ProcessIncomeData(responseStr);
                }
            }
        }
        private void Message_List_ViaSocket(string _ipAddress, int _portNo, string _nodeHex = "")
        {
            if (_nodeHex == "")
            {
                _nodeHex = IpPortToKey(_ipAddress, _portNo);
            }
            string _nodeKeyText = _nodeHex + "list";
            if (MessageTimeListAvailable(_nodeKeyText, 2))
            {
                AddToMessageTimeList(_nodeKeyText);
                string tmpReturnStr = SendMessage(_ipAddress, _portNo, "<list>" + JsonSerializer.Serialize(MainAddressList) + "</list>", true);
                if (string.Equals("err", tmpReturnStr) == false)
                {
                    ProcessIncomeData(tmpReturnStr);
                }
            }
        }
        private void MainLoop()
        {
            while (ExitFromLoop == false)
            {
                //burası belirli periyotlarda hash gönderiminin yapıldığı kod grubu
                if ((DateTime.Now - LastPingTime).TotalSeconds > 20 || SyncReady == false)
                {
                    bool innerControlLoop = false;
                    string tmpData = string.Empty;
                    while (innerControlLoop == false)
                    {
                        try
                        {
                            tmpData = JsonSerializer.Serialize(MainAddressList);
                            innerControlLoop = true;
                        }
                        catch { }
                    }
                    SortedDictionary<string, IpInfo> tmpMainAddressList = JsonSerializer.Deserialize<SortedDictionary<string, IpInfo>>(tmpData);
                    bool tmpRefreshNodeDetails = false;
                    foreach (KeyValuePair<string, IpInfo> entry in tmpMainAddressList)
                    {
                        string tmpNodeHexStr = IpPortToKey(entry.Value.IpAddress, entry.Value.Port);
                        if (string.Equals(MyNodeHexKey, tmpNodeHexStr) == false)
                        {
                            string tmpReturnStr = Message_Hash_ViaSocket(entry.Value.IpAddress, entry.Value.Port, "hash");
                            if (tmpReturnStr == "1") // list not equal
                            {
                                Message_List_ViaSocket(entry.Value.IpAddress, entry.Value.Port, tmpNodeHexStr);
                            }

                            if (tmpReturnStr == "2") // list equal but node hash different
                            {
                                tmpRefreshNodeDetails = true;
                            }

                            if (tmpReturnStr == "0") // list and node hash are equal
                            {
                            }

                            if (tmpReturnStr == "err") // socket comm error
                            {
                                tmpRefreshNodeDetails = true;
                            }
                        }
                    }
                    if (tmpRefreshNodeDetails == true)
                    {
                        foreach (KeyValuePair<string, IpInfo> entry in tmpMainAddressList)
                        {
                            string tmpNodeHexStr = IpPortToKey(entry.Value.IpAddress, entry.Value.Port);
                            if (string.Equals(MyNodeHexKey, tmpNodeHexStr) == false)
                            {
                                Message_Node_ViaSocket(entry.Value.IpAddress, entry.Value.Port, tmpNodeHexStr);
                            }
                        }
                    }
                    LastPingTime = DateTime.Now;
                }

                // burada durumu bilinmeyen nodeların bilgilerinin sorgulandığı kısım
                foreach (KeyValuePair<string, NodeQueueInfo> entry in NodeList)
                {
                    bool tmpRefreshNodeDetails = false;
                    string tmpCheckHex = IpPortToKey(entry.Value.IP.IpAddress, entry.Value.IP.Port);
                    if (entry.Value.Status == NodeStatus.Unknown)
                    {
                        tmpRefreshNodeDetails = true;
                    }
                    if (tmpRefreshNodeDetails == true)
                    {
                        Message_Node_ViaSocket(entry.Value.IP.IpAddress, entry.Value.IP.Port, tmpCheckHex);
                    }
                }
                NodeList[MyNodeHexKey].NodeHash = CalculateMyNodeListHash();
                int nodeCount = 0;
                SyncReady = true;
                //burada eğer nodeların hashleri farklı ise senkron olacağı kısım
                foreach (KeyValuePair<string, NodeQueueInfo> entry in NodeList)
                {
                    string tmpCheckHex = IpPortToKey(entry.Value.IP.IpAddress, entry.Value.IP.Port);

                    if (string.Equals(MyNodeHexKey, tmpCheckHex) == false)
                    {
                        if (entry.Value.Status == NodeStatus.Online && entry.Value.ErrorCount == 0)
                        {
                            nodeCount++;
                            if (NodeListHash != entry.Value.NodeHash)
                            {
                                SyncReady = false;
                                Message_Node_ViaSocket(entry.Value.IP.IpAddress, entry.Value.IP.Port, tmpCheckHex);
                            }
                        }
                    }
                }

                if (nodeCount == 0)
                {
                    SyncReady = false;
                }

                if (SyncReady == true)
                {
                    if (LastHashForStoreList != NodeListHash)
                    {
                        CalculateTimeDifference(true);
                        if (NtpTime > NextQueueValidNtpTime)
                        {
                            OrganizeQueue();
                            Val_Ready = true;
                        }
                        StoreNodeListToDb();
                    }
                }
            }
        }
        private void OrganizeQueue()
        {
            //önce geçerli node listesinin bir yedeği alınıyor ve önceki node listesi değişkeninde tutuluyor.
            PreviousNodeList = JsonSerializer.Deserialize<Dictionary<string, NodeQueueInfo>>(JsonSerializer.Serialize(NodeList));
            LastHashForStoreList = NodeListHash;

            Dictionary<BigInteger, string> tmpNodeTimeList = new Dictionary<BigInteger, string>();
            Dictionary<BigInteger, string> tmpWalletList = new Dictionary<BigInteger, string>();
            List<BigInteger> tmpWalletOrder = new List<BigInteger>();
            SortedDictionary<string, string> tmpWalletHashList = new SortedDictionary<string, string>();


            foreach (KeyValuePair<string, NodeQueueInfo> entry in PreviousNodeList)
            {
                if (entry.Value.ErrorCount == 0)
                {
                    BigInteger walletNo = Notus.Convert.FromBase58(entry.Value.Wallet);
                    tmpWalletList.Add(walletNo, entry.Value.Wallet);
                    tmpNodeTimeList.Add(walletNo, entry.Value.Time.World.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText));

                    tmpWalletOrder.Add(walletNo);
                }
            }
            tmpWalletOrder.Sort();
            string tmpSalt = new Notus.Hash().CommonHash("md5", string.Join("#", tmpWalletOrder.ToArray()));
            for (int i = 0; i < tmpWalletOrder.Count; i++)
            {
                string tmpWalletHash = new Notus.Hash().CommonHash("md5",
                    tmpSalt + tmpWalletList[tmpWalletOrder[i]] + tmpNodeTimeList[tmpWalletOrder[i]]
                );

                tmpWalletHashList.Add(tmpWalletHash, tmpWalletList[tmpWalletOrder[i]]);
            }

            int counter = 0;
            NodeOrderList.Clear();

            foreach (KeyValuePair<string, string> entry in tmpWalletHashList)
            {
                counter++;
                NodeOrderList.Add(counter, entry.Value);
            }

            MyTurn = (string.Equals(MyWallet, NodeOrderList[1]));
            if (MyTurn == true)
            {
                Notus.Print.Info(Obj_Settings.DebugMode,"My Turn");
                CalculateTimeDifference(false);
                RefreshNtpTime();
                foreach (KeyValuePair<string, NodeQueueInfo> entry in PreviousNodeList)
                {
                    if (
                        entry.Value.ErrorCount == 0 &&
                        entry.Value.Status == NodeStatus.Online &&
                        string.Equals(entry.Value.Wallet, MyWallet) == false
                    )
                    {
                        SendMessage(
                            entry.Value.IP.IpAddress,
                            entry.Value.IP.Port,
                            "<time>" + Notus.Date.ToString(NextQueueValidNtpTime) + "</time>",
                            true
                        );
                    }
                }
            }
            else
            {
                Notus.Print.Info(Obj_Settings.DebugMode, "Waiting For Turn");
            }
            NodeList[MyNodeHexKey].Time.Node = DateTime.Now;
            NodeList[MyNodeHexKey].Time.World = NtpTime;
        }
        public void Start()
        {
            Task.Run(() =>
            {
                MainLoop();
            });
        }

        public void PreStart()
        {
            MyPortNo = Obj_Settings.Port.MainNet;
            
            if (Obj_Settings.Network == Variable.Enum.NetworkType.TestNet)
                MyPortNo = Obj_Settings.Port.TestNet;
            
            if (Obj_Settings.Network == Variable.Enum.NetworkType.DevNet)
                MyPortNo = Obj_Settings.Port.DevNet;

            InTheCodeNodeList.Clear();
            foreach (string defaultIpAddress in Notus.Variable.Constant.ListMainNodeIp)
            {
                InTheCodeNodeList.Add(new IpInfo()
                {
                    IpAddress = defaultIpAddress,
                    Port = MyPortNo
                }
                );
            }
            MyIpAddress = (Obj_Settings.LocalNode == true ? Obj_Settings.IpInfo.Local : Obj_Settings.IpInfo.Public);
            MyNodeHexKey = IpPortToKey(MyIpAddress, MyPortNo);

            if (Obj_Settings.LocalNode == true)
            {
                AddToMainAddressList(Obj_Settings.IpInfo.Local, MyPortNo, false);
            }
            else
            {
                foreach (IpInfo entry in InTheCodeNodeList)
                {
                    AddToMainAddressList(entry.IpAddress, entry.Port, false);
                }
            }
            
            MyWallet = Obj_Settings.NodeWallet.WalletKey;
            CalculateTimeDifference(false);

            string tmpNodeListStr = ObjMp_NodeList.Get("ip_list", string.Empty);
            if (tmpNodeListStr == string.Empty)
            {
                StoreNodeListToDb();
            }
            else
            {
                SortedDictionary<string, IpInfo> tmpDbNodeList = JsonSerializer.Deserialize<SortedDictionary<string, IpInfo>>(tmpNodeListStr);
                foreach (KeyValuePair<string, IpInfo> entry in tmpDbNodeList)
                {
                    AddToMainAddressList(entry.Value.IpAddress, entry.Value.Port);
                }
            }
            AddToNodeList(new NodeQueueInfo()
            {
                ErrorCount = 0,
                NodeHash = "#",
                Status = NodeStatus.Online,
                Time = new NodeQueueInfo_Time()
                {
                    Node = NtpCheckTime,
                    World = NtpTime,
                    Error = DefaultTime
                },
                Wallet = MyWallet,
                IP = new IpInfo()
                {
                    IpAddress = MyIpAddress,
                    Port = MyPortNo
                }
            });


            foreach (KeyValuePair<string, IpInfo> entry in MainAddressList)
            {
                if (string.Equals(MyNodeHexKey, IpPortToKey(entry.Value.IpAddress, entry.Value.Port)) == false)
                {
                    AddToNodeList(new NodeQueueInfo()
                    {
                        ErrorCount = 0,
                        NodeHash = "#",
                        Status = NodeStatus.Unknown,
                        Time = new NodeQueueInfo_Time()
                        {
                            Node = DefaultTime,
                            World = DefaultTime,
                            Error = DefaultTime
                        },
                        Wallet = "#",
                        IP = new IpInfo()
                        {
                            IpAddress = entry.Value.IpAddress,
                            Port = entry.Value.Port,
                        }
                    });
                }
            }
            NodeList[MyNodeHexKey].NodeHash = CalculateMyNodeListHash();
        }
        public Queue()
        {
            NodeList.Clear();
            MessageTimeList.Clear();
            NtpCheckTime = DefaultTime;
            LastPingTime = DefaultTime;
            NextQueueValidNtpTime = DefaultTime;
            ObjMp_NodeList = new Notus.Mempool("node_pool_list");
            ObjMp_NodeList.AsyncActive = false;
        }
        ~Queue()
        {
            Dispose();
        }
        public void Dispose()
        {
            ExitFromLoop = true;
            if (ObjMp_NodeList != null)
            {
                ObjMp_NodeList.Dispose();
            }
        }
    }
}
