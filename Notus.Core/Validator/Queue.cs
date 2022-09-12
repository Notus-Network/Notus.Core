using System;
using System.Threading;
using System.Collections.Generic;
using Notus.Variable.Struct;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;

namespace Notus.Validator
{
    public class Queue : IDisposable
    {
        public DateTime StartingTimeAfterEnoughNode;

        private bool WaitForEnoughNode_Val = true;
        public bool WaitForEnoughNode
        {
            get { return WaitForEnoughNode_Val; }
        }

        public bool NotEnoughNode_Printed = false;
        public bool NotEnoughNode_Val = true;
        public bool NotEnoughNode
        {
            get { return NotEnoughNode_Val; }
        }
        public int ActiveNodeCount_Val = 0;
        public int ActiveNodeCount
        {
            get { return ActiveNodeCount_Val; }
        }

        private Notus.Variable.Common.ClassSetting Obj_Settings;
        public Notus.Variable.Common.ClassSetting Settings
        {
            get { return Obj_Settings; }
            set { Obj_Settings = value; }
        }
        private bool Val_Ready = false;
        public bool Ready
        {
            get { return Val_Ready; }
        }
        private bool MyTurn_Val = false;
        public bool MyTurn
        {
            get { return MyTurn_Val; }
        }
        private bool SyncReady = true;

        private SortedDictionary<string, IpInfo> MainAddressList = new SortedDictionary<string, IpInfo>();
        private string MainAddressListHash = string.Empty;

        private Dictionary<string, NodeQueueInfo>? PreviousNodeList = new Dictionary<string, NodeQueueInfo>();
        public Dictionary<string, NodeQueueInfo>? SyncNodeList
        {
            get { return PreviousNodeList; }
            set { PreviousNodeList = value; }
        }
        private Dictionary<string, int> NodeTurnCount = new Dictionary<string, int>();
        private Dictionary<string, NodeQueueInfo> NodeList = new Dictionary<string, NodeQueueInfo>();
        private Dictionary<string, DateTime> MessageTimeList = new Dictionary<string, DateTime>();
        private Dictionary<int, string> NodeOrderList = new Dictionary<int, string>();

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
        private bool NtpTimeWorked = false;               // we get time from ntp server
        private DateTime NtpCheckTime;                  // last check ntp time
        private TimeSpan NtpTimeDifference;             // time difference between NTP server and current node
        private bool NodeTimeAfterNtpTime = false;      // time difference before or after NTP Server
        private DateTime NextQueueValidNtpTime;         // New Queue will be usable after this NTP time

        public System.Func<Notus.Variable.Class.BlockData, bool>? Func_NewBlockIncome = null;

        //empty blok için kontrolü yapacak olan node'u seçen fonksiyon
        public Notus.Variable.Enum.ValidatorOrder EmptyTimer()
        {
            return Notus.Variable.Enum.ValidatorOrder.Primary;
        }

        //oluşturulacak blokları kimin oluşturacağını seçen fonksiyon
        public void Distrubute(long blockRowNo,int blockType=0)
        {
            foreach (KeyValuePair<string, NodeQueueInfo> entry in NodeList)
            {
                if (string.Equals(MyNodeHexKey, entry.Key) == false && entry.Value.Status == NodeStatus.Online)
                {
                    Notus.Print.Info(Obj_Settings, 
                        "Distrubuting " + 
                        blockRowNo.ToString() +"[ " +
                        blockType.ToString()+ 
                        " ] . Block To " + 
                        entry.Value.IP.IpAddress+":"+ 
                        entry.Value.IP.Port.ToString()
                    );
                    SendMessage(entry.Value.IP,
                        "<block>" + blockRowNo.ToString() + ":" + 
                        Obj_Settings.NodeWallet.WalletKey + "</block>",
                        true
                    );
                }
            }
        }

        private DateTime RefreshNtpTime(ulong MaxSecondCount)
        {
            DateTime tmpNtpTime = NtpTime;
            const ulong secondPointConst = 1000;

            DateTime afterMiliSecondTime = tmpNtpTime.AddMilliseconds(
                secondPointConst + (secondPointConst - (Notus.Date.ToLong(NtpTime) % secondPointConst))
            );
            double secondVal = MaxSecondCount + (MaxSecondCount - (ulong.Parse(afterMiliSecondTime.ToString("ss")) % MaxSecondCount));
            return afterMiliSecondTime.AddSeconds(secondVal);
        }
        public void GetUtcTimeFromServer()
        {
            NtpTime = Notus.Time.GetFromNtpServer(true);
            NtpCheckTime = DateTime.Now;
            NodeTimeAfterNtpTime = (NtpCheckTime > NtpTime);
            if (NodeTimeAfterNtpTime == true)
            {
                NtpTimeDifference = NtpCheckTime - NtpTime;
            }
            else
            {
                NtpTimeDifference = NtpTime - NtpCheckTime;
            }
            NtpTimeWorked = true;
        }
        public DateTime GetUtcTime()
        {
            return NtpTime;
        }
        private void CalculateTimeDifference(bool useLocalValue)
        {
            if ((DateTime.Now - NtpCheckTime).TotalMinutes > 10 && useLocalValue == false)
            {
                DateTime tmpNtpTime;
                if (NtpTimeWorked == false)
                {
                    tmpNtpTime = Notus.Time.GetFromNtpServer(true);
                    NtpTimeWorked = true;
                }
                else
                {
                    tmpNtpTime = Notus.Time.GetFromNtpServer();
                }
                if (tmpNtpTime > NtpTime)
                {
                    NtpTime = tmpNtpTime;
                    NtpCheckTime = DateTime.Now;
                    NodeTimeAfterNtpTime = (NtpCheckTime > NtpTime);
                    if (NodeTimeAfterNtpTime == true)
                    {
                        NtpTimeDifference = NtpCheckTime - NtpTime;
                    }
                    else
                    {
                        NtpTimeDifference = NtpTime - NtpCheckTime;
                    }
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
        public void PingOtherNodes()
        {
            Notus.Print.Info(Obj_Settings, "Waiting For Node Sync", false);
            bool tmpExitWhileLoop = false;
            while (tmpExitWhileLoop == false)
            {
                int tmpNodeCount = 0;
                foreach (KeyValuePair<string, IpInfo> entry in MainAddressList)
                {
                    if (string.Equals(entry.Key, MyNodeHexKey) == false)
                    {
                        string urlPath =
                            Notus.Network.Node.MakeHttpListenerPath(
                                entry.Value.IpAddress, 
                                entry.Value.Port
                            ) + "ping/";
                        string incodeResponse = Notus.Communication.Request.GetSync(
                            urlPath, 2, true, false, null
                        );
                        if (string.Equals(incodeResponse, "pong"))
                        {
                            tmpNodeCount++;
                        }
                    }
                }
                if (tmpNodeCount > 0)
                {
                    tmpExitWhileLoop = true;
                }
                else
                {
                    Thread.Sleep(5500);
                }
            }
            Thread.Sleep(500);
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
        public List<IpInfo> GiveMeNodeList()
        {
            List<IpInfo> tmpNodeList = new List<IpInfo>();
            foreach (KeyValuePair<string, IpInfo> entry in MainAddressList)
            {
                if (string.Equals(entry.Key, MyNodeHexKey) == false)
                {
                    tmpNodeList.Add(new IpInfo()
                    {
                        IpAddress = entry.Value.IpAddress,
                        Port = entry.Value.Port
                    });
                }
            }
            return tmpNodeList;
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
                foreach (IpInfo entry in Notus.Validator.List.Main[Obj_Settings.Layer][Obj_Settings.Network])
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
            Dictionary<string, NodeQueueInfo>? tmpNodeList = JsonSerializer.Deserialize<Dictionary<string, NodeQueueInfo>>(JsonSerializer.Serialize(NodeList));
            if (tmpNodeList == null)
            {
                return string.Empty;
            }

            List<string> tmpAllAddressList = new List<string>();
            List<string> tmpAllWalletList = new List<string>();
            List<long> tmpAllWordlTimeList = new List<long>();
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
        public string IpPortToKey(string ipAddress, int portNo)
        {
            string resultStr = "";
            foreach (string byteStr in ipAddress.Split("."))
            {
                resultStr += int.Parse(byteStr).ToString("x").PadLeft(2, '0');
            }
            return resultStr.ToLower() + portNo.ToString("x").PadLeft(5, '0').ToLower();
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
            string reponseText = ProcessIncomeData(incomeData.PostParams["data"]);
            NodeIsOnline(incomeData.UrlList[2].ToLower());
            return reponseText;
        }
        private string ProcessIncomeData(string incomeData)
        {
            if (CheckXmlTag(incomeData, "block"))
            {
                string incomeDataStr = GetPureText(incomeData, "block");
                if (incomeDataStr.IndexOf(":") < 0)
                {
                    return "error-msg";
                }

                string[] tmpArr = incomeDataStr.Split(":");
                long tmpBlockNo = long.Parse(tmpArr[0]);
                string tmpNodeWalletKey = tmpArr[1];
                string tmpIpAddress = string.Empty;
                int tmpPortNo = 0;
                foreach (KeyValuePair<string, NodeQueueInfo> entry in NodeList)
                {
                    if (string.Equals(entry.Value.Wallet, tmpNodeWalletKey))
                    {
                        tmpIpAddress = entry.Value.IP.IpAddress;
                        tmpPortNo = entry.Value.IP.Port;
                    }
                    if (
                        entry.Value.Status == NodeStatus.Online &&
                        entry.Value.Ready == true &&
                        entry.Value.ErrorCount == 0
                    )
                    {

                    }
                }
                if (tmpPortNo == 0)
                {
                    return "fncResult-port-zero";
                }
                Variable.Class.BlockData? tmpBlockData =
                    Notus.Toolbox.Network.GetBlockFromNode(tmpIpAddress, tmpPortNo, tmpBlockNo, Obj_Settings);
                if (tmpBlockData == null)
                {
                    return "tmpError-true";
                }
                if (Func_NewBlockIncome != null)
                {
                    bool fncResult = Func_NewBlockIncome(tmpBlockData);
                    if (fncResult == true)
                    {
                        return "done";
                    }
                }
                return "fncResult-false";
            }

            if (CheckXmlTag(incomeData, "when"))
            {
                Console.WriteLine("When = Is Come");
                StartingTimeAfterEnoughNode = Notus.Date.ToDateTime(GetPureText(incomeData, "when"));
                Console.WriteLine(StartingTimeAfterEnoughNode);
                return "done";
            }
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
            if (CheckXmlTag(incomeData, "ready"))
            {
                incomeData = GetPureText(incomeData, "ready");
                Console.WriteLine("Ready Income : " + incomeData);
                foreach (KeyValuePair<string, NodeQueueInfo> entry in NodeList)
                {
                    if (string.Equals(entry.Value.Wallet, incomeData) == true)
                    {
                        //Console.WriteLine("Ready Income Became");
                        NodeList[entry.Key].Ready = true;
                    }
                }
                return "done";
            }
            if (CheckXmlTag(incomeData, "node"))
            {
                incomeData = GetPureText(incomeData, "node");
                try
                {
                    NodeQueueInfo? tmpNodeQueueInfo = JsonSerializer.Deserialize<NodeQueueInfo>(incomeData);
                    if (tmpNodeQueueInfo != null)
                    {
                        AddToNodeList(tmpNodeQueueInfo);
                    }
                }
                catch(Exception err)
                {
                    Notus.Print.Log(
                        Notus.Variable.Enum.LogLevel.Info,
                        286321,
                        err.Message,
                        "BlockRowNo",
                        null,
                        err
                    );
                }
                return "<node>" + JsonSerializer.Serialize(NodeList[MyNodeHexKey]) + "</node>";
            }
            if (CheckXmlTag(incomeData, "list"))
            {
                incomeData = GetPureText(incomeData, "list");
                SortedDictionary<string, IpInfo>? tmpNodeList = JsonSerializer.Deserialize<SortedDictionary<string, IpInfo>>(incomeData);
                if (tmpNodeList == null)
                {
                    return "<err>1</err>";
                }
                foreach (KeyValuePair<string, IpInfo> entry in tmpNodeList)
                {
                    AddToMainAddressList(entry.Value.IpAddress, entry.Value.Port, true);
                }
                return "<list>" + JsonSerializer.Serialize(MainAddressList) + "</list>";
            }
            return "<err>1</err>";
        }
        private void NodeError(string nodeHexText)
        {
            if (NodeList.ContainsKey(nodeHexText) == true)
            {
                NodeList[nodeHexText].ErrorCount++;
                NodeList[nodeHexText].Status = NodeStatus.Offline;
                NodeList[nodeHexText].Ready = false;
                NodeList[nodeHexText].Time.Error = DateTime.Now;
            }
        }
        private void NodeIsOnline(string nodeHexText)
        {
            if (NodeList.ContainsKey(nodeHexText) == true)
            {
                NodeList[nodeHexText].ErrorCount = 0;
                NodeList[nodeHexText].Status = NodeStatus.Online;
                NodeList[nodeHexText].Time.Error = Notus.Variable.Constant.DefaultTime;
            }
        }
        private string SendMessage(Notus.Variable.Struct.IpInfo receiverNode, string messageText, bool executeErrorControl)
        {
            return SendMessage(receiverNode.IpAddress, receiverNode.Port, messageText, executeErrorControl);
        }
        private string SendMessage(string receiverIpAddress, int receiverPortNo, string messageText, bool executeErrorControl)
        {
            string tmpNodeHexStr = IpPortToKey(receiverIpAddress, receiverPortNo);
            TimeSpan tmpErrorDiff = DateTime.Now - NodeList[tmpNodeHexStr].Time.Error;
            if (tmpErrorDiff.TotalSeconds > 60)
            {
                string urlPath =
                    Notus.Network.Node.MakeHttpListenerPath(receiverIpAddress, receiverPortNo) +
                    "queue/node/" + tmpNodeHexStr;
                (bool worksCorrent, string incodeResponse) = Notus.Communication.Request.PostSync(
                    urlPath,
                    new Dictionary<string, string>()
                    {
                        { "data",messageText }
                    },
                    2,
                    true,
                    false
                );
                if (worksCorrent == true)
                {
                    NodeList[tmpNodeHexStr].ErrorCount = 0;
                    NodeList[tmpNodeHexStr].Status = NodeStatus.Online;
                    NodeList[tmpNodeHexStr].Time.Error = Notus.Variable.Constant.DefaultTime;
                    return incodeResponse;
                }
                NodeError(tmpNodeHexStr);
            }
            return string.Empty;
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
                        catch (Exception err){
                            Notus.Print.Log(
                                Notus.Variable.Enum.LogLevel.Info,
                                986547,
                                err.Message,
                                "BlockRowNo",
                                null,
                                err
                            );
                        }
                    }
                    SortedDictionary<string, IpInfo>? tmpMainAddressList = JsonSerializer.Deserialize<SortedDictionary<string, IpInfo>>(tmpData);
                    bool tmpRefreshNodeDetails = false;
                    if (tmpMainAddressList != null)
                    {
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
                    if (entry.Value.Status == NodeStatus.Online && entry.Value.ErrorCount == 0)
                    {
                        nodeCount++;
                        string tmpCheckHex = IpPortToKey(entry.Value.IP.IpAddress, entry.Value.IP.Port);
                        if (string.Equals(MyNodeHexKey, tmpCheckHex) == false)
                        {
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
                            CheckNodeCount();
                        }
                        StoreNodeListToDb();
                    }
                }
            }
        }

        private void CheckNodeCount()
        {
            int nodeCount = 0;
            foreach (KeyValuePair<string, NodeQueueInfo> entry in NodeList)
            {
                if (
                    entry.Value.Status == NodeStatus.Online && 
                    entry.Value.ErrorCount == 0 &&
                    entry.Value.Ready==true
                )
                {
                    nodeCount++;
                }
            }
            ActiveNodeCount_Val = nodeCount;

            //burada ready olduğu seçilmediğinden dolayı
            //sending ready mesajı gönderilemiyor

            if (ActiveNodeCount_Val > 1 && Val_Ready == true)
            {
                if (NodeList[MyNodeHexKey].Ready == false)
                {
                    Console.WriteLine("Control-Point-2");
                    MyNodeIsReady();
                }
                if (NotEnoughNode_Val == true) // ilk aşamada buraya girecek
                {
                    //Notus.Print.Basic(Obj_Settings, "Notus.Validator.Queue -> Line 820");
                    Notus.Print.Info(Obj_Settings, "Active Node Count : " + ActiveNodeCount_Val.ToString());
                    SortedDictionary<BigInteger, string> tmpWalletList = new SortedDictionary<BigInteger, string>();
                    foreach (KeyValuePair<string, NodeQueueInfo> entry in NodeList)
                    {
                        if (entry.Value.Status == NodeStatus.Online && entry.Value.ErrorCount == 0)
                        {
                            BigInteger walletNo = BigInteger.Parse(
                                new Notus.Hash().CommonHash("sha1", entry.Value.Wallet),
                                NumberStyles.AllowHexSpecifier
                            );
                            if (tmpWalletList.ContainsKey(walletNo) == false)
                            {
                                tmpWalletList.Add(walletNo, entry.Value.Wallet);
                            }
                        }
                    }
                    string tmpFirstWallet = tmpWalletList.First().Value;
                    if (string.Equals(tmpFirstWallet, MyWallet))
                    {
                        StartingTimeAfterEnoughNode = RefreshNtpTime(Notus.Variable.Constant.NodeStartingSync);
                        Notus.Print.Info(Obj_Settings, 
                            "I'm Sending Starting (When) Time / Current : " + 
                            StartingTimeAfterEnoughNode.ToString("HH:mm:ss.fff") +
                            " / " + GetUtcTime().ToString("HH:mm:ss.fff")
                        );
                        foreach (KeyValuePair<string, NodeQueueInfo> entry in NodeList)
                        {
                            if (entry.Value.Status == NodeStatus.Online && entry.Value.ErrorCount == 0)
                            {
                                SendMessage(
                                    entry.Value.IP,
                                    "<when>" +
                                        StartingTimeAfterEnoughNode.ToString(
                                            Notus.Variable.Constant.DefaultDateTimeFormatText
                                        ) +
                                    "</when>",
                                    true
                                );
                            }
                        }
                    }
                    else
                    {
                        //listen and wait
                        for (int x = 0; x < 100; x++)
                        {
                            Thread.Sleep(20);
                        }
                        Notus.Print.Info(Obj_Settings, 
                            "I'm Waiting Starting (When) Time / Current : " + 
                            StartingTimeAfterEnoughNode.ToString("HH:mm:ss.fff") +
                            " /  " + 
                            GetUtcTime().ToString("HH:mm:ss.fff")
                        );
                    }
                }
                if (GetUtcTime() > StartingTimeAfterEnoughNode)
                {
                    OrganizeQueue();
                }
                NotEnoughNode_Val = false;
                NotEnoughNode_Printed = false;
            }
            else
            {
                NotEnoughNode_Val = true;
                WaitForEnoughNode_Val = true;
                if (NotEnoughNode_Printed == false)
                {
                    NotEnoughNode_Printed = true;
                    Notus.Print.Basic(Obj_Settings, "Waiting For Enough Node");
                }
            }
        }

        private void OrganizeQueue()
        {
            /*
            Console.WriteLine(
                JsonSerializer.Serialize(NodeList, Notus.Variable.Constant.JsonSetting)
            );
            */

            //önce geçerli node listesinin bir yedeği alınıyor ve önceki node listesi değişkeninde tutuluyor.
            PreviousNodeList = JsonSerializer.Deserialize<Dictionary<string, NodeQueueInfo>>(
                JsonSerializer.Serialize(NodeList)
            );
            LastHashForStoreList = NodeListHash;

            Dictionary<BigInteger, string> tmpNodeTimeList = new Dictionary<BigInteger, string>();
            Dictionary<BigInteger, string> tmpWalletList = new Dictionary<BigInteger, string>();
            List<BigInteger> tmpWalletOrder = new List<BigInteger>();
            SortedDictionary<string, string> tmpWalletHashList = new SortedDictionary<string, string>();
            foreach (KeyValuePair<string, NodeQueueInfo> entry in PreviousNodeList)
            {
                if (entry.Value.ErrorCount == 0 && entry.Value.Ready == true)
                {
                    BigInteger walletNo = Notus.Convert.FromBase58(entry.Value.Wallet);
                    tmpWalletList.Add(walletNo, entry.Value.Wallet);
                    tmpNodeTimeList.Add(walletNo, entry.Value.Time.World.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText));

                    tmpWalletOrder.Add(walletNo);
                }
            }
            //omergoksoy
            /*
            */

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

            NodeTurnCount.Clear();
            foreach (KeyValuePair<int, string> entry in NodeOrderList)
            {
                if (NodeTurnCount.ContainsKey(entry.Value) == false)
                {
                    NodeTurnCount.Add(entry.Value, 0);
                }
            }
            /*

            empty blok sayısı toplanacak


            empty blok + transaction sayısı + blok sayısı
            ---------------------------------------------= İşlem başına ödül miktarı
                             ödül miktarı


            toplam ödül miktarından vakıf payı çıkarılacak ( % 2 )
            ayrıca 10 blok ödülü seçilecek bir kişiye verilecek


            */
            int myRewardCount = NodeTurnCount[NodeOrderList[1]];
            int minRewardCount = int.MaxValue;
            int maxRewardCount = 0;
            foreach (KeyValuePair<string, int> entry in NodeTurnCount)
            {
                if (entry.Value > maxRewardCount)
                {
                    maxRewardCount = entry.Value;
                }
                if (minRewardCount> entry.Value)
                {
                    minRewardCount = entry.Value;
                }
            }


            // Console.WriteLine("+++++++++++++++++++++++++++++++++++++++++");
            // Console.WriteLine(JsonSerializer.Serialize(NodeOrderList));
            MyTurn_Val = (string.Equals(MyWallet, NodeOrderList[1]));

            if (MyTurn_Val == true)
            {
                //Notus.Print.Info(Obj_Settings, "My Turn");
                CalculateTimeDifference(false);
                NextQueueValidNtpTime = RefreshNtpTime(Notus.Variable.Constant.NodeSortFrequency);
                foreach (KeyValuePair<string, NodeQueueInfo> entry in PreviousNodeList)
                {
                    if (
                        entry.Value.ErrorCount == 0 &&
                        entry.Value.Status == NodeStatus.Online &&
                        entry.Value.Ready == true &&
                        string.Equals(entry.Value.Wallet, MyWallet) == false
                    )
                    {
                        SendMessage(
                            entry.Value.IP,
                            "<time>" + Notus.Date.ToString(NextQueueValidNtpTime) + "</time>",
                            true
                        );
                    }
                }
            }
            else
            {
                //Notus.Print.Info(Obj_Settings, "Waiting For Turn");
            }
            NodeList[MyNodeHexKey].Time.Node = DateTime.Now;
            NodeList[MyNodeHexKey].Time.World = NtpTime;
            WaitForEnoughNode_Val = false;
        }
        public void Start()
        {
            Notus.Print.Info(Obj_Settings, "Getting UTC Time From NTP Server");
            CalculateTimeDifference(false);
            Task.Run(() =>
            {
                MainLoop();
            });
        }

        public void PreStart(
            long lastBlockRowNo,
            string lastBlockUid,
            string lastBlockSign,
            string lastBlockPrev
        )
        {
            //Console.WriteLine("PreStart : " + lastBlockRowNo.ToString() + " - " + lastBlockUid + " - " + lastBlockSign + " - " + lastBlockPrev);
            MyPortNo = Notus.Toolbox.Network.GetNetworkPort(Obj_Settings);

            MyIpAddress = (Obj_Settings.LocalNode == true ? Obj_Settings.IpInfo.Local : Obj_Settings.IpInfo.Public);
            MyNodeHexKey = IpPortToKey(MyIpAddress, MyPortNo);
            //Notus.Print.Basic(Obj_Settings, "My Node Hex Key : " + MyNodeHexKey);
            if (Obj_Settings.LocalNode == true)
            {
                AddToMainAddressList(Obj_Settings.IpInfo.Local, MyPortNo, false);
            }
            else
            {
                foreach (IpInfo defaultNodeInfo in Notus.Validator.List.Main[Obj_Settings.Layer][Obj_Settings.Network])
                {
                    AddToMainAddressList(defaultNodeInfo.IpAddress, defaultNodeInfo.Port, false);
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
                Ready = false,
                NodeHash = "#",
                Status = NodeStatus.Online,
                Time = new NodeQueueInfo_Time()
                {
                    Node = NtpCheckTime,
                    World = NtpTime,
                    Error = Notus.Variable.Constant.DefaultTime
                },
                Wallet = MyWallet,
                IP = new IpInfo()
                {
                    IpAddress = MyIpAddress,
                    Port = MyPortNo
                },
                LastRowNo = lastBlockRowNo,
                LastSign = lastBlockSign,
                LastUid = lastBlockUid,
                LastPrev = lastBlockPrev
            });

            foreach (KeyValuePair<string, IpInfo> entry in MainAddressList)
            {
                if (string.Equals(MyNodeHexKey, IpPortToKey(entry.Value.IpAddress, entry.Value.Port)) == false)
                {
                    AddToNodeList(new NodeQueueInfo()
                    {
                        Ready = false,
                        ErrorCount = 0,
                        NodeHash = "#",
                        Status = NodeStatus.Unknown,
                        Time = new NodeQueueInfo_Time()
                        {
                            Node = Notus.Variable.Constant.DefaultTime,
                            World = Notus.Variable.Constant.DefaultTime,
                            Error = Notus.Variable.Constant.DefaultTime
                        },
                        Wallet = "#",
                        IP = new IpInfo()
                        {
                            IpAddress = entry.Value.IpAddress,
                            Port = entry.Value.Port,
                        },
                        LastRowNo = 0,
                        LastSign = string.Empty,
                        LastPrev = string.Empty,
                        LastUid = string.Empty
                    });
                }
            }
            NodeList[MyNodeHexKey].NodeHash = CalculateMyNodeListHash();
        }
        private void Message_Ready_ViaSocket(string _ipAddress, int _portNo, string _nodeHex = "")
        {
            if (_nodeHex == "")
            {
                _nodeHex = IpPortToKey(_ipAddress, _portNo);
            }
            string _nodeKeyText = _nodeHex + "ready";
            if (MessageTimeListAvailable(_nodeKeyText, 2))
            {
                AddToMessageTimeList(_nodeKeyText);
                string responseStr = SendMessage(_ipAddress, _portNo,
                    "<ready>" + NodeList[MyNodeHexKey].Wallet + "</ready>",
                    true
                );
                //Console.WriteLine("_ipAddress / _portNo: " + _ipAddress + " : "+ _portNo.ToString());
                //Console.WriteLine("responseStr : " + responseStr);
                if (string.Equals("done", responseStr.Trim()) == true)
                {
                    ProcessIncomeData(responseStr);
                }
                else
                {
                    Notus.Print.Danger(Obj_Settings, "Ready Signal Doesnt Received From Node -> Queue -> Line 998");
                }
            }
            else
            {
                //Console.WriteLine("Ready Signal Were Sended Before");
            }
        }
        public void MyNodeIsReady()
        {
            NodeList[MyNodeHexKey].Ready = true;
            Val_Ready = true;
            if (ActiveNodeCount_Val > 1)
            {
                Notus.Print.Info(Obj_Settings, "Sending Ready Signal To Other Nodes");
                NodeList[MyNodeHexKey].Ready = true;
                foreach (KeyValuePair<string, IpInfo> entry in MainAddressList)
                {
                    string tmpNodeHexStr = IpPortToKey(entry.Value.IpAddress, entry.Value.Port);
                    if (string.Equals(MyNodeHexKey, tmpNodeHexStr) == false)
                    {
                        Message_Ready_ViaSocket(entry.Value.IpAddress, entry.Value.Port, tmpNodeHexStr);
                    }
                }
            }
        }
        public Queue()
        {
            NodeList.Clear();
            MessageTimeList.Clear();

            NtpTime = Notus.Variable.Constant.DefaultTime;
            NtpCheckTime = Notus.Variable.Constant.DefaultTime;
            LastPingTime = Notus.Variable.Constant.DefaultTime;

            NextQueueValidNtpTime = Notus.Variable.Constant.DefaultTime;
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
