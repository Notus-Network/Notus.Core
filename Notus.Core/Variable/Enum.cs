using System;
using System.Collections.Generic;
using System.Numerics;

namespace Notus.Variable.Enum
{
    public enum NetworkNodeType
    {
        All = 2048,
        Connectable = 4096,
        Main = 0,
        Master = 1,
        Replicant = 2,
        Unknown = 1024,
        Suitable = 512
    }

    public enum PingServerType
    {
        MainNet = 0,
        Validator = 1,
        Staker = 2
    }

    public enum MempoolEachRecordLimitType
    {
        Time = 2,
        Count = 4
    }
    public enum ShortAlgorithmResultType
    {
        Mix = 2,
        OneByOne = 4
    }
    public enum HttpUrlPathCompareType
    {
        ExactMatch = 32,
        StartsWith = 64
    }
    public enum ValidatorOrder
    {
        Primary = 0,
        Controller = 1,
        Backup = 2,
        Wait = 4
    }
    public enum BlockIntegrityStatus
    {
        Valid = 0,
        GenesisNeed = 1,
        DamagedBlock = 2,

        ExtraData = 4,
        UndefinedError = 8,

        MultipleId = 1024,
        NonValid = 2048,
        MultipleHeight = 4096,
        WrongRowOrder = 8192,
        WrongBlockOrder = 16384,
        CheckAgain = 32768,
    }

    public enum NodeTypeSelector
    {
        Main = 0,
        Master = 1,
        Replicant = 2,
        Suitable = 3
    }

    public enum NetworkLayer
    {
        Unknown=99999,
        // MainLayer = 1,     // crypto and token transfer & token generation
        Layer1 = 1,     // crypto and token transfer & token generation

        // StorageLayer = 2,     // encrypted file storage ( never delete )
        Layer2 = 2,     // file storage ( never delete )

        // StorageLayer = 3,     // accesible file storage ( never delete )
        Layer3 = 3,     // message storage ( delete after 1 year )

        // MessageLayer = 4,     // message storage ( delete after 1 year )
        Layer4 = 4,    // not using
        Layer5 = 5,    // not using
        Layer6 = 6,    // not using
        Layer7 = 7,    // not using
        Layer8 = 8,    // not using
        Layer9 = 9,    // not using
        Layer10 = 10   // not using
    }
    public enum NetworkType
    {
        Unknown = 99999,
        MainNet = 10,
        TestNet = 20,
        DevNet = 30
    }

    public enum ProtectionLevel
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public enum BlockStatusCode
    {
        AddedToQueue = 0,
        InAlreadyQueue = 1,
        InProgress = 57,
        WrongParameter = 45,
        WrongPublicKey = 10,
        WrongSignature = 2,
        WrongVolume = 5,
        WrongWallet = 67,

        WrongWallet_Sender = 6,
        WrongWallet_Receiver = 9,

        WalletNotAllowed = 90,
        WalletDoesntExist = 11,
        InsufficientBalance = 12,
        PendingPreviousTransaction = 13,
        AnErrorOccurred = 99,
        Pending = 89,
        InQueue = 1,
        Completed = 22,
        Rejected = 97,
        Unknown = 78
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
    public enum LogLevel
    {
        Info = 0,
        Error = 1,
        Fatal=9,
        Warning = 2
    }
    public enum MultiWalletType
    {
        AllRequired = 0,
        MajorityRequired = 1
    }
}
