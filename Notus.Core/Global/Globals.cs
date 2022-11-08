using Notus.Compression.TGZ;
using Notus.Globals.Variable;
using Notus.Sync;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ND = Notus.Date;
using NP = Notus.Print;
using NVC = Notus.Variable.Constant;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Variable
{
    static class Globals
    {
        /*


        * minimum gereken node sayısı -> 6 adet node
        * 
            * EMPTY BLOK
            * empty blok belirlenen süre içerisinde oluşturulacak
                * eğer belirlenen süre içerisinde oluşturulması gereken empty blok oluşturulmazsa
                  o zaman önce eksik kalan empty bloklar oluşturulana kadar başka blok üretilmeyecek

                * 
                *
                *
        * 
        * bu nodelar sıraya girecek
            * 
            * 1. node 
            * 
            * 0 ile 0,2 saniye arasını işlem dinlemek için ayıracak
            * 0,2 ile 0,3 saniye arasını bloğu oluşturmak için ayıracak
            * 0,3 ile 0,5 saniye arasını ilk 20 arasındaki node'lara blokları dağıtmak için harcayacak.
            * eğer kendinden sonraki 5 node 1. node'dan haber alamazsa
                * birinci node'un oluşturduğu blok gözardı edilecek
                * 


        |-------|-------|-------|-------|-------|-------|-------|-------|-------|-------|-------|-------|
        0      0,2     0,5     0,7     1,0     1,2     1,5     1,7     2,0     2,2     2,5     2,7     3,0





        ilk başlangıçta 2 node senkron olarak başlayacak ve aralarında starting time için karar verecekler.
        sonrasında ağa eklenecek olan node önce ağdaki 2 node'un startingtime zamanını alacak.
        yeni node blok senkronizasyonunu tamamladıktan sonra
        şu an ki zamanın üzerine 1 dakika ekleyecek ve o zaman geldiğinde kuyruğa dahil edilmiş olacak

        */
        public static bool LocalBlockLoaded { get; set; }
        public static int GroupNo { get; set; }
        public static string SessionPrivateKey { get; set; }
        public static bool NodeListPrinted { get; set; }
        public static TimeStruct NOW { get; set; }
        public static Notus.Globals.Variable.NodeQueueList NodeQueue { get; set; }
        public static int OnlineNodeCount { get; set; }

        public static ConcurrentDictionary<string, NVS.NodeQueueInfo> NodeList { get; set; }
        public static Notus.Globals.Variable.Settings Settings { get; set; }
        static Globals()
        {
            LocalBlockLoaded = false;
            GroupNo = 1;
            SessionPrivateKey = Notus.Wallet.ID.New();

            Settings = new Notus.Globals.Variable.Settings()
            {
                WaitForGeneratedBlock = false,
                NodeClosing = false,
                ClosingCompleted = false,
                CommEstablished = false,
                EmptyBlockCount = 0,
                LocalNode = true,
                InfoMode = true,
                DebugMode = true,
                EncryptMode = false,
                SynchronousSocketIsActive = false,
                PrettyJson = true,
                GenesisAssigned = false,
                DevelopmentNode = false,

                WaitTickCount = 4,

                EncryptKey = "key-password-string",

                HashSalt = Notus.Encryption.Toolbox.GenerateSalt(),


                Layer = NVE.NetworkLayer.Layer1,
                Network = NVE.NetworkType.MainNet,
                NodeType = NVE.NetworkNodeType.Suitable,
                MsgOrch = new Notus.Message.Orchestra(),
                Nodes = new NVS.NodeQueueList()
                {
                    My = new Struct.NodeQueueInfo()
                    {
                        PublicKey = Notus.Wallet.ID.Generate(SessionPrivateKey),
                        Begin = 0,
                        Tick = 0,
                        SyncNo = 0,
                        JoinTime = 0,
                        HexKey = "",
                        IP = new NVS.NodeInfo()
                        {
                            IpAddress = "",
                            Port = 0,
                            Wallet = ""
                        },
                        Ready = false,
                        Status = NVS.NodeStatus.Unknown,
                    },
                    Lists = new List<NVS.IpInfo>() { },
                    Queue = new Dictionary<ulong, NVS.NodeInfo> { },
                    //Listener = new ConcurrentDictionary<string, Notus.Communication.Sync.Socket.Server> { }
                },
                NodeWallet = new NVS.EccKeyPair()
                {
                    CurveName = "",
                    PrivateKey = "",
                    PublicKey = "",
                    WalletKey = "",
                    Words = new string[] { },
                },

                Port = new NVS.CommunicationPorts()
                {
                    MainNet = 0,
                    TestNet = 0,
                    DevNet = 0
                },
                BlockOrder = new Dictionary<ulong, string>() { }
            };
        }

        public static class Functions
        {
            public static Thread? UdpListenThread { get; set; }
            public static Notus.Communication.UDP? JoinObj { get; set; }
            public static ConcurrentDictionary<string, string> LockWalletList { get; set; }
            
            /*
                  blok oluşturma sırası gelince, kullanımda olan cüzdan listesi alınacak
                  burada bulunan cüzdan listesi imzalanarak diğer node'lara iletilecek
                  böylece eğer oluşturulan blok içeriği iletilene kadar 

                  */
            /*
            bu dictionary'ye işlenen bloğun içinde işlem yapan cüzdan adreslerinin listesi yazılacak
            bu liste <block> bilgisi ile sıradaki node'a iletilecek
            böylece node sıradaki bloğu hazırlarken kendisinden önce hazırlanan bloktan haberdar olacak


            her node açılışta rastgele bir private key oluşturacak ve diğer validator'e ait özel bir metni imzalayacak
            validator mesajını diğer node'a iletirken bu private key ile ilatecek 
            eğer gönderdiği mesajda hata varsa hatalı mesaj diğer node'lara da gönderilerek onlarında bu olaya şahitlik etmesi sağlanacak

            bu şahitlik ile birlitke ortak ir oy kullanılacak ve validator'e süre, sıra veya stake üzerinden ceza uygulanacak
            */
            public static ConcurrentDictionary<string, byte> WalletUsageList { get; set; }
            public static ConcurrentDictionary<long, string> BlockOrder { get; set; }
            //public static ConcurrentDictionary<ulong, string> BlockCreatorList { get; set; }
            //public static Notus.Mempool BlockOrder { get; set; }
            public static Notus.Block.Storage Storage { get; set; }
            public static Notus.Wallet.Balance Balance { get; set; }
            public static Notus.TGZArchiver Archiver { get; set; }
            public static Notus.Block.Queue BlockQueue { get; set; }
            public static string SendMessage(string receiverIpAddress, int receiverPortNo, string messageText, string nodeHexStr = "")
            {
                if (nodeHexStr == "")
                {
                    nodeHexStr = Notus.Toolbox.Network.IpAndPortToHex(receiverIpAddress, receiverPortNo);
                }
                string urlPath =
                    Notus.Network.Node.MakeHttpListenerPath(receiverIpAddress, receiverPortNo) +
                    "queue/node/" + nodeHexStr;
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
                    NodeList[nodeHexStr].Status = NVS.NodeStatus.Online;
                    return incodeResponse;
                }
                return string.Empty;
            }
            public static void SendKillMessage()
            {
                NP.Info(Settings, "Sending Kill Signals To All Nodes");
                // nodeların kapanma işlemi şu sıra ile olacak
                /*
                node öncelikle diğer tüm ağlara kapanmak istediğini "kill" mesajı ile bildirecek.
                mesajı alan nodelar, mesajı gönderen node'a kapanmak isteyip istemediğini soracak
                eğer geri gelen cevap kapanmak istediğine dair bir mesaj ise
                diğer nodelar kendi listelerinden node'u çıkartacak

                burada her node açılışta rastgele bir private key oluşturacak ve onu gönderecek
                node'lar o adresi kaydedecek ve kapanma sinyali gönderildiğinde 
                kontrol ederek node'u listeden offline moduna alacak
                */
                // diğer nodelara kapandığımızı bildiriyoruz...
                SendMessageToTimeServer("k", 10);

                ulong nowUtcValue = NVG.NOW.Int;
                string controlSignForKillMsg = Notus.Wallet.ID.Sign(
                    nowUtcValue.ToString() +
                        NVC.CommonDelimeterChar +
                    NVG.Settings.Nodes.My.IP.Wallet,
                    SessionPrivateKey
                );

                foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NodeList)
                {
                    if (string.Equals(entry.Key, Settings.Nodes.My.HexKey) == false)
                    {
                        if (entry.Value.Status == NVS.NodeStatus.Online)
                        {
                            NP.Warning(NVG.Settings, "Sending Kill Message To -> " + entry.Value.IP.Wallet);
                            SendMessage(entry.Value.IP.IpAddress,
                                entry.Value.IP.Port,
                                "<kill>" +
                                    Settings.Nodes.My.IP.Wallet +
                                    NVC.CommonDelimeterChar +
                                    nowUtcValue.ToString() +
                                    NVC.CommonDelimeterChar +
                                    controlSignForKillMsg +
                                "</kill>",
                                entry.Key
                            );
                        }
                    }
                }
                Settings.ClosingCompleted = true;
            }
            public static string GenerateTxUid()
            {
                string seedStr = "node-wallet-key";
                if (Settings != null)
                {
                    if (Settings.NodeWallet != null)
                    {
                        seedStr = Settings.NodeWallet.WalletKey;
                    }
                }
                return Notus.Block.Key.Generate(ND.NowObj(), seedStr);
            }
            public static void Dispose()
            {
                Storage.Dispose();
                BlockQueue.Dispose();
                //Archiver.
                Balance.Dispose();
            }
            public static void PreStart()
            {
                NOW = new TimeStruct();
                NOW.Obj = DateTime.UtcNow;
                NOW.Int = ND.ToLong(NOW.Obj);
                NOW.Diff = new TimeSpan(0);
                NOW.DiffUpdated = false;
                NOW.LastDiffUpdate = DateTime.UtcNow;
            }
            public static void Start()
            {
                WalletUsageList = new ConcurrentDictionary<string, byte>();
                LockWalletList = new ConcurrentDictionary<string, string>();
                BlockOrder = new ConcurrentDictionary<long, string>();
                Storage = new Notus.Block.Storage();
                BlockQueue = new Notus.Block.Queue();
                Archiver = new Notus.TGZArchiver();
                Balance = new Notus.Wallet.Balance();
                if (Settings.GenesisCreated == false)
                {
                    Balance.Start();
                }

                Globals.NodeListPrinted = false;
                //Globals.MsgSocketList = new ConcurrentDictionary<string, Notus.Communication.Sync.Socket.Server>();
                Globals.NodeList = new ConcurrentDictionary<string, NVS.NodeQueueInfo>();
                Globals.NodeQueue = new Notus.Globals.Variable.NodeQueueList();

                Globals.NodeQueue.Begin = false;
                Globals.NodeQueue.OrderCount = 1;

                Globals.NodeQueue.NodeOrder = new Dictionary<int, string>();
                Globals.NodeQueue.TimeBaseWalletList = new Dictionary<ulong, string>();


                BlockOrder.Clear();
                /*
                string tmpFolderName = Notus.IO.GetFolderName(
                    Settings,
                    NVC.StorageFolderName.Common
                );
                BlockOrder = new Notus.Mempool(tmpFolderName +"block_order_list");
                BlockOrder.AsyncActive = false;
                BlockOrder.Clear();
                */
            }

            public static void CloseMessageSockets(string walletId = "")
            {
                //socket-exception
                /*
                yeni oluşturulan soket kitaplığı düzenlenecek ve bu fonksiyon ile kapatılacak
                buradaki amaç sırası gelmeden soket bağlantısını açarak gerektiğinde 
                hızlı bir biçimde veri gönderimini mümkün hale getirmek
                */

                /*
                foreach (KeyValuePair<string, Notus.Communication.Listener> entry in MsgSocketList)
                {
                    bool closeConnection = false;
                    if (walletId.Length == 0)
                    {
                        closeConnection = true;
                    }
                    else
                    {
                        if (string.Equals(entry.Key, walletId))
                        {
                            closeConnection = true;
                        }
                    }
                    if (closeConnection == true)
                    {
                        if (MsgSocketList[entry.Key] != null)
                        {
                            MsgSocketList[entry.Key].Dispose();
                        }
                    }
                }
                */
            }
            public static void CloseMyNode()
            {
                NP.Warning(NVG.Settings, "Please Wait While Node Terminating");
                if (NVG.Settings.CommEstablished == true)
                {
                    SendKillMessage();
                    while (NVG.Settings.ClosingCompleted == false)
                    {
                        Thread.Sleep(10);
                    }
                }
                /*
                int count = NVG.Settings.Nodes.Queue.Count - 20;
                int sayac = 0;
                foreach (var entry in NVG.Settings.Nodes.Queue)
                {
                    sayac++;
                    if (sayac > count)
                    {
                        Console.WriteLine(entry.Key + " -> " + JsonSerializer.Serialize(entry.Value));
                    }
                }
                */
                Environment.Exit(0);
            }
            public static void GetUtcTimeFromNode(int howManySeconds, bool beginingRoutine)
            {
                if (beginingRoutine == true)
                {
                    NP.Info("Waiting For Time Sync");
                }
                DateTime timerExecutedTime = DateTime.Now.Subtract(new TimeSpan(1, 0, 0));
                while (NOW.DiffUpdated == false)
                {
                    TimeSpan ts = DateTime.Now - timerExecutedTime;
                    if (ts.TotalSeconds > howManySeconds)
                    {
                        KillTimeSync(beginingRoutine);
                        Thread.Sleep(150);
                        StartTimeSync();
                        timerExecutedTime = DateTime.Now;
                    }
                }
                if (beginingRoutine == true)
                {
                    NP.Success("Time Sync Is Done");
                }
            }
            public static void KillTimeSync(bool beginingRoutine)
            {
                if (beginingRoutine == false)
                {
                    NP.Info("Sending Killing Message");
                }
                SendMessageToTimeServer("k", 5);
                if (JoinObj != null)
                {
                    JoinObj.CloseOnlyListen();
                    Thread.Sleep(2000);
                    JoinObj = null;
                }
                UdpListenThread = null;
            }
            public static void SendMessageToTimeServer(string cmdName, int howManyMsg)
            {
                using (UdpClient udpClient = new UdpClient())
                {
                    for (int i = 0; i < howManyMsg; i++)
                    {
                        try
                        {
                            string cmdText = cmdName + ":" +
                                Settings.Nodes.My.IP.Wallet + ":" +
                                Settings.Nodes.My.IP.IpAddress;
                            udpClient.Connect(NVC.TimeSyncNodeIpAddress, NVC.TimeSyncAddingCommPort);
                            byte[] sendBytes = Encoding.ASCII.GetBytes(cmdText);
                            udpClient.Send(sendBytes, sendBytes.Length);
                        }
                        catch { }
                        Thread.Sleep(20);
                    }
                    udpClient.Close();
                }
            }
            public static void StartTimeSync()
            {
                UdpListenThread = new Thread(new ThreadStart(ThreadNodeDinleme));
                UdpListenThread.Start();
                Thread.Sleep(100);
                SendMessageToTimeServer("a", 3);
            }

            public static ulong NowInt()
            {
                UpdateTime();
                return NVG.NOW.Int;
            }
            public static void UpdateTime()
            {
                NVG.NOW.Obj = DateTime.UtcNow.Add(NVG.NOW.Diff);
                NVG.NOW.Int = ND.ToLong(NVG.NOW.Obj);
            }

            //burada merkezi node'dan zaman bilgisini alıyor
            public static void ThreadNodeDinleme()
            {
                JoinObj = new Notus.Communication.UDP();
                JoinObj.OnlyListen(NVC.TimeSyncCommPort, (dataArriveTimeObj, incomeString, remoteEp) =>
                {
                    string[] incomeArr = incomeString.Split(':');
                    if (ulong.TryParse(incomeArr[0], out ulong ntpServerTimeLong))
                    {
                        int transferSpeed = int.Parse(incomeArr[1]);
                        if (transferSpeed == 0)
                        {
                            DateTime rightNow = DateTime.UtcNow;
                            if (NOW.LastDiffUpdate!= rightNow)
                            {
                                NOW.Diff = DateTime.ParseExact(incomeArr[0], "yyyyMMddHHmmssfffff", CultureInfo.InvariantCulture) - dataArriveTimeObj;
                                NOW.DiffUpdated = true;
                                NOW.LastDiffUpdate = rightNow;
                                Thread.Sleep(1);
                            }
                        }
                    }
                });
            }
            static Functions()
            {
            }
        }
    }
}
