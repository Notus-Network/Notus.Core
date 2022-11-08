using System;
using System.Collections.Generic;
using System.Numerics;

namespace Notus.Variable.Struct
{
    public class MultiTransactionApproveStruct
    {
        public bool Approve { get; set; }
        public ulong CurrentTime { get; set; }
        public string Sign { get; set; }
        public string PublicKey { get; set; }
    }

    public class MultiWalletTransactionStruct
    {
        public Notus.Variable.Struct.CryptoTransaction Sender { get; set; }
        public Dictionary<string, MultiTransactionApproveStruct> Approve { get; set; }
        public Dictionary<string, Notus.Variable.Struct.BeforeBalanceStruct> Before { get; set; }
        public Dictionary<string, Dictionary<string, Dictionary<ulong, string>>> After { get; set; }
        public string Fee { get; set; }
    }

    public class CryptoTransaction
    {
        public ulong CurrentTime { get; set; }
        public string Currency { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Volume { get; set; }
        public string PublicKey { get; set; }
        public string Sign { get; set; }
        public string CurveName { get; set; }
    }
    /*
    public class MultiWalletFounderStruct
    {
        public string WalletKey { get; set; }
        public string PublicKey { get; set; }
    }
    public class MultiWalletStruct
    {
        public MultiWalletFounderStruct Founder { get; set; }
        public List<string> WalletList { get; set; }
        public string MultiWalletKey { get; set; }
        public Notus.Variable.Enum.MultiWalletType VoteType { get; set; }
        public string Sign { get; set; }
    }
    public class MultiWalletStoreStruct
    {
        public string UID { get; set; }
        public MultiWalletFounderStruct Founder { get; set; }
        public List<string> WalletList { get; set; }
        public string MultiWalletKey { get; set; }
        public Notus.Variable.Enum.MultiWalletType VoteType { get; set; }
        public Notus.Variable.Struct.WalletBalanceStruct? Balance { get; set; }
        public Dictionary<string, Dictionary<ulong, string>>? Out { get; set; }
        public string Fee { get; set; }
        public string Sign { get; set; }
    }
    public class LogStruct
    {
        public string WalletKey { get; set; }
        public Notus.Variable.Enum.LogLevel LogType { get; set; }
        public int LogNo { get; set; }
        public string BlockRowNo { get; set; }
        public string Message { get; set; }
        public string ExceptionType { get; set; }
        public string StackTrace { get; set; }
    }

    public class MultiWalletTransactionApproveStruct
    {
        public bool Approve { get; set; }
        public string TransactionId { get; set; }
        public ulong CurrentTime { get; set; }
        public string Sign { get; set; }
        public string PublicKey { get; set; }
    }

    public class MultiWalletTransactionVoteStruct
    {
        public string TransactionId { get; set; }
        public Notus.Variable.Struct.CryptoTransactionStruct Sender { get; set; }
        public Dictionary<string, MultiWalletTransactionApproveStruct> Approve { get; set; }
        public Notus.Variable.Enum.BlockStatusCode Status { get; set; }
        public Notus.Variable.Enum.MultiWalletType VoteType { get; set; }
    }

    public class MultiWalletTransactionVoteStruct
    {
        public Dictionary<string, Dictionary<ulong, string>>? Out { get; set; }
        public string Fee { get; set; }
    }
    */
}

