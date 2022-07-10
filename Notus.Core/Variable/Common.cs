using System;
using System.Collections.Generic;

namespace Notus.Variable.Common
{
    public class ClassSetting
    {
        public bool LocalNode { get; set; }
        public bool DevelopmentNode { get; set; }

        public bool GenesisCreated { get; set; }
        public bool GenesisAssigned { get; set; }
        public Notus.Variable.Genesis.GenesisBlockData Genesis { get; set; }

        public Notus.Variable.Struct.NodeIpInfo IpInfo { get; set; }
        public Notus.Variable.Struct.EccKeyPair NodeWallet { get; set; }
        public Notus.Variable.Enum.NetworkNodeType NodeType { get; set; }
        public Notus.Variable.Enum.NetworkType Network { get; set; }
        public Notus.Variable.Enum.NetworkLayer Layer { get; set; }
        public Notus.Variable.Struct.CommunicationPorts Port { get; set; }
    
        public bool DebugMode { get; set; }
        public bool InfoMode { get; set; }
        public bool PrettyJson { get; set; }
        public int WaitTickCount { get; set; }
        public bool SynchronousSocketIsActive { get; set; }

        public bool EncryptMode { get; set; }
        public string EncryptKey { get; set; }
        public string HashSalt { get; set; }

        public Notus.Variable.Class.BlockData LastBlock { get; set; }
    }
}
