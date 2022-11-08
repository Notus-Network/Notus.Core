using System;
using System.Collections.Generic;

namespace Notus.Globals.Variable
{
    public class Object
    {
        public Notus.TGZArchiver? Archive { get; set; }
        public Notus.Wallet.Balance? Balance { get; set; }

    }

    public class Settings
    {
        public bool WaitForGeneratedBlock { get; set; }
        public bool NodeClosing { get; set; }
        public bool ClosingCompleted { get; set; }
        public bool CommEstablished { get; set; }

        public bool LocalNode { get; set; }
        public bool DevelopmentNode { get; set; }
        //public ulong NodeStartingTime { get; set; }
        //public Notus.Variable.Struct.UTCTimeStruct? UTCTime { get; set; }
        public bool GenesisCreated { get; set; }
        public bool GenesisAssigned { get; set; }
        public Notus.Message.Orchestra? MsgOrch { get; set; }
        public Notus.Variable.Genesis.GenesisBlockData? Genesis { get; set; }
        public Notus.Variable.Struct.NodeQueueList? Nodes { get; set; }

        public Notus.Variable.Struct.NodeIpInfo? IpInfo { get; set; }
        public Notus.Variable.Struct.EccKeyPair? NodeWallet { get; set; }
        public Notus.Variable.Enum.NetworkNodeType NodeType { get; set; }
        public Notus.Variable.Enum.NetworkType Network { get; set; }
        public Notus.Variable.Enum.NetworkLayer Layer { get; set; }
        public Notus.Variable.Struct.CommunicationPorts? Port { get; set; }
        public Dictionary<ulong, string> BlockOrder { get; set; }

        //public ulong PacketSend { get; set; }
        //public ulong PacketReceive { get; set; }
        public bool DebugMode { get; set; }
        public bool InfoMode { get; set; }
        public bool PrettyJson { get; set; }
        public int WaitTickCount { get; set; }
        public bool SynchronousSocketIsActive { get; set; }

        public bool EncryptMode { get; set; }
        public string EncryptKey { get; set; }
        public string HashSalt { get; set; }

        public int OtherBlockCount { get; set; }
        public int EmptyBlockCount { get; set; }
        public Notus.Variable.Class.BlockData? LastBlock { get; set; }
    }

    public class TimeStruct
    {
        public DateTime LastDiffUpdate { get; set; }
        public bool DiffUpdated { get; set; }
        public TimeSpan Diff { get; set; }
        public DateTime Obj { get; set; }
        public ulong Int { get; set; }
    }
    public class NodeQueueList
    {
        // işlem sırası
        public int OrderCount { get; set; }
        //true ise senkron bşalmıştır
        public bool Begin { get; set; }
        //nodeların senkron sonrası başlangıç zamanı - değiştirilmeyecek
        public ulong Starting { get; set; }
        //ntp hesaplaması ile oluşturulan şu an
        public ulong Now { get; set; }
        // node zaman sıralaması
        public Dictionary<ulong, string>? TimeBaseWalletList { get; set; }
        // node işlem sıralaması
        public Dictionary<int, string>? NodeOrder { get; set; }
    }

    /*
    public class NodeOrderStruct
    {
        public string Wallet { get; set; }
        public DateTime Begin { get; set; }
    }
    */
}
