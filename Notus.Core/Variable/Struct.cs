using System;
using System.Collections.Generic;
using System.Numerics;

namespace Notus.Variable.Struct
{

    public class FileTransferStruct
    {
        public int BlockType { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string FileHash { get; set; }
        public int ChunkSize { get; set; }
        public int ChunkCount { get; set; }
        public Notus.Variable.Enum.ProtectionLevel Level { get; set; }
        public bool StoreEncrypted { get; set; }
        public bool WaterMarkIsLight { get; set; }
        public string PublicKey { get; set; }
        public string Sign { get; set; }
    }
    public class FileChunkStruct
    {
        public string UID { get; set; }
        public int Count { get; set; }
        public string Data { get; set; }
    }



    // currency list struct
    public class CurrencyList
    {
        public bool ReserveCurrency { get; set; }   // token generation version
        public string Tag { get; set; }             // short name
        public string Name { get; set; }            // creator info
        public FileStorageStruct Logo { get; set; } // token info
    }
    // token generation struct
    public class BlockStruct_160
    {
        public int Version { get; set; }               // token generation version
        public CreationStruct Creation { get; set; }   // creator info
        public TokenInfoStruct Info { get; set; }      // token info
        public SupplyStruct Reserve { get; set; }      // how much token
        public Notus.Variable.Class.WalletBalanceStructForTransaction Balance { get; set; }        // token owner current balance
        public Dictionary<string, Dictionary<ulong, string>> Out { get; set; }  // after 
        public ValidatorStruct Validator { get; set; }           // who validate this block
    }
    public class CreationStruct
    {
        public string UID { get; set; }     // token UID verisi
        public string PublicKey { get; set; }     // creator public key
        public string Sign { get; set; }  // struct sign
    }
    public class GenericSignStruct
    {
        public string Time { get; set; }     // time ID
        public string PublicKey { get; set; }     // signer public key
        public string Sign { get; set; }  // sign
    }
    public class FileStorageStruct
    {
        public bool Used { get; set; }          // if struct is been using
        public string Base64 { get; set; }      // if file storage on chain, its own image data
        public string Url { get; set; }         // file url
        public string Source { get; set; }      // file storage service name
    }
    public class StorageOnChainStruct
    {
        public string Name { get; set; }          // file name
        public long Size { get; set; }             // file size
        public string Hash { get; set; }          // file hash
        public bool Encrypted { get; set; }       // file url
        public string PublicKey { get; set; }      // file owner public key
        public string Sign { get; set; }          // sign data
        public BalanceAfterBlockStruct Balance { get; set; }  // new balance
    }
    public class TokenInfoStruct
    {
        public string Name { get; set; }                // token long name
        public string Tag { get; set; }                 // token short tag
        public FileStorageStruct Logo { get; set; }     // token logo

    }
    public class SupplyStruct
    {
        public Int64 Supply { get; set; }       // integer value
        public int Decimal { get; set; }        // decimal value
        public bool Resupplyable { get; set; }  // token can be editable
    }

    // block struct for type 142 -> increase in token supply block strcut
    public class BlockStruct_142
    {
        public string TokenUid { get; set; }                // token generation uid
        public Int64 Supply { get; set; }                   // new integer value
        public int Decimal { get; set; }                    // new decimal value
        public bool DistributeEqually { get; set; }         // when resupply token distbute equally to token holders ( protect token holders share )
        public bool Resupplyable { get; set; }              // token can be editable after that
    }

    public class EccKeyPair
    {
        public string CurveName { get; set; }
        public string[] Words { get; set; }
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
        public string WalletKey { get; set; }
    }
    public class BlockResponse
    {
        public string UID { get; set; }
        public Notus.Variable.Enum.BlockStatusCode Result { get; set; }
        public string Status { get; set; }
    }
    public class BlockResponseStruct
    {
        public string UID { get; set; }
        public int Code { get; set; }
        public string Status { get; set; }
    }
    public class WalletBalanceResponseStruct
    {
        public int ErrorNo { get; set; }
        public bool Exist { get; set; }
        public Int64 RowNo { get; set; }
        public string Source { get; set; }
        public string Balance { get; set; }
    }
    public class CryptoTransactionResult
    {
        public int ErrorNo { get; set; }
        public Notus.Variable.Enum.BlockStatusCode Result { get; set; }
        public string ID { get; set; }
    }

    public class WalletBalanceStruct
    {
        public string Wallet { get; set; }         // account wallet id
        public Dictionary<string, Dictionary<ulong, string>> Balance { get; set; }      // account current balance
                                                                                        //public string Currency { get; set; }       // account curreny
        public Int64 RowNo { get; set; }           // witness row no
        public string UID { get; set; }            // witness uid
    }
    public class BalanceAfterBlockStruct
    {
        public string Wallet { get; set; }         // account wallet id
        public Dictionary<string, string> Balance { get; set; }      // account current balance
        public Int64 RowNo { get; set; }           // witness row no
        public string UID { get; set; }            // witness uid
        public string Fee { get; set; }             // witness row no
    }

    public class WalletActivitiesStruct
    {
        public Int64 Order { get; set; }        // activities order no
        public int Type { get; set; }           // block type
        public string UID { get; set; }         // block uid
    }
    public class CryptoTransactionStruct
    {
        public int ErrorNo { get; set; }
        public ulong UnlockTime { get; set; }
        public string Currency { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Volume { get; set; }
        public string PublicKey { get; set; }
        public string Sign { get; set; }
        public string CurveName { get; set; }
        public Notus.Variable.Enum.NetworkType Network { get; set; }
    }

    public class CryptoTransactionDataStruct
    {
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Volume { get; set; }
        public string PublicKey { get; set; }
        public string Sign { get; set; }
        public string CurveName { get; set; }
    }

    public class CryptoTransactionBeforeStruct
    {
        public ulong UnlockTime { get; set; }
        public string Currency { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Volume { get; set; }
        public string PrivateKey { get; set; }
        public string CurveName { get; set; }
        public Notus.Variable.Enum.NetworkType Network { get; set; }
    }

    //control-point
    public class ValidatorStruct
    {
        public string Reward { get; set; }
        public string NodeWallet { get; set; }
    }

    public class CryptoTransactionStoreStruct
    {
        public ulong UnlockTime { get; set; }
        public int Version { get; set; }
        public string TransferId { get; set; }              // transfer control id
        public string Currency { get; set; }
        //public Notus.Variable.Struct.WalletBalanceStruct Sender { get; set; }      // sender wallet id
        public string Sender { get; set; }      // sender wallet id
        public string Receiver { get; set; }                                     // receiver wallet id
        public string Fee { get; set; }                                          // transfer fee
        public string Volume { get; set; }        // transfer volume
        public string PublicKey { get; set; }
        public string Sign { get; set; }
    }
    public class RSAKeysModel
    {
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
    }

    public class LastBlockInfo
    {
        public Int64 RowNo { get; set; }
        public string Sign { get; set; }
        public string uID { get; set; }
    }

    public class HttpRequestDetails
    {
        public bool KeepAlive { get; set; }
        public bool IsSecureConnection { get; set; }
        public bool IsAuthenticated { get; set; }
        public string HttpMethod { get; set; }
        public string ProtocolVersion { get; set; }
        public string UserAgent { get; set; }
        public string UserHostName { get; set; }
        
        public string LocalEP { get; set; }
        public string LocalIP { get; set; }
        public int LocalPort { get; set; }

        public string RemoteEP { get; set; }
        public string RemoteIP { get; set; }
        public int RemotePort { get; set; }

        public string RawUrl { get; set; }
        public string Url { get; set; }
        public string[] UrlList { get; set; }
        public Dictionary<string, string> GetParams { get; set; }
        public Dictionary<string, string> PostParams { get; set; }
    }

    public class StorageHash_BlockStatus
    {
        public List<string> List { get; set; }
        public string Total { get; set; }
    }
    public class Common_BlockStatus
    {
        public Int64 BlockHeight { get; set; }
        public string PreviousId { get; set; }
        public Dictionary<int, string> PreviousList { get; set; }
    }
    public class BlockStatus
    {
        public bool Equal { get; set; }
        public Common_BlockStatus DB { get; set; }
        public Common_BlockStatus Storage { get; set; }
        public StorageHash_BlockStatus Hash { get; set; }
    }

    public class TmpNodeListStruct
    {
        public string IpAddress { get; set; }
        public string PortNo { get; set; }
        public string WalletKey { get; set; }
    }
    public class NodeListStruct
    {
        public string key { get; set; }
        public string ip { get; set; }
        public int port { get; set; }
        public int countdown { get; set; }
        public int order { get; set; }
        public DateTime added { get; set; }
        public DateTime updated { get; set; }
    }
    public class NodeListResponseStruct
    {
        public string key { get; set; }
        public string ip { get; set; }
        public int port { get; set; }
        public int countdown { get; set; }
        public int order { get; set; }
    }
    public class TmpMinerPingListStruct
    {
        public string IpAddress { get; set; }
        public string PortNo { get; set; }
        public string PublicKey { get; set; }
        public string Sign { get; set; }
    }
    public class MinerPingListStruct
    {
        public string IpAddress { get; set; }
        public int PortNo { get; set; }
        public string PublicKey { get; set; }
        public string Sign { get; set; }
    }

    public class PoolResponseStruct
    {
        public int code { get; set; }
        public string data { get; set; }
        public string key { get; set; }
    }

    public class PeerResponseStruct
    {
        public int code { get; set; }
        public int count { get; set; }
        public string hash { get; set; }
    }

    public class PoolBlockRecordStruct
    {
        public int type { get; set; }
        public string data { get; set; }
    }

    public class List_PoolBlockRecordStruct
    {
        public string key { get; set; }
        public int type { get; set; }
        public string data { get; set; }
    }


    // added for temporary 
    public class SocketTransferDataStruct
    {
        public int type { get; set; }
        public string data { get; set; }
    }

    public class NodeIpInfo
    {
        public string Local { get; set; }
        public string Public { get; set; }
    }
    public class CommunicationPorts
    {
        public int MainNet { get; set; }
        public int TestNet { get; set; }
        public int DevNet { get; set; }
    }
    public class LayerInfo
    {
        public bool Active { get; set; }
        public CommunicationPorts Port { get; set; }
    }
    public class NodeWalletInfo
    {
        public bool FullDefined { get; set; }
        public bool Defined { get; set; }
        public string Key { get; set; }
        public string PublicKey { get; set; }
        public string Sign { get; set; }
    }
    public class NodeInfo
    {
        public NodeWalletInfo Wallet { get; set; }
        public Dictionary<Notus.Variable.Enum.NetworkLayer, LayerInfo> Layer { get; set; }
        public bool DebugMode { get; set; }
        public bool InfoMode { get; set; }
        public bool LocalMode { get; set; }
        public bool DevelopmentMode { get; set; }
    }

    public class ConnectionDetailStruct
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public string Time { get; set; }
        public bool Online { get; set; }
    }

    public class MasterWalletInfo
    {
        public bool FullDefined { get; set; }
        public bool Defined { get; set; }
        public string Key { get; set; }
        public string PublicKey { get; set; }
        public string Sign { get; set; }
    }

    public class MasterDetailStruct
    {
        public MasterWalletInfo Wallet { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }
        public string Time { get; set; }
    }



    public class MempoolDataList
    {
        public string Data { get; set; }
        public int expire { get; set; }
        public DateTime added { get; set; }
        public DateTime remove { get; set; }
    }

    public class MetricsResponseStruct
    {
        public UInt64 Count { get; set; }
    }

    public class CryptoTransferStatus
    {
        public Notus.Variable.Enum.BlockStatusCode Code { get; set; }
        public Int64 RowNo { get; set; }
        public string UID { get; set; }
        public string Text { get; set; }
    }

    public class QueryResponseStruct
    {
        public int Code { get; set; }
        public string Status { get; set; }
    }
    public class FeeCalculationStruct
    {
        public Int64 Fee { get; set; }
        public bool Error { get; set; }
    }
    public class CurrencyListStorageStruct
    {
        public CurrencyList Detail{ get; set; }   // token generation version
        public string Uid { get; set; }             // short name
    }

}
