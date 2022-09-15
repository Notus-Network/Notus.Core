using System;
using System.Collections.Generic;
using System.Numerics;

namespace Notus.Variable.Class
{
    public static class Block
    {
        public static BlockData GetEmpty()
        {
            return new BlockData()
            {
                info = new InfoType()
                {
                    version = 0,
                    type = 0,
                    uID = "",
                    time = "",
                    multi = false,
                    rowNo = 0,
                    nonce = new InfoNonceType()
                    {
                        method = 0,
                        type = 0,
                        difficulty = 0
                    },
                    node = new NodeType()
                    {
                        id = "",
                        master = false,
                        replicant = false,
                        broadcaster = false,
                        validator = false,
                        executor = false,
                        keeper = new NodeKeeperType()
                        {
                            key = false,
                            block = false,
                            file = false,
                            tor = false
                        }
                    },
                    prevList = new Dictionary<int, string>()
                },
                cipher = new CipherType()
                {
                    ver = "",
                    data = "",
                    sign = ""
                },
                hash = new HashType()
                {
                    block = "",
                    data = "",
                    info = "",
                    FINAL = ""
                },
                validator = new ValidatorMainType()
                {
                    count = new Dictionary<string, int>(),
                    map = new MapValidatorType()
                    {
                        block = new Dictionary<int, string>(),
                        data = new Dictionary<int, string>(),
                        info = new Dictionary<int, string>()
                    },
                    sign = ""
                },
                nonce = new NonceType()
                {
                    block = "",
                    data = "",
                    info = ""
                },
                prev = "",
                sign = ""
            };
        }
    }

    public class BlockData
    {
        public InfoType info { get; set; }
        public CipherType cipher { get; set; }
        public HashType hash { get; set; }
        public ValidatorMainType validator { get; set; }
        public NonceType nonce { get; set; }
        public string prev { get; set; }
        public string sign { get; set; }
        public BlockData Clone()
        {
            return (BlockData)this.MemberwiseClone();
        }
    }
    public class NonceType
    {
        public string block { get; set; }
        public string data { get; set; }
        public string info { get; set; }
    }
    public class MapValidatorType
    {
        public Dictionary<int, string> block { get; set; }
        public Dictionary<int, string> data { get; set; }
        public Dictionary<int, string> info { get; set; }
    }
    public class ValidatorMainType
    {
        public Dictionary<string, int> count { get; set; }
        public MapValidatorType map { get; set; }
        public string sign { get; set; }
    }
    public class HashType
    {
        public string block { get; set; }
        public string data { get; set; }
        public string info { get; set; }
        public string FINAL { get; set; }
    }
    public class CipherType
    {
        public string ver { get; set; }
        public string data { get; set; }
        public string sign { get; set; }
    }
    public class InfoType
    {
        public int version { get; set; }
        public int type { get; set; }
        public string uID { get; set; }
        public string time { get; set; }
        public bool multi { get; set; }
        public Int64 rowNo { get; set; }
        public InfoNonceType nonce { get; set; }
        public NodeType node { get; set; }
        public Dictionary<int, string> prevList { get; set; } // önceki blokların türlerine göre burada değerler yazılacak..
    }
    public class InfoNonceType
    {
        public int method { get; set; }
        public int type { get; set; }
        public int difficulty { get; set; }
    }
    public class NodeType
    {
        public string id { get; set; }
        public bool master { get; set; }            // kaynak kodda tanımlı olan adresler
        public bool replicant { get; set; }         // sonradan dahil olan adresler
        public bool broadcaster { get; set; }       // yayın yapanlar
        public bool validator { get; set; }             // sadece mining işlemi yapanlar
        public bool executor { get; set; }          // kontratları çalıştıranlar
        public NodeKeeperType keeper { get; set; }  // dosyaları tutanlar
    }
    public class NodeKeeperType
    {
        public bool key { get; set; }
        public bool block { get; set; }
        public bool file { get; set; }
        public bool tor { get; set; }
    }
    public class ValidatorNonceListe
    {
        public Dictionary<int, string> Block { get; set; }
        public Dictionary<int, string> Data { get; set; }
        public Dictionary<int, string> Info { get; set; }
    }
    public class ValidatorNonceValueListe
    {
        public Dictionary<int, int> Block { get; set; }
        public Dictionary<int, int> Data { get; set; }
        public Dictionary<int, int> Info { get; set; }
    }


    // this class for validator
    public class ValidatorDetailsForPool
    {
        //public IPAddress ip { get; set; }
        public string ip { get; set; }
        public int port { get; set; }
        public int recordType { get; set; }
        public string walletId { get; set; }
        public string publicKey { get; set; }
        public string sign { get; set; }
        public DateTime connectTime { get; set; }

        // public key değerinin sasha sign değeri burada olacak ve en büyükten en küçükğe doğru sıra ile işlem yapacaklar
        public string order { get; set; }

        //soket bağlantısı tamamlanınca true olacak
        public bool connected { get; set; }
        //public Notus.Kernel.Communication.Sender netSoc { get; set; }
        //public System.Net.Sockets.Socket netSoc { get; set; }
    }

    // this class for validator
    public class ValidatorListForPool
    {
        //public IPAddress ip { get; set; }
        public string ip { get; set; }
        public int port { get; set; }
        public string wallet { get; set; }
        public DateTime tick { get; set; }

        // public key değerinin sasha sign değeri burada olacak ve en büyükten en küçükğe doğru sıra ile işlem yapacaklar
        public BigInteger order { get; set; }

        //soket bağlantısı tamamlanınca true olacak
        //public System.Net.Sockets.Socket netSoc { get; set; }
        //public Notus.Kernel.Communication.Sender netSoc { get; set; }
        public bool connected { get; set; }
    }


    /*
    // 
    public class NodePingScoreList
    {
        //public string wallet { get; set; }
        public string ip { get; set; }
        public int port { get; set; }
        public Notus.Kernel.Communication.Sender netSoc { get; set; }
        public DateTime send { get; set; }
        public TimeSpan last { get; set; }
        public bool connected { get; set; }
        public bool error { get; set; }
    }
    */


    // 
    public class PingScoreStruct
    {
        public Notus.Variable.Enum.PingServerType type { get; set; }
        public string ip { get; set; }
        public int port { get; set; }
        public DateTime send { get; set; }
        public TimeSpan last { get; set; }
        public bool error { get; set; }      // if its error
        public int count { get; set; }      // error count
    }



    public class TransactionSendStruct
    {
        public string sender { get; set; }
        public string receiver { get; set; }
        public string publicKey { get; set; }
        public string sign { get; set; }
        public BigInteger volume { get; set; }
        public BigInteger fee { get; set; }
        public DateTime time { get; set; }
    }

    public class TransactionPoolStruct
    {
        public string poolKey { get; set; }
        public string sender { get; set; }
        public string receiver { get; set; }
        public string publicKey { get; set; }
        public string sign { get; set; }
        public BigInteger volume { get; set; }
        public BigInteger fee { get; set; }
        public DateTime time { get; set; }
    }
    public class TempTransactionPoolStruct
    {
        public string poolKey { get; set; }
        public string sender { get; set; }
        public string receiver { get; set; }
        public string publicKey { get; set; }
        public string sign { get; set; }
        public string volume { get; set; }
        public string fee { get; set; }
        public string time { get; set; }
    }

    public class PreTransactionSendStruct
    {
        public string sender { get; set; }
        public string receiver { get; set; }
        public string publicKey { get; set; }
        public string sign { get; set; }
        public string volume { get; set; }
        public string fee { get; set; }
        public string time { get; set; }
    }


    // block struct for type 120
    public class WalletBalanceStructForTransaction
    {
        public string Wallet { get; set; }
        public Dictionary<string, Dictionary<ulong, string>> Balance { get; set; }
        public long WitnessRowNo { get; set; }
        public string WitnessBlockUid { get; set; }
    }
    public class BlockStruct_120
    {
        public Dictionary<string, BlockStruct_120_In_Struct> In { get; set; }
        public Dictionary<string, Dictionary<string, Dictionary<ulong, string>>> Out { get; set; }
        public Notus.Variable.Struct.ValidatorStruct Validator { get; set; }
    }
    public class BlockStruct_120_In_Struct
    {
        public WalletBalanceStructForTransaction Sender { get; set; }             // sender wallet id
        public WalletBalanceStructForTransaction Receiver { get; set; }           // recevier wallet id
        public ulong CurrentTime { get; set; }          // how much coin
        public string Currency { get; set; }          // how much coin
        public string Volume { get; set; }          // how much coin
        public string Fee { get; set; }             // transfer fee
        public string PublicKey { get; set; }       // control public key
        public string Sign { get; set; }            // control sign data
    }


}
