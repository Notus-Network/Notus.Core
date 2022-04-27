using System;
using System.Collections.Generic;

namespace Notus.Core
{
    public static class Variable
    {
        public static readonly string DefaultHexAlphabetString = "0123456789abcdef";
        public static readonly string DefaultBase32AlphabetString = "QAZ2WSX3EDC4RFV5TGB6YHN7UJM8K9LP";

        public static readonly string DefaultBase35AlphabetString = "123456789abcdefghijklmnopqrstuvwxyz";

        public static readonly string DefaultBase58AlphabetString = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        public static readonly string DefaultBase64AlphabetString = "ABCDEFGHIJKLMNOPQRSTUVWXYZ" + "abcdefghijklmnopqrstuvwxyz" + "0123456789" + "+/";
        public static readonly char[] DefaultBase64AlphabetCharArray = new char[64] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/' };

        public readonly static Dictionary<char, byte> DefaultHexMapCharDictionary = new Dictionary<char, byte>()
        {
        { 'a', 0xA },{ 'b', 0xB },{ 'c', 0xC },{ 'd', 0xD },
        { 'e', 0xE },{ 'f', 0xF },{ 'A', 0xA },{ 'B', 0xB },
        { 'C', 0xC },{ 'D', 0xD },{ 'E', 0xE },{ 'F', 0xF },
        { '0', 0x0 },{ '1', 0x1 },{ '2', 0x2 },{ '3', 0x3 },
        { '4', 0x4 },{ '5', 0x5 },{ '6', 0x6 },{ '7', 0x7 },
        { '8', 0x8 },{ '9', 0x9 }
        };
        public static readonly string CommonDelimeterChar = ":";
        //tüm sunucu işlemlerinden Http server portu olarak bu kullanılacak
        public static readonly int PortNo_BlockPoolListener = 5500;
        public static readonly int PortNo_HttpListener = 5000;
        public static readonly List<string> ListMainNodeIp = new List<string> {
            "94.101.87.42"
        };
        public const string Default_EccCurveName = "prime256v1";
        public const int Default_WordListArrayCount = 16;

        public static readonly int BlockStorageMonthlyGroupCount = 50;
        public static readonly string Prefix_MainNetwork = "NR";
        public static readonly string Prefix_TestNetwork = "NT";

        public enum NetworkType
        {
            Const_MainNetwork = 10,
            Const_TestNetwork = 20
        }

        public enum ShortAlgorithmResultType
        {
            Mix = 2,
            OneByOne = 4
        }
        public class ErrorNoList
        {
            public const int Success = 0;
            public const int AddedToQueue = 1;
            public const int UnknownError = 1;
            public const int NeedCoin = 5;
            public const int AccountDoesntExist = 7;
            public const int WrongSign = 9;
            public const int TagExists = 10;
            public const int WrongAccount = 13;
            public const int MissingArgument = 11;
        }

        // token generation struct
        public class BlockStruct_160
        {
            public int Version { get; set; }               // token generation version
            public CreationStruct Creation { get; set; }   // creator info
            public TokenInfoStruct Info { get; set; }      // token info
            public SupplyStruct Reserve { get; set; }      // how much token
        }
        public class CreationStruct
        {
            public string UID { get; set; }     // token UID verisi
            public string PublicKey { get; set; }     // creator public key
            public string Sign { get; set; }  // struct sign
        }
        public class FileStorageStruct
        {
            public bool Used { get; set; }          // if struct is been using
            public string Base64 { get; set; }      // if file storage on chain, its own image data
            public string Url { get; set; }         // file url
            public string Source { get; set; }      // file storage service name
        }
        public class TokenInfoStruct
        {
            public string Name { get; set; }    // token long name
            public string Tag { get; set; }     // token short tag
            public FileStorageStruct Logo { get; set; }    // token logo

        }
        public class SupplyStruct
        {
            public Int64 Supply { get; set; }   // integer value
            public int Decimal { get; set; }    // decimal value

            public bool Resupplyable { get; set; }  // token can be editable
        }


        // block struct for type 141 -> token transaction block strcut
        public class BlockStruct_141
        {
            public string Tag { get; set; }                             // token short tag
            public List<In_Struct> In { get; set; }
            public Dictionary<string, string> Out { get; set; }
        }

        public class BalanceStruct
        {
            public string Wallet { get; set; }         // account wallet id
            public string Balance { get; set; }        // account current balance
            public Int64 RowNo { get; set; }           // witness row no
            public string UID { get; set; }            // witness uid
        }

        public class In_Struct
        {
            public BalanceStruct Sender { get; set; }             // sender wallet id
            public BalanceStruct Receiver { get; set; }           // recevier wallet id
            public string Volume { get; set; }          // how much coin
            public string Fee { get; set; }             // transfer fee
            public string PublicKey { get; set; }       // control public key
            public string Sign { get; set; }            // control sign data
        }


        // block struct for type 142 -> increase in token supply block strcut
        public class BlockStruct_142
        {
            public string TokenUid { get; set; }                // token generation uid
            public Int64 Supply { get; set; }                   // new integer value
            public int Decimal { get; set; }                    // new decimal value
            public bool DistributeEqually { get; set; }         // Distbute Equally to token holders
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
            public Notus.Core.Variable.CryptoTransactionResultCode Result { get; set; }
            public string ID { get; set; }
        }

        public class WalletBalanceStruct
        {
            public string Wallet { get; set; }         // account wallet id
            public string Balance { get; set; }        // account current balance
            public Int64 RowNo { get; set; }           // witness row no
            public string UID { get; set; }            // witness uid
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
            public string Sender { get; set; }
            public string Receiver { get; set; }
            public string Volume { get; set; }
            public string PublicKey { get; set; }
            public string Sign { get; set; }
            public string CurveName { get; set; }
            public Notus.Core.Variable.NetworkType Network { get; set; }
        }
        public class CryptoTransactionStoreStruct
        {
            public int ErrorNo { get; set; }
            public WalletBalanceStruct Sender { get; set; }      // sender wallet id
            public WalletBalanceStruct Receiver { get; set; }    // receiver wallet id
            public WalletBalanceStruct Validator { get; set; }    // receiver wallet id
                                                                  //public Int64 RowNo { get; set; }
                                                                  //public string Source { get; set; }      // reference block ID
                                                                  //public string Balance { get; set; }     // sender current balance
            public string Fee { get; set; }           // transfer fee
            public string Volume { get; set; }        // transfer volume
            public string PublicKey { get; set; }
            public string Sign { get; set; }
            //public string CurveName { get; set; }
            //public Notus.Variable.Variable.NetworkType Network { get; set; }
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
            public string Sender { get; set; }
            public string Receiver { get; set; }
            public string Volume { get; set; }
            public string PrivateKey { get; set; }
            public string CurveName { get; set; }
            public Notus.Core.Variable.NetworkType Network { get; set; }
        }

        public enum CryptoTransactionResultCode
        {
            AddedToQueue = 0,
            InAlreadyQueue = 1,
            WrongPublicKey = 10,
            WrongSignature = 2,
            WrongVolume = 5,
            WrongWallet_Sender = 6,
            WrongWallet_Receiver = 9,
            WalletDoesntExist = 11,
            InsufficientBalance = 12,
            PendingPreviousTransaction = 13,
            AnErrorOccurred = 3
        }

        public enum CryptoTransactionStatusCode
        {
            InQueue = 1,
            Completed = 2,
            Rejected = 3,
            Unknown = 4
        }
        public enum WalletTypeCode
        {
            Sender = 1,
            Receiver = 2,
            Validator = 3
        }

        public enum Fee
        {
            CryptoTransfer = 2,             /* para transfer işlemi için genel işlem için */
            CryptoTransfer_Fast = 1,        /* para transfer işlemi için */
            CryptoTransfer_NoName = 3,      /* para transfer işlemi için */
            CryptoTransfer_ByPieces = 4,    /* para transfer işlemi için */

            TokenGeneration = 5,            /* token oluşturma işlemi için */
            TokenUpdate = 6,                /* token supplye veya diğer güncellemeler için işlem tutarı*/

            DataStorage = 7                 /* blok içeriğinde kaydedilen verinin her byte'ı için  */

        }

    }
}

