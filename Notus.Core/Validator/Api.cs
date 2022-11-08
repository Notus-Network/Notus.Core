using Notus.Variable.Struct;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using NGF = Notus.Variable.Globals.Functions;
using NVG = Notus.Variable.Globals;
using NP = Notus.Print;
using ND = Notus.Date;
namespace Notus.Validator
{
    public class Api : IDisposable
    {
        private DateTime LastNtpTime = Notus.Variable.Constant.DefaultTime;
        private TimeSpan NtpTimeDifference;
        private bool NodeTimeAfterNtpTime = false;      // time difference before or after NTP Server

        private List<string> AllMainList = new List<string>();
        private List<string> AllNodeList = new List<string>();
        private List<string> AllMasterList = new List<string>();
        private List<string> AllReplicantList = new List<string>();

        private Notus.Mempool ObjMp_TestBlockOrder;
        private Notus.Mempool ObjMp_AirdropLimit;
        private Notus.Mempool ObjMp_MultiSignPool;
        public Notus.Mempool Obj_MultiSignPool
        {
            get { return ObjMp_MultiSignPool; }
        }

        //private Notus.Mempool ObjMp_BlockOrderList;
        private Notus.Mempool ObjMp_CryptoTranStatus;
        public Notus.Mempool CryptoTranStatus
        {
            get { return ObjMp_CryptoTranStatus; }
            set { ObjMp_CryptoTranStatus = value; }
        }
        private Notus.Mempool ObjMp_CryptoTransfer;
        private ConcurrentDictionary<string, Notus.Variable.Enum.BlockStatusCode> Obj_TransferStatusList;

        //public System.Func<int, List<Notus.Variable.Struct.List_PoolBlockRecordStruct>?>? Func_GetPoolList = null;
        //public System.Func<Dictionary<int, int>?>? Func_GetPoolCount = null;
        //public System.Func<string, Notus.Variable.Class.BlockData?>? Func_OnReadFromChain = null;
        //public System.Func<Notus.Variable.Struct.PoolBlockRecordStruct, bool>? Func_AddToChainPool = null;

        private bool PrepareExecuted = false;

        //ffb_CurrencyList Currency list buffer
        private List<Notus.Variable.Struct.CurrencyList> ffb_CurrencyList = new List<Notus.Variable.Struct.CurrencyList>();
        private DateTime ffb_CurrencyList_LastCheck = ND.NowObj().Subtract(TimeSpan.FromDays(1));
        //private bool ffb_CurrencyList_Defined = false;
        private Notus.Variable.Enum.NetworkType ffb_CurrencyList_Network = Notus.Variable.Enum.NetworkType.MainNet;
        private Notus.Variable.Enum.NetworkLayer ffb_CurrencyList_Layer = Notus.Variable.Enum.NetworkLayer.Layer1;

        private void Prepare_Layer1()
        {
            if (NVG.Settings.GenesisCreated == false)
            {
                Obj_TransferStatusList = new ConcurrentDictionary<string, Notus.Variable.Enum.BlockStatusCode>();

                ObjMp_CryptoTransfer = new Notus.Mempool(Notus.IO.GetFolderName(NVG.Settings, Notus.Variable.Constant.StorageFolderName.Common) + "crypto_transfer");
                ObjMp_CryptoTransfer.AsyncActive = false;

                ObjMp_CryptoTranStatus = new Notus.Mempool(Notus.IO.GetFolderName(NVG.Settings, Notus.Variable.Constant.StorageFolderName.Common) + "crypto_transfer_status");
                ObjMp_CryptoTranStatus.AsyncActive = false;

                //NGF.BlockOrder.Clear();
                /*
                ObjMp_BlockOrderList = new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings, Notus.Variable.Constant.StorageFolderName.Common
                    ) + "ordered_block_list");

                ObjMp_BlockOrderList.AsyncActive = false;
                ObjMp_BlockOrderList.Clear();
                */

                ObjMp_MultiSignPool = new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings, Notus.Variable.Constant.StorageFolderName.Pool
                    ) + "multi_sign_tx");

                ObjMp_MultiSignPool.AsyncActive = false;

                ObjMp_AirdropLimit = new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings, Notus.Variable.Constant.StorageFolderName.Pool
                    ) + "airdrop_request");

                ObjMp_AirdropLimit.AsyncActive = false;


                ObjMp_TestBlockOrder = new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings, Notus.Variable.Constant.StorageFolderName.Block
                    ) + "test_block_order");

                ObjMp_TestBlockOrder.AsyncActive = false;
                ObjMp_TestBlockOrder.Clear();
            }
        }
        private void Prepare_Layer2()
        {
            if (NVG.Settings.GenesisCreated == false)
            {
                Obj_TransferStatusList = new ConcurrentDictionary<string, Notus.Variable.Enum.BlockStatusCode>();
                //NGF.Balance.Start();
                ObjMp_CryptoTransfer = new Notus.Mempool(Notus.IO.GetFolderName(NVG.Settings.Network, NVG.Settings.Layer, Notus.Variable.Constant.StorageFolderName.Common) + "crypto_transfer");
                ObjMp_CryptoTranStatus = new Notus.Mempool(Notus.IO.GetFolderName(NVG.Settings.Network, NVG.Settings.Layer, Notus.Variable.Constant.StorageFolderName.Common) + "crypto_transfer_status");

                ObjMp_CryptoTransfer.AsyncActive = false;
                ObjMp_CryptoTranStatus.AsyncActive = false;
            }
        }
        private void Prepare_Layer3()
        {
            if (NVG.Settings.GenesisCreated == false)
            {
                Obj_TransferStatusList = new ConcurrentDictionary<string, Notus.Variable.Enum.BlockStatusCode>();
                //NGF.Balance.Start();
                ObjMp_CryptoTransfer = new Notus.Mempool(Notus.IO.GetFolderName(NVG.Settings.Network, NVG.Settings.Layer, Notus.Variable.Constant.StorageFolderName.Common) + "crypto_transfer");
                ObjMp_CryptoTranStatus = new Notus.Mempool(Notus.IO.GetFolderName(NVG.Settings.Network, NVG.Settings.Layer, Notus.Variable.Constant.StorageFolderName.Common) + "crypto_transfer_status");

                ObjMp_CryptoTransfer.AsyncActive = false;
                ObjMp_CryptoTranStatus.AsyncActive = false;
            }
        }
        public void Prepare()
        {
            if (NVG.Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
            {
                Prepare_Layer1();
            }
            if (NVG.Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer2)
            {
                Prepare_Layer2();
            }
            if (NVG.Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer3)
            {
                Prepare_Layer3();
            }

            PrepareExecuted = true;
        }

        public void AddForCache(Notus.Variable.Class.BlockData Obj_BlockData)
        {
            ObjMp_TestBlockOrder.Set(
                DateTime.Now.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText),
                Obj_BlockData.info.rowNo + " - " +
                Obj_BlockData.prev + " = " +
                Obj_BlockData.info.uID
            );

            if (Obj_BlockData.prev.Length < 20)
            {
                NP.Info("Block Is Proccessing -> " +
                    Obj_BlockData.info.rowNo.ToString() + " -> " +
                    "Prev is Empty [ " + Obj_BlockData.prev + " ]"
                );
            }
            else
            {
                NP.Info("Block Is Proccessing -> " +
                    Obj_BlockData.info.rowNo.ToString() + " -> " +
                    Obj_BlockData.prev.Substring(0, 20)
                );
            }
            if (NGF.BlockOrder.ContainsKey(Obj_BlockData.info.rowNo) == false)
            {
                NGF.BlockOrder.TryAdd(Obj_BlockData.info.rowNo, Obj_BlockData.info.uID);
            }
            else
            {
                NGF.BlockOrder[Obj_BlockData.info.rowNo] = Obj_BlockData.info.uID;
            }

            /*
            string tmpBlockKey = ObjMp_BlockOrderList.Get(Obj_BlockData.info.rowNo.ToString(), string.Empty);
            if (tmpBlockKey.Length == 0)
            {
                //block-order-exception
                ObjMp_BlockOrderList.Add(
                    Obj_BlockData.info.rowNo.ToString(),
                    Obj_BlockData.info.uID
                );
            }
            else
            {
                Console.WriteLine("Block Row No Exist");
                Console.WriteLine("Block Row No Exist");
                Console.WriteLine("Block Row No Exist");
                Console.WriteLine("Block Row No Exist");
            }
            */

            NGF.Balance.Control(Obj_BlockData);
            if (Obj_BlockData.info.type == Notus.Variable.Enum.BlockTypeList.CryptoTransfer)
            {
                Notus.Variable.Class.BlockStruct_120? tmpBalanceVal = JsonSerializer.Deserialize<Notus.Variable.Class.BlockStruct_120>(System.Text.Encoding.UTF8.GetString(
                    System.Convert.FromBase64String(
                        Obj_BlockData.cipher.data
                    )
                ));
                if (tmpBalanceVal != null)
                {
                    //Console.WriteLine("Node.Api.AddToBalanceDB [cba09834] : " + Obj_BlockData.info.type);
                    foreach (KeyValuePair<string, Notus.Variable.Class.BlockStruct_120_In_Struct> entry in tmpBalanceVal.In)
                    {
                        RequestSend_Done(entry.Key, Obj_BlockData.info.rowNo, Obj_BlockData.info.uID);
                    }
                }
            }
            if (Obj_BlockData.info.type == Notus.Variable.Enum.BlockTypeList.LockAccount)
            {
                /*
                 
                Notus.Variable.Class.BlockStruct_120? tmpBalanceVal = JsonSerializer.Deserialize<Notus.Variable.Class.BlockStruct_120>(System.Text.Encoding.UTF8.GetString(
                    System.Convert.FromBase64String(
                        Obj_BlockData.cipher.data
                    )
                ));
                if (tmpBalanceVal != null)
                {
                    Console.WriteLine("Node.Api.AddToBalanceDB [cba09834] : " + Obj_BlockData.info.type);
                    foreach (KeyValuePair<string, Notus.Variable.Class.BlockStruct_120_In_Struct> entry in tmpBalanceVal.In)
                    {
                        RequestSend_Done(entry.Key, Obj_BlockData.info.rowNo, Obj_BlockData.info.uID);
                    }
                }
                */
            }
        }

        //layer -1 kontrolünü sağla
        private string Interpret_Layer1(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            return "";
        }

        public string Interpret(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            //Console.WriteLine("IncomeData.RawUrl : " + IncomeData.RawUrl);
            if (PrepareExecuted == false)
            {
                Prepare();
            }

            if (IncomeData.UrlList.Length == 0)
            {
                return JsonSerializer.Serialize(false);
            }
            string incomeFullUrlPath = string.Join("/", IncomeData.UrlList).ToLower();

            //Console.WriteLine("incomeFullUrlPath : " + incomeFullUrlPath);
            if (incomeFullUrlPath.Length < 2)
            {
                return JsonSerializer.Serialize(false);
            }

            if (string.Equals(incomeFullUrlPath.Substring(incomeFullUrlPath.Length - 1), "/"))
            {
                incomeFullUrlPath = incomeFullUrlPath.Substring(0, incomeFullUrlPath.Length - 1);
            }

            // storage işlemleri

            if (IncomeData.UrlList.Length > 2)
            {
                if (string.Equals(IncomeData.UrlList[0].ToLower(), "storage"))
                {
                    //Console.WriteLine(JsonSerializer.Serialize(IncomeData, Notus.Variable.Constant.JsonSetting);
                    //Console.WriteLine(JsonSerializer.Serialize(IncomeData));
                    if (string.Equals(IncomeData.UrlList[1].ToLower(), "file"))
                    {
                        //this parts need to organize
                        if (NVG.Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
                        {
                            if (string.Equals(IncomeData.UrlList[2].ToLower(), "new") && IncomeData.PostParams.ContainsKey("data") == true)
                            {
                                // bu fonksiyon şimdilik devre dışı
                                // genesis tamamlandığında burası tekrar aktive edilecek
                                return Request_Layer1_StoreFile_New(IncomeData);
                            }
                            if (string.Equals(IncomeData.UrlList[2].ToLower(), "status"))
                            {
                                return Request_Layer1_StoreFile_Status(IncomeData);
                            }
                        }
                        //this parts need to organize
                        if (NVG.Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer2)
                        {
                            if (string.Equals(IncomeData.UrlList[2].ToLower(), "new") && IncomeData.PostParams.ContainsKey("data") == true)
                            {
                                return Request_StoreEncryptedFile_New(IncomeData);
                            }
                            if (string.Equals(IncomeData.UrlList[2].ToLower(), "update") && IncomeData.PostParams.ContainsKey("data") == true)
                            {
                                return Request_StoreEncryptedFile_Update(IncomeData);
                            }
                            if (string.Equals(IncomeData.UrlList[2].ToLower(), "status"))
                            {
                                return Request_StoreEncryptedFile_Status(IncomeData);
                            }
                        }

                        if (NVG.Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer3)
                        {
                            if (string.Equals(IncomeData.UrlList[2].ToLower(), "new") && IncomeData.PostParams.ContainsKey("data") == true)
                            {
                                return Request_Layer3_StoreFileNew(IncomeData);
                            }
                            if (string.Equals(IncomeData.UrlList[2].ToLower(), "update") && IncomeData.PostParams.ContainsKey("data") == true)
                            {
                                return Request_Layer3_StoreFileUpdate(IncomeData);
                            }
                            if (string.Equals(IncomeData.UrlList[2].ToLower(), "status"))
                            {
                                return Request_Layer3_StoreFileStatus(IncomeData);
                            }
                        }
                    }
                }
            }
            if (string.Equals(incomeFullUrlPath, "ping"))
            {
                return "pong";
            }
            if (string.Equals(incomeFullUrlPath, "metrics"))
            {
                return Request_Metrics(IncomeData);
            }
            if (string.Equals(incomeFullUrlPath, "online"))
            {
                return Request_Online(IncomeData);
            }
            if (string.Equals(incomeFullUrlPath, "node"))
            {
                return Request_Node();
            }
            if (string.Equals(incomeFullUrlPath, "main"))
            {
                return Request_Main();
            }

            if (string.Equals(incomeFullUrlPath, "master"))
            {
                return Request_Master();
            }

            if (string.Equals(incomeFullUrlPath, "replicant"))
            {
                return Request_Replicant();
            }

            if (incomeFullUrlPath.StartsWith("token/generate/"))
            {
                return Request_GenerateToken(IncomeData);
            }


            if (incomeFullUrlPath.StartsWith("multi/"))
            {

                // burada block uid verilirse, blok detayları gösterilecek
                // eğer cüzdan adresi verilirse hangi block id olduğu listesi verilir.
                if (incomeFullUrlPath.StartsWith("multi/pool/"))
                {
                    if (IncomeData.UrlList[2].Length == 90)
                    {
                        string tmpBlockUid = IncomeData.UrlList[2];
                        Notus.Variable.Struct.MultiWalletTransactionVoteStruct? tmpResult = null;
                        Dictionary<string, Notus.Variable.Enum.BlockStatusCode> SignList
                            = new Dictionary<string, Notus.Variable.Enum.BlockStatusCode>();
                        ObjMp_MultiSignPool.Each((string multiKeyId, string multiTransferList) =>
                        {
                            //Console.WriteLine(multiKeyId);
                            //Console.WriteLine(multiTransferList);
                            if (tmpResult == null)
                            {
                                Dictionary<ulong, Notus.Variable.Struct.MultiWalletTransactionVoteStruct>? uidList =
                                    JsonSerializer.Deserialize<Dictionary<
                                        ulong,
                                        Notus.Variable.Struct.MultiWalletTransactionVoteStruct>
                                    >(multiTransferList);

                                if (uidList != null)
                                {
                                    foreach (KeyValuePair<ulong, Variable.Struct.MultiWalletTransactionVoteStruct> entry in uidList)
                                    {
                                        if (string.Equals(tmpBlockUid, entry.Value.TransactionId))
                                        {
                                            if (tmpResult == null)
                                            {
                                                tmpResult = entry.Value;
                                            }
                                        }
                                    }
                                }
                            }
                        });
                        if (tmpResult == null)
                        {
                            return JsonSerializer.Serialize(false);
                        }
                        return JsonSerializer.Serialize(tmpResult);
                    }

                    //burada seçilen cüzdan detayları verilecek...
                    if (IncomeData.UrlList[2].Length == Notus.Variable.Constant.SingleWalletTextLength)
                    {
                        string controlWalletId = IncomeData.UrlList[2];
                        bool multiWalletId = Notus.Wallet.MultiID.IsMultiId(controlWalletId, NVG.Settings.Network);

                        Dictionary<string, Notus.Variable.Enum.BlockStatusCode> SignList
                            = new Dictionary<string, Notus.Variable.Enum.BlockStatusCode>();
                        ObjMp_MultiSignPool.Each((string multiKeyId, string multiTransferList) =>
                        {
                            //Console.WriteLine(multiKeyId);
                            //Console.WriteLine(multiTransferList);
                            Dictionary<ulong, Notus.Variable.Struct.MultiWalletTransactionVoteStruct>? uidList =
                                JsonSerializer.Deserialize<Dictionary<
                                    ulong,
                                    Notus.Variable.Struct.MultiWalletTransactionVoteStruct>
                                >(multiTransferList);

                            if (uidList != null)
                            {
                                foreach (KeyValuePair<ulong, Variable.Struct.MultiWalletTransactionVoteStruct> entry in uidList)
                                {
                                    if (multiWalletId == true)
                                    {
                                        if (string.Equals(entry.Value.Sender.Sender, controlWalletId))
                                        {
                                            SignList.Add(entry.Value.TransactionId, entry.Value.Status);
                                        }
                                    }
                                    else
                                    {
                                        foreach (var innerEntry in entry.Value.Approve)
                                        {
                                            if (string.Equals(innerEntry.Key, controlWalletId))
                                            {
                                                SignList.Add(entry.Value.TransactionId, entry.Value.Status);
                                            }
                                        }
                                    }
                                }
                            }
                        });
                        return JsonSerializer.Serialize(SignList);
                    }
                }

                //burada pool listesinde bekleyen işlemler id ile listelenecek...
                if (string.Equals(incomeFullUrlPath, "multi/pool"))
                {
                    Dictionary<string, Notus.Variable.Enum.BlockStatusCode> SignList
                        = new Dictionary<string, Notus.Variable.Enum.BlockStatusCode>();
                    ObjMp_MultiSignPool.Each((string multiKeyId, string multiTransferList) =>
                    {
                        //Console.WriteLine(multiKeyId);
                        //Console.WriteLine(multiTransferList);
                        Dictionary<ulong, Notus.Variable.Struct.MultiWalletTransactionVoteStruct>? uidList =
                            JsonSerializer.Deserialize<Dictionary<
                                ulong,
                                Notus.Variable.Struct.MultiWalletTransactionVoteStruct>
                            >(multiTransferList);

                        if (uidList != null)
                        {
                            foreach (KeyValuePair<ulong, Variable.Struct.MultiWalletTransactionVoteStruct> entry in uidList)
                            {
                                SignList.Add(entry.Value.TransactionId, entry.Value.Status);
                            }
                        }
                    });
                    return JsonSerializer.Serialize(SignList);
                }

                //multi transation'ın onaylandığı url
                if (incomeFullUrlPath.StartsWith("multi/transaction/approve/"))
                {
                    return Request_ApproveMultiTransaction(IncomeData);

                }

                //multi wallet oluşturma işlemi
                if (string.Equals(incomeFullUrlPath, "multi/wallet/add"))
                {
                    return Request_AddMultiWallet(IncomeData);
                }

                if (IncomeData.UrlList.Length > 1)
                {
                    string tmpWallet = IncomeData.UrlList[1];
                    string multiPrefix = Notus.Variable.Constant.MultiWalletPrefix_MainNetwork;
                    string singlePrefix = Notus.Variable.Constant.SingleWalletPrefix_MainNetwork;
                    if (NVG.Settings.Network == Variable.Enum.NetworkType.DevNet)
                    {
                        singlePrefix = Notus.Variable.Constant.SingleWalletPrefix_DevelopmentNetwork;
                        multiPrefix = Notus.Variable.Constant.MultiWalletPrefix_DevelopmentNetwork;
                    }
                    if (NVG.Settings.Network == Variable.Enum.NetworkType.TestNet)
                    {
                        singlePrefix = Notus.Variable.Constant.SingleWalletPrefix_TestNetwork;
                        multiPrefix = Notus.Variable.Constant.MultiWalletPrefix_TestNetwork;
                    }
                    if (tmpWallet.Length >= singlePrefix.Length)
                    {
                        if (string.Equals(singlePrefix, tmpWallet.Substring(0, singlePrefix.Length)))
                        {
                            return JsonSerializer.Serialize(NGF.Balance.WalletsICanApprove(tmpWallet));
                        }
                    }
                    if (tmpWallet.Length >= multiPrefix.Length)
                    {
                        if (string.Equals(multiPrefix, tmpWallet.Substring(0, multiPrefix.Length)))
                        {
                            return JsonSerializer.Serialize(NGF.Balance.GetParticipant(tmpWallet));
                        }
                    }
                }
                return JsonSerializer.Serialize(false);
            }

            if (IncomeData.UrlList.Length > 0)
            {
                //burada nft işlemleri yapılıyor...
                if (IncomeData.UrlList.Length > 2)
                {
                    if (string.Equals(IncomeData.UrlList[0].ToLower(), "nft"))
                    {
                        if (string.Equals(IncomeData.UrlList[1].ToLower(), "list"))
                        {
                            return Request_NFTImageList(IncomeData);
                        }
                        if (string.Equals(IncomeData.UrlList[1].ToLower(), "detail"))
                        {
                            if (IncomeData.UrlList.Length > 3)
                            {
                                return Request_NFTPublicImageDetail(IncomeData);
                            }
                            else
                            {
                                return Request_NFTPrivateImageDetail(IncomeData);
                            }
                        }
                        return JsonSerializer.Serialize(false);
                    }
                }

                if (IncomeData.UrlList[0].ToLower() == "pool")
                {
                    if (IncomeData.UrlList.Length > 1)
                    {
                        if (int.TryParse(IncomeData.UrlList[1], out int blockTypeNo))
                        {

                            List<Variable.Struct.List_PoolBlockRecordStruct>? tmpPoolList =
                                NGF.BlockQueue.GetPoolList(blockTypeNo);
                            if (tmpPoolList != null)
                            {
                                if (tmpPoolList.Count > 0)
                                {
                                    Dictionary<string, string> tmpResultList = new Dictionary<string, string>();
                                    for (int innerCount = 0; innerCount < tmpPoolList.Count; innerCount++)
                                    {
                                        Variable.Struct.List_PoolBlockRecordStruct? tmpItem = tmpPoolList[innerCount];
                                        if (tmpItem != null)
                                        {
                                            tmpResultList.Add(tmpItem.key, tmpItem.data);
                                        }
                                    }

                                    if (tmpResultList.Count > 0)
                                    {
                                        return JsonSerializer.Serialize(tmpResultList);
                                    }
                                }
                            }
                        }
                    }
                    return JsonSerializer.Serialize(NGF.BlockQueue.GetPoolCount());
                }

                if (IncomeData.UrlList.Length > 1)
                {
                    if (string.Equals(IncomeData.UrlList[0].ToLower(), "lock"))
                    {
                        return Request_LockAccount(IncomeData);
                    }
                    if (string.Equals(IncomeData.UrlList[0].ToLower(), "balance"))
                    {
                        return Request_Balance(IncomeData);
                    }

                    // gönderilen işlem transferini veriyor
                    if (string.Equals(IncomeData.UrlList[0].ToLower(), "tx"))
                    {
                        if (IncomeData.UrlList[1].Length == Notus.Variable.Constant.SingleWalletTextLength)
                        {

                        }
                    }

                    // alınan işlem transferini veriyor
                    if (string.Equals(IncomeData.UrlList[0].ToLower(), "rx"))
                    {
                        if (IncomeData.UrlList[1].Length == Notus.Variable.Constant.SingleWalletTextLength)
                        {

                        }
                    }

                    // blok içeriklerini veriyor...
                    if (string.Equals(IncomeData.UrlList[0].ToLower(), "block"))
                    {
                        if (string.Equals(IncomeData.UrlList[1].ToLower(), "summary"))
                        {
                            return Request_BlockSummary(IncomeData);
                        }

                        if (string.Equals(IncomeData.UrlList[1].ToLower(), "last"))
                        {
                            return Request_BlockLast(IncomeData);
                        }
                        if (IncomeData.UrlList.Length > 2)
                        {
                            if (string.Equals(IncomeData.UrlList[1].ToLower(), "status"))
                            {
                                return Request_TransactionStatus(IncomeData);
                            }

                            if (string.Equals(IncomeData.UrlList[1].ToLower(), "hash"))
                            {
                                return Request_BlockHash(IncomeData);
                            }
                        }

                        return Request_Block(IncomeData);
                    }

                    // yapılan transferin durumunu geri gönderen fonksiyon
                    if (string.Equals(IncomeData.UrlList[0].ToLower(), "transaction"))
                    {
                        if (IncomeData.UrlList.Length > 2)
                        {
                            if (string.Equals(IncomeData.UrlList[1].ToLower(), "status"))
                            {
                                return Request_TransactionStatus(IncomeData);
                            }
                        }
                    }
                }

                if (string.Equals(IncomeData.UrlList[0].ToLower(), "currency") && IncomeData.UrlList.Length > 1)
                {
                    if (string.Equals(IncomeData.UrlList[1].ToLower(), "list"))
                    {
                        if ((ND.NowObj() - ffb_CurrencyList_LastCheck).TotalMinutes > 1)
                        {
                            ffb_CurrencyList_LastCheck = ND.NowObj();
                            ffb_CurrencyList = Notus.Wallet.Block.GetList(NVG.Settings.Network, NVG.Settings.Layer);
                        }
                        else
                        {
                            if (NVG.Settings.Network != ffb_CurrencyList_Network || NVG.Settings.Layer != ffb_CurrencyList_Layer)
                            {
                                ffb_CurrencyList = Notus.Wallet.Block.GetList(NVG.Settings.Network, NVG.Settings.Layer);
                            }
                        }
                        return JsonSerializer.Serialize(ffb_CurrencyList);
                    }
                }

                if (string.Equals(IncomeData.UrlList[0].ToLower(), "send") && IncomeData.PostParams.ContainsKey("data") == true)
                {
                    return Request_Send(IncomeData);
                }

                if (string.Equals(IncomeData.UrlList[0].ToLower(), "airdrop"))
                {
                    if (IncomeData.UrlList.Length > 1)
                    {
                        return AirDropRequest(IncomeData);
                    }
                }

                if (string.Equals(IncomeData.UrlList[0].ToLower(), "info"))
                {
                    if (IncomeData.UrlList.Length > 1)
                    {
                        if (string.Equals(IncomeData.UrlList[1].ToLower(), "genesis"))
                        {
                            return JsonSerializer.Serialize(NVG.Settings.Genesis);
                        }
                        if (string.Equals(IncomeData.UrlList[1].ToLower(), "reserve"))
                        {
                            return JsonSerializer.Serialize(NVG.Settings.Genesis.Reserve);
                        }
                        if (string.Equals(IncomeData.UrlList[1].ToLower(), "transfer"))
                        {
                            return JsonSerializer.Serialize(NVG.Settings.Genesis.Fee);
                        }
                    }
                }
            }

            // bu veri API class'ı tarafından değil, Queue Class'ı tarafından yorumlanacak
            if (IncomeData.UrlList.Length > 2)
            {
                if (
                    string.Equals(IncomeData.UrlList[0].ToLower(), "queue")
                    &&
                    string.Equals(IncomeData.UrlList[1].ToLower(), "node")
                    &&
                    IncomeData.PostParams.ContainsKey("data")
                )
                {
                    return "queue-data";
                }
            }
            return JsonSerializer.Serialize(false);
        }

        private string AirDropRequest(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            if (NVG.Settings.Network == Variable.Enum.NetworkType.MainNet)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 35496,
                    ErrorText = "NotSupported",
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.NotSupported
                });
            }

            string airdropStr = "2000000000";
            if (Notus.Variable.Constant.AirDropVolume.ContainsKey(NVG.Settings.Layer))
            {
                if (Notus.Variable.Constant.AirDropVolume[NVG.Settings.Layer].ContainsKey(NVG.Settings.Network))
                {
                    airdropStr = Notus.Variable.Constant.AirDropVolume[NVG.Settings.Layer][NVG.Settings.Network];
                }
            }

            string ReceiverWalletKey = IncomeData.UrlList[1];
            string controlStr = ObjMp_AirdropLimit.Get(ReceiverWalletKey, "");
            int.TryParse(controlStr, out int requestCount);
            if (requestCount > 1)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 371854,
                    ErrorText = "TooManyRequest",
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.TooManyRequest
                });
            }

            requestCount++;
            ObjMp_AirdropLimit.Set(ReceiverWalletKey, requestCount.ToString(), 4320, true);

            string tmpCoinCurrency = NVG.Settings.Genesis.CoinInfo.Tag;
            if (NGF.Balance.AccountIsLock(ReceiverWalletKey) == true)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 3827,
                    ErrorText = "WalletNotAllowed",
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.WalletNotAllowed
                });
            }

            if (NGF.Balance.WalletUsageAvailable(ReceiverWalletKey) == false)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 36789,
                    ErrorText = "WalletUsing",
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.WalletUsing
                });
            }

            if (NGF.Balance.StartWalletUsage(ReceiverWalletKey) == false)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 27468,
                    ErrorText = "AnErrorOccurred",
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }

            Notus.Variable.Struct.WalletBalanceStruct tmpBalanceBefore = NGF.Balance.Get(ReceiverWalletKey, 0);
            Notus.Variable.Struct.WalletBalanceStruct tmpBalanceAfter = NGF.Balance.Get(ReceiverWalletKey, 0);
            //Console.WriteLine(JsonSerializer.Serialize(tmpBalanceAfter, Notus.Variable.Constant.JsonSetting));

            ulong tmpCoinKeyVal = NVG.NOW.Int;
            if (tmpBalanceAfter.Balance[tmpCoinCurrency].ContainsKey(tmpCoinKeyVal) == false)
            {
                tmpBalanceAfter.Balance[tmpCoinCurrency].Add(tmpCoinKeyVal, airdropStr);
            }
            else
            {
                BigInteger tmpResult =
                    BigInteger.Parse(tmpBalanceAfter.Balance[tmpCoinCurrency][tmpCoinKeyVal]) +
                    BigInteger.Parse(airdropStr);
                tmpBalanceAfter.Balance[tmpCoinCurrency][tmpCoinKeyVal] = tmpResult.ToString();
            }
            //Console.WriteLine(JsonSerializer.Serialize(tmpBalanceAfter, Notus.Variable.Constant.JsonSetting));

            string tmpChunkIdKey = NGF.GenerateTxUid();

            Notus.Variable.Class.BlockStruct_125 airDrop = new Variable.Class.BlockStruct_125()
            {
                In = new Dictionary<string, Notus.Variable.Struct.WalletBalanceStruct>(),
                Out = new Dictionary<string, Dictionary<string, Dictionary<ulong, string>>>(),
                Validator = NVG.Settings.NodeWallet.WalletKey
            };
            airDrop.In.Add(tmpChunkIdKey, tmpBalanceBefore);
            airDrop.Out.Add(ReceiverWalletKey, tmpBalanceAfter.Balance);

            bool tmpAddResult = NGF.BlockQueue.Add(new Notus.Variable.Struct.PoolBlockRecordStruct()
            {
                uid = tmpChunkIdKey,
                type = Notus.Variable.Enum.BlockTypeList.AirDrop,
                data = JsonSerializer.Serialize(airDrop)
            });
            if (tmpAddResult == true)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 0,
                    ErrorText = "AddedToQueue",
                    ID = tmpChunkIdKey,
                    Result = Notus.Variable.Enum.BlockStatusCode.AddedToQueue
                });
            }
            return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
            {
                ErrorNo = 55632,
                ErrorText = "Unknown",
                ID = string.Empty,
                Result = Notus.Variable.Enum.BlockStatusCode.Unknown
            });
        }
        private Notus.Variable.Class.BlockData? GetBlockWithRowNo(Int64 BlockRowNo)
        {
            /*
            if (Func_OnReadFromChain == null)
            {
                return null;
            }
            */
            if (NVG.Settings == null)
            {
                return null;
            }
            if (NVG.Settings.LastBlock == null)
            {
                return null;
            }
            if (NVG.Settings.LastBlock.info.rowNo >= BlockRowNo)
            {
                if (NVG.Settings.LastBlock.info.rowNo == BlockRowNo)
                {
                    return NVG.Settings.LastBlock;
                }

                //string tmpBlockKey = ObjMp_BlockOrderList.Get(BlockRowNo.ToString(), string.Empty);
                string tmpBlockKey = string.Empty;
                if (NGF.BlockOrder.ContainsKey(BlockRowNo) == true)
                {
                    tmpBlockKey = NGF.BlockOrder[BlockRowNo];
                }
                else
                {
                    //Console.WriteLine("ContainsKey == false;");
                }

                if (tmpBlockKey.Length > 0)
                {
                    Notus.Variable.Class.BlockData? tmpStoredBlock = NGF.BlockQueue.ReadFromChain(tmpBlockKey);
                    if (tmpStoredBlock != null)
                    {
                        return tmpStoredBlock;
                    }
                    else
                    {
                    }
                }
                else
                {
                }

                bool exitPrevWhile = false;
                string PrevBlockIdStr = NVG.Settings.LastBlock.prev;
                while (exitPrevWhile == false)
                {
                    Notus.Variable.Class.BlockData? tmpStoredBlock = NGF.BlockQueue.ReadFromChain(PrevBlockIdStr.Substring(0, 90));
                    if (tmpStoredBlock != null)
                    {
                        if (tmpStoredBlock.info.rowNo == BlockRowNo)
                        {
                            return tmpStoredBlock;
                        }
                        PrevBlockIdStr = tmpStoredBlock.prev;
                    }
                    else
                    {
                        exitPrevWhile = true;
                    }
                }
            }
            return null;
        }

        private string Request_TransactionStatus(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            string tmpTransactionIdStr = IncomeData.UrlList[2].ToLower();
            string tmpDataResultStr = ObjMp_CryptoTranStatus.Get(tmpTransactionIdStr, string.Empty);
            if (tmpDataResultStr.Length > 5)
            {
                try
                {
                    Notus.Variable.Struct.CryptoTransferStatus Obj_CryptTrnStatus = JsonSerializer.Deserialize<Notus.Variable.Struct.CryptoTransferStatus>(tmpDataResultStr);
                    return JsonSerializer.Serialize(Obj_CryptTrnStatus.Code);
                }
                catch (Exception err)
                {
                    Console.WriteLine("Error Text [ba09c83fe] : " + err.Message);
                }
            }
            return JsonSerializer.Serialize(
                new Notus.Variable.Struct.CryptoTransferStatus()
                {
                    Code = Variable.Enum.BlockStatusCode.Unknown,
                    RowNo = 0,
                    Text = "Unknown",
                    UID = string.Empty
                }
            );
        }

        private string Request_Layer3_StoreFileNew(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            // we have to communicate with layer1 for crypto balance
            // if its says have not enough coin return balance not efficent
            // if its says have enogh coin then add file upload transaction 
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine(JsonSerializer.Serialize(IncomeData));
            Console.WriteLine("----------------------------------------------");
            int Val_Timeout = 86400 * 7; // it will wait 7 days, if its not completed during that time than delete file from db pool
            Notus.Variable.Struct.FileTransferStruct tmpFileData;
            //tmpFileData.
            try
            {
                tmpFileData = JsonSerializer.Deserialize<Notus.Variable.Struct.FileTransferStruct>(IncomeData.PostParams["data"]);
            }
            catch (Exception err)
            {
                NP.Danger(NVG.Settings, "Error Text [a46cbe8d9] : " + err.Message);
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }

            //string tmpTransferIdKey = Notus.Core.Function.GenerateBlockKey(true);
            string tmpTransferIdKey = IncomeData.UrlList[3].ToLower();
            using (Notus.Mempool ObjMp_FileChunkList =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings.Network,
                        NVG.Settings.Layer,
                        Notus.Variable.Constant.StorageFolderName.File) + "upload_list"
                )
            )
            {
                ObjMp_FileChunkList.AsyncActive = false;
                ObjMp_FileChunkList.Add(tmpTransferIdKey, JsonSerializer.Serialize(tmpFileData), Val_Timeout);
                ObjMp_FileChunkList.Add(tmpTransferIdKey + "_chunk", JsonSerializer.Serialize(new Dictionary<int, string>() { }), Val_Timeout);
            }

            using (Notus.Mempool ObjMp_FileStatus =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings.Network,
                        NVG.Settings.Layer,
                        Notus.Variable.Constant.StorageFolderName.File) + "upload_list_status"
                )
            )
            {
                ObjMp_FileStatus.AsyncActive = false;
                ObjMp_FileStatus.Add(tmpTransferIdKey, JsonSerializer.Serialize(Notus.Variable.Enum.BlockStatusCode.InQueue), Val_Timeout);
            }

            return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
            {
                UID = tmpTransferIdKey,
                Status = "AddedToQueue",
                Result = Notus.Variable.Enum.BlockStatusCode.AddedToQueue
            });
        }

        public void Layer3_StorageFileDone(string BlockUid)
        {
            using (Notus.Mempool ObjMp_FileList =
                            new Notus.Mempool(
                                Notus.IO.GetFolderName(
                                    NVG.Settings.Network,
                                    NVG.Settings.Layer,
                                    Notus.Variable.Constant.StorageFolderName.File) + "upload_list"
                            )
                        )
            {
                ObjMp_FileList.AsyncActive = false;
                ObjMp_FileList.Set(BlockUid, JsonSerializer.Serialize(Notus.Variable.Enum.BlockStatusCode.Completed));
            }
        }
        private string Request_Layer3_StoreFileUpdate(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            const int Val_Timeout = 86400 * 7;
            Notus.Variable.Struct.FileChunkStruct tmpChunkData;

            try
            {
                tmpChunkData = JsonSerializer.Deserialize<Notus.Variable.Struct.FileChunkStruct>(System.Uri.UnescapeDataString(IncomeData.PostParams["data"]));
            }
            catch (Exception err)
            {
                NP.Danger(NVG.Settings, "Error Text [a354cd67] : " + err.Message);
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }

            string tmpStorageIdKey = tmpChunkData.UID;
            string tmpChunkIdKey = Notus.Block.Key.Generate(ND.NowObj(), NVG.Settings.NodeWallet.WalletKey);
            int tmpStorageNo = Notus.Block.Key.CalculateStorageNumber(Notus.Convert.Hex2BigInteger(tmpChunkIdKey).ToString());

            using (Notus.Mempool ObjMp_FileChunkList =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings.Network,
                        NVG.Settings.Layer,
                        Notus.Variable.Constant.StorageFolderName.File) + "chunk_list_" + tmpStorageNo.ToString()
                )
            )
            {
                ObjMp_FileChunkList.AsyncActive = false;
                ObjMp_FileChunkList.Add(tmpChunkIdKey, System.Uri.EscapeDataString(tmpChunkData.Data), Val_Timeout);
            }

            Notus.Variable.Struct.FileTransferStruct tmpFileObj = new Notus.Variable.Struct.FileTransferStruct();
            using (Notus.Mempool ObjMp_FileList =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings.Network,
                        NVG.Settings.Layer,
                        Notus.Variable.Constant.StorageFolderName.File) + "upload_list"
                )
            )
            {
                ObjMp_FileList.AsyncActive = false;
                string tmpFileObjStr = ObjMp_FileList.Get(tmpStorageIdKey, "");
                if (tmpFileObjStr.Length == 0)
                {
                    return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                    {
                        UID = tmpStorageIdKey,
                        Status = "Unknown",
                        Result = Notus.Variable.Enum.BlockStatusCode.Unknown
                    });
                }

                tmpFileObj = JsonSerializer.Deserialize<Notus.Variable.Struct.FileTransferStruct>(tmpFileObjStr);

                int calculatedChunkLength = ((int)Math.Ceiling(System.Convert.ToDouble(tmpFileObj.FileSize / tmpFileObj.ChunkSize))) - 1;
                string tmpCurrentList = ObjMp_FileList.Get(tmpStorageIdKey + "_chunk", "");
                Dictionary<int, string> tmpChunkList = new Dictionary<int, string>();
                if (tmpCurrentList.Length > 0)
                {
                    try
                    {
                        tmpChunkList = JsonSerializer.Deserialize<Dictionary<int, string>>(tmpCurrentList);
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine("Notus.Validator.Api.Request_StoreFileUpdate -> Convertion Error - Line 595");
                        Console.WriteLine("Notus.Validator.Api.Request_StoreFileUpdate -> Convertion Error - Line 595");
                    }
                }
                tmpChunkList.Add(tmpChunkData.Count, tmpChunkIdKey);
                ObjMp_FileList.Set(tmpStorageIdKey + "_chunk", JsonSerializer.Serialize(tmpChunkList));

                if (calculatedChunkLength == tmpChunkData.Count)
                {
                    using (Notus.Mempool ObjMp_FileStatus =
                        new Notus.Mempool(
                            Notus.IO.GetFolderName(
                                NVG.Settings.Network,
                                NVG.Settings.Layer,
                                Notus.Variable.Constant.StorageFolderName.File) + "upload_list_status"
                        )
                    )
                    {
                        ObjMp_FileStatus.AsyncActive = false;
                        ObjMp_FileStatus.Set(tmpStorageIdKey, JsonSerializer.Serialize(Notus.Variable.Enum.BlockStatusCode.Pending), true);
                    }
                }
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = tmpStorageIdKey,
                    Status = "AddedToQueue",
                    Result = Notus.Variable.Enum.BlockStatusCode.AddedToQueue
                });
            }

        }
        private string Request_Layer3_StoreFileStatus(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            string tmpstorageIdStr = IncomeData.UrlList[3];

            using (Notus.Mempool ObjMp_FileStatus =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings.Network,
                        NVG.Settings.Layer,
                        Notus.Variable.Constant.StorageFolderName.File) + "upload_list_status"
                )
            )
            {
                ObjMp_FileStatus.AsyncActive = false;
                string tmpRawStr = ObjMp_FileStatus.Get(tmpstorageIdStr, "");
                try
                {
                    Notus.Variable.Enum.BlockStatusCode tmpUploadStatus = JsonSerializer.Deserialize<Notus.Variable.Enum.BlockStatusCode>(tmpRawStr);
                    return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                    {
                        UID = string.Empty,
                        Status = tmpUploadStatus.ToString(),
                        Result = tmpUploadStatus
                    });
                }
                catch (Exception err)
                {
                }
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = Notus.Variable.Enum.BlockStatusCode.Unknown
                });
            }
        }

        private string Request_StoreEncryptedFile_New(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            int Val_Timeout = 86400;
            Notus.Variable.Struct.FileTransferStruct tmpFileData;
            try
            {
                tmpFileData = JsonSerializer.Deserialize<Notus.Variable.Struct.FileTransferStruct>(IncomeData.PostParams["data"]);
            }
            catch (Exception err)
            {
                NP.Danger(NVG.Settings, "Error Text [a46cbe8d9] : " + err.Message);
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }

            string tmpTransferIdKey = Notus.Block.Key.Generate(ND.NowObj(), NVG.Settings.NodeWallet.WalletKey);
            using (Notus.Mempool ObjMp_FileChunkList =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings.Network,
                        NVG.Settings.Layer,
                        Notus.Variable.Constant.StorageFolderName.File) + "upload_list"
                )
            )
            {
                ObjMp_FileChunkList.AsyncActive = false;
                ObjMp_FileChunkList.Add(tmpTransferIdKey, JsonSerializer.Serialize(tmpFileData), Val_Timeout);
                ObjMp_FileChunkList.Add(tmpTransferIdKey + "_chunk", JsonSerializer.Serialize(new Dictionary<int, string>() { }), Val_Timeout);
            }

            using (Notus.Mempool ObjMp_FileStatus =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings.Network,
                        NVG.Settings.Layer,
                        Notus.Variable.Constant.StorageFolderName.File) + "upload_list_status"
                )
            )
            {
                ObjMp_FileStatus.AsyncActive = false;
                ObjMp_FileStatus.Add(tmpTransferIdKey, JsonSerializer.Serialize(Notus.Variable.Enum.BlockStatusCode.InQueue), Val_Timeout);
            }

            return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
            {
                UID = tmpTransferIdKey,
                Status = "AddedToQueue",
                Result = Notus.Variable.Enum.BlockStatusCode.AddedToQueue
            });
        }
        private string Request_StoreEncryptedFile_Update(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            const int Val_Timeout = 86400;
            Notus.Variable.Struct.FileChunkStruct tmpChunkData;

            /*
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine((IncomeData.PostParams["data"]));
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine(System.Uri.UnescapeDataString(IncomeData.PostParams["data"]));
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine(System.Uri.UnescapeDataString(System.Uri.UnescapeDataString(IncomeData.PostParams["data"])));
            Console.WriteLine("----------------------------------------------------");
            //Console.WriteLine((IncomeData.PostParams["data"]));
            //Console.WriteLine(JsonSerializer.Serialize(IncomeData.PostParams["data"]));
            //Console.WriteLine(JsonSerializer.Serialize(IncomeData.PostParams, Notus.Variable.Constant.JsonSetting));
            */
            try
            {
                tmpChunkData = JsonSerializer.Deserialize<Notus.Variable.Struct.FileChunkStruct>(System.Uri.UnescapeDataString(IncomeData.PostParams["data"]));
            }
            catch (Exception err)
            {
                NP.Danger(NVG.Settings, "Error Text [a354cd67] : " + err.Message);
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }

            string tmpStorageIdKey = tmpChunkData.UID;
            string tmpChunkIdKey = Notus.Block.Key.Generate(ND.NowObj(), NVG.Settings.NodeWallet.WalletKey);
            int tmpStorageNo = Notus.Block.Key.CalculateStorageNumber(Notus.Convert.Hex2BigInteger(tmpChunkIdKey).ToString());

            using (Notus.Mempool ObjMp_FileChunkList =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings.Network,
                        NVG.Settings.Layer,
                        Notus.Variable.Constant.StorageFolderName.File) + "chunk_list_" + tmpStorageNo.ToString()
                )
            )
            {
                ObjMp_FileChunkList.AsyncActive = false;
                ObjMp_FileChunkList.Add(tmpChunkIdKey, System.Uri.EscapeDataString(tmpChunkData.Data), Val_Timeout);
            }

            Notus.Variable.Struct.FileTransferStruct tmpFileObj = new Notus.Variable.Struct.FileTransferStruct();
            using (Notus.Mempool ObjMp_FileList =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings.Network,
                        NVG.Settings.Layer,
                        Notus.Variable.Constant.StorageFolderName.File) + "upload_list"
                )
            )
            {
                ObjMp_FileList.AsyncActive = false;
                string tmpFileObjStr = ObjMp_FileList.Get(tmpStorageIdKey, "");
                if (tmpFileObjStr.Length > 0)
                {
                    tmpFileObj = JsonSerializer.Deserialize<Notus.Variable.Struct.FileTransferStruct>(tmpFileObjStr);
                }


                int calculatedChunkLength = ((int)Math.Ceiling(System.Convert.ToDouble(tmpFileObj.FileSize / tmpFileObj.ChunkSize))) - 1;
                string tmpCurrentList = ObjMp_FileList.Get(tmpStorageIdKey + "_chunk", "");
                Dictionary<int, string> tmpChunkList = new Dictionary<int, string>();
                if (tmpCurrentList.Length > 0)
                {
                    try
                    {
                        tmpChunkList = JsonSerializer.Deserialize<Dictionary<int, string>>(tmpCurrentList);
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine("Notus.Validator.Api.Request_StoreFileUpdate -> Convertion Error - Line 595");
                        Console.WriteLine("Notus.Validator.Api.Request_StoreFileUpdate -> Convertion Error - Line 595");
                    }
                }
                tmpChunkList.Add(tmpChunkData.Count, tmpChunkIdKey);
                ObjMp_FileList.Set(tmpStorageIdKey + "_chunk", JsonSerializer.Serialize(tmpChunkList));
                if (calculatedChunkLength == tmpChunkData.Count)
                {
                    using (Notus.Mempool ObjMp_FileStatus =
                        new Notus.Mempool(
                            Notus.IO.GetFolderName(
                                NVG.Settings.Network,
                                NVG.Settings.Layer,
                                Notus.Variable.Constant.StorageFolderName.File) + "upload_list_status"
                        )
                    )
                    {
                        ObjMp_FileStatus.AsyncActive = false;
                        ObjMp_FileStatus.Set(tmpStorageIdKey, JsonSerializer.Serialize(Notus.Variable.Enum.BlockStatusCode.Pending), true);
                    }
                }
            }

            return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
            {
                UID = tmpStorageIdKey,
                Status = "AddedToQueue",
                Result = Notus.Variable.Enum.BlockStatusCode.AddedToQueue
            });
        }
        private string Request_StoreEncryptedFile_Status(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            string tmpstorageIdStr = IncomeData.UrlList[3];

            using (Notus.Mempool ObjMp_FileStatus =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings.Network,
                        NVG.Settings.Layer,
                        Notus.Variable.Constant.StorageFolderName.File) + "upload_list_status"
                )
            )
            {
                ObjMp_FileStatus.AsyncActive = false;
                string tmpRawStr = ObjMp_FileStatus.Get(tmpstorageIdStr, "");
                try
                {
                    Notus.Variable.Enum.BlockStatusCode tmpUploadStatus = JsonSerializer.Deserialize<Notus.Variable.Enum.BlockStatusCode>(tmpRawStr);
                    return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                    {
                        UID = string.Empty,
                        Status = tmpUploadStatus.ToString(),
                        Result = tmpUploadStatus
                    });
                }
                catch (Exception err)
                {
                }
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = Notus.Variable.Enum.BlockStatusCode.Unknown
                });
            }
        }

        private string Request_Layer1_StoreFile_New(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            return "Genesis coin işlemleri tamamlanana kadar beklemeye alındı";
            /*
            Notus.Variable.Struct.StorageOnChainStruct tmpStorageData;
            try
            {
                tmpStorageData = JsonSerializer.Deserialize<Notus.Variable.Struct.StorageOnChainStruct>(IncomeData.PostParams["data"]);
            }
            catch (Exception err)
            {
                NP.Danger(NVG.Settings, "Error Text [bad849506] : " + err.Message);
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }

            //Console.WriteLine(JsonSerializer.Serialize(NVG.Settings.Genesis.Fee, Notus.Variable.Constant.JsonSetting));
            //Console.WriteLine("Control_Point_4-a");
            // 1500 * 44304
            long StorageFee = NVG.Settings.Genesis.Fee.Data * tmpStorageData.Size;
            if (tmpStorageData.Encrypted == true)
            {
                StorageFee = StorageFee * 2;
            }

            string tmpWalletKey = Notus.Wallet.ID.GetAddressWithPublicKey(tmpStorageData.PublicKey);
            Notus.Variable.Struct.WalletBalanceStruct tmpWalletBalance = NGF.Balance.Get(tmpWalletKey);
            
            BigInteger tmpCurrentBalance = NGF.Balance.GetCoinBalance(tmpWalletBalance, Notus.Variable.Struct.MainCoinTagName);
            if (StorageFee > tmpCurrentBalance)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "InsufficientBalance",
                    Result = Notus.Variable.Enum.BlockStatusCode.InsufficientBalance
                });
            }
            if (Func_AddToChainPool == null)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }

            BigInteger tmpCoinLeft = tmpCurrentBalance - StorageFee;

            tmpWalletBalance.Balance[NVG.Settings.Genesis.CoinInfo.Tag] = tmpCoinLeft.ToString();

            tmpStorageData.Balance.Balance = tmpWalletBalance.Balance;
            tmpStorageData.Balance.RowNo = tmpWalletBalance.RowNo;
            tmpStorageData.Balance.UID = tmpWalletBalance.UID;
            tmpStorageData.Balance.Wallet = tmpWalletBalance.Wallet;
            tmpStorageData.Balance.Fee = StorageFee.ToString();

            Console.WriteLine(JsonSerializer.Serialize(tmpStorageData, Notus.Variable.Constant.JsonSetting));

            string tmpTransferIdKey = Notus.Core.Function.GenerateBlockKey(true);

            bool tmpAddResult = Func_AddToChainPool(new Notus.Variable.Struct.PoolBlockRecordStruct()
            {
                type = 240,
                data = JsonSerializer.Serialize(new List<string>() { tmpTransferIdKey, JsonSerializer.Serialize(tmpStorageData) })
            });
            if (tmpAddResult == true)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = tmpTransferIdKey,
                    Status = "AddedToQueue",
                    Result = Notus.Variable.Enum.BlockStatusCode.AddedToQueue
                });
            }
            return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
            {
                UID = tmpTransferIdKey,
                Status = "Unknown",
                Result = Notus.Variable.Enum.BlockStatusCode.Unknown
            });
            */
        }
        private string Request_Layer1_StoreFile_Status(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            string tmpstorageIdStr = IncomeData.UrlList[3];

            using (Notus.Mempool ObjMp_FileStatus =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings.Network,
                        NVG.Settings.Layer,
                        Notus.Variable.Constant.StorageFolderName.File) + "upload_list_status"
                )
            )
            {
                ObjMp_FileStatus.AsyncActive = false;
                string tmpRawStr = ObjMp_FileStatus.Get(tmpstorageIdStr, "");
                try
                {
                    Notus.Variable.Enum.BlockStatusCode tmpUploadStatus = JsonSerializer.Deserialize<Notus.Variable.Enum.BlockStatusCode>(tmpRawStr);
                    return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                    {
                        UID = string.Empty,
                        Status = tmpUploadStatus.ToString(),
                        Result = tmpUploadStatus
                    });
                }
                catch (Exception err)
                {
                }
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = Notus.Variable.Enum.BlockStatusCode.Unknown
                });
            }
        }

        private string Request_MultiSignatureSend(
            Notus.Variable.Struct.HttpRequestDetails IncomeData,
            Notus.Variable.Struct.CryptoTransactionStruct tmpTransfer
        )
        {
            Dictionary<ulong, Notus.Variable.Struct.MultiWalletTransactionVoteStruct>? uidList = null;
            string dbKeyStr = Notus.Toolbox.Text.ToHex(tmpTransfer.Sender, 90);
            string dbText = ObjMp_MultiSignPool.Get(dbKeyStr, "");
            if (dbText.Length > 0)
            {
                uidList = JsonSerializer.Deserialize<Dictionary<
                    ulong,
                    Notus.Variable.Struct.MultiWalletTransactionVoteStruct>
                >(dbText);
            }
            if (uidList == null)
            {
                uidList = new Dictionary<ulong, Notus.Variable.Struct.MultiWalletTransactionVoteStruct>();
            }

            if (uidList.ContainsKey(tmpTransfer.CurrentTime) == true)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 0,
                    ErrorText = uidList[tmpTransfer.CurrentTime].Status.ToString(),
                    ID = uidList[tmpTransfer.CurrentTime].TransactionId,
                    Result = uidList[tmpTransfer.CurrentTime].Status
                });
            }

            string tmpBlockUid = Notus.Block.Key.Generate(Notus.Date.ToDateTime(tmpTransfer.CurrentTime), NVG.Settings.NodeWallet.WalletKey);
            List<string>? participant = NGF.Balance.GetParticipant(tmpTransfer.Sender);
            uidList.Add(tmpTransfer.CurrentTime, new Variable.Struct.MultiWalletTransactionVoteStruct()
            {
                TransactionId = tmpBlockUid,
                Sender = tmpTransfer,
                VoteType = NGF.Balance.GetMultiWalletType(tmpTransfer.Sender),
                Status = Variable.Enum.BlockStatusCode.Pending,
                Approve = new Dictionary<string, Variable.Struct.MultiWalletTransactionApproveStruct>()
                {

                }
            });
            string calculatedWalletKey = Notus.Wallet.ID.GetAddressWithPublicKey(tmpTransfer.PublicKey, NVG.Settings.Network);
            for (int i = 0; i < participant.Count; i++)
            {
                if (string.Equals(participant[i], calculatedWalletKey) == false)
                {
                    uidList[tmpTransfer.CurrentTime].Approve.Add(
                        participant[i], new Variable.Struct.MultiWalletTransactionApproveStruct()
                        {
                            Approve = false,
                            TransactionId = "",
                            CurrentTime = 0,
                            PublicKey = "",
                            Sign = ""
                        }
                    );
                }
            }

            bool addingResult = ObjMp_MultiSignPool.Add(
                dbKeyStr,
                JsonSerializer.Serialize(uidList),
                Notus.Variable.Constant.MultiWalletTransactionTimeout
            );
            if (addingResult == true)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 0,
                    ErrorText = "AddedToQueue",
                    ID = tmpBlockUid,
                    Result = Notus.Variable.Enum.BlockStatusCode.AddedToQueue
                });
            }
            return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
            {
                ErrorNo = 7546,
                ErrorText = "AnErrorOccurred",
                ID = string.Empty,
                Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
            });
        }
        private string Request_Send(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            Notus.Variable.Struct.CryptoTransactionStruct? tmpTransfer;
            try
            {
                tmpTransfer = JsonSerializer.Deserialize<Notus.Variable.Struct.CryptoTransactionStruct>(
                    IncomeData.PostParams["data"]
                );
            }
            catch (Exception err)
            {
                NP.Danger(NVG.Settings, "Error Text [abc875768] : " + err.Message);
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 9618,
                    ErrorText = "AnErrorOccurred",
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }
            if (tmpTransfer == null)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 78945,
                    ErrorText = "AnErrorOccurred",
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }

            if (
                tmpTransfer.Volume == null ||
                tmpTransfer.Sign == null ||
                tmpTransfer.PublicKey == null ||
                tmpTransfer.Sender == null ||
                tmpTransfer.CurrentTime == 0 ||
                tmpTransfer.UnlockTime == 0 ||
                tmpTransfer.Currency == null ||
                tmpTransfer.Receiver == null
            )
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 4928,
                    ErrorText = "WrongParameter",
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.WrongParameter
                });
            }

            bool accountLocked = NGF.Balance.AccountIsLock(tmpTransfer.Sender);
            if (accountLocked == true)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 3827,
                    ErrorText = "WalletNotAllowed",
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.WalletNotAllowed
                });
            }
            if (Notus.Wallet.MultiID.IsMultiId(tmpTransfer.Sender, NVG.Settings.Network) == true)
            {
                return Request_MultiSignatureSend(IncomeData, tmpTransfer);
            }

            const int transferTimeOut = 0;
            if (tmpTransfer.Sender.Length != Notus.Variable.Constant.SingleWalletTextLength)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 7546,
                    ErrorText = "WrongWallet_Sender",
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.WrongWallet_Sender
                });
            }
            //receiver
            if (tmpTransfer.Receiver.Length != Notus.Variable.Constant.SingleWalletTextLength)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 5245,
                    ErrorText = "WrongWallet_Receiver",
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.WrongWallet_Receiver
                });
            }

            if (string.Equals(tmpTransfer.Receiver, tmpTransfer.Sender))
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 5245,
                    ErrorText = "WrongWallet_Receiver",
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.WrongWallet_Receiver
                });
            }
            DateTime rightNow = ND.NowObj();
            DateTime currentTime = Notus.Date.ToDateTime(tmpTransfer.CurrentTime);
            double totaSeconds = Math.Abs((rightNow - currentTime).TotalSeconds);
            // iki günden eski ise  zaman aşımı olarak işaretle
            if (totaSeconds > (2 * 86400))
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 5245,
                    ErrorText = "OldTransaction",
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.OldTransaction
                });
            }

            string calculatedWalletKey = Notus.Wallet.ID.GetAddressWithPublicKey(tmpTransfer.PublicKey, NVG.Settings.Network);
            if (string.Equals(calculatedWalletKey, tmpTransfer.Sender) == false)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 5245,
                    ErrorText = "WrongWallet_Sender",
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.WrongWallet_Sender
                });
            }

            if (Int64.TryParse(tmpTransfer.Volume, out _) == false)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 3652,
                    ErrorText = "WrongVolume",
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.WrongVolume
                });
            }


            string rawDataStr = Notus.Core.MergeRawData.Transaction(tmpTransfer);
            //transaction sign
            if (Notus.Wallet.ID.Verify(rawDataStr, tmpTransfer.Sign, tmpTransfer.PublicKey) == false)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 7314,
                    ErrorText = "WrongSignature",
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.WrongSignature
                });
            }


            // burada gelen bakiyeyi zaman kiliti ile kontrol edecek.

            Notus.Variable.Struct.WalletBalanceStruct tmpSenderBalanceObj = NGF.Balance.Get(tmpTransfer.Sender, 0);

            if (tmpSenderBalanceObj.Balance.ContainsKey(tmpTransfer.Currency) == false)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 7854,
                    ErrorText = "InsufficientBalance",
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.InsufficientBalance
                });
            }

            // if wallet wants to send coin then control only coin balance
            Int64 transferFee = Notus.Wallet.Fee.Calculate(
                Notus.Variable.Enum.Fee.CryptoTransfer,
                NVG.Settings.Network,
                NVG.Settings.Layer
            );
            if (string.Equals(tmpTransfer.Currency, NVG.Settings.Genesis.CoinInfo.Tag))
            {
                BigInteger RequiredBalanceInt = BigInteger.Parse(tmpTransfer.Volume) + transferFee;
                BigInteger CoinBalanceInt = NGF.Balance.GetCoinBalance(tmpSenderBalanceObj, NVG.Settings.Genesis.CoinInfo.Tag);

                if (RequiredBalanceInt > CoinBalanceInt)
                {
                    return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                    {
                        ErrorNo = 2536,
                        ErrorText = "InsufficientBalance",
                        ID = string.Empty,
                        Result = Notus.Variable.Enum.BlockStatusCode.InsufficientBalance
                    });
                }
            }
            else
            {
                if (tmpSenderBalanceObj.Balance.ContainsKey(NVG.Settings.Genesis.CoinInfo.Tag) == false)
                {
                    return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                    {
                        ErrorNo = 7854,
                        ErrorText = "InsufficientBalance",
                        ID = string.Empty,
                        Result = Notus.Variable.Enum.BlockStatusCode.InsufficientBalance
                    });
                }
                BigInteger coinFeeBalance = NGF.Balance.GetCoinBalance(tmpSenderBalanceObj, NVG.Settings.Genesis.CoinInfo.Tag);
                if (transferFee > coinFeeBalance)
                {
                    return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                    {
                        ErrorNo = 7523,
                        ErrorText = "InsufficientBalance",
                        ID = string.Empty,
                        Result = Notus.Variable.Enum.BlockStatusCode.InsufficientBalance
                    });
                }
                BigInteger tokenCurrentBalance = NGF.Balance.GetCoinBalance(tmpSenderBalanceObj, tmpTransfer.Currency);
                BigInteger RequiredBalanceInt = BigInteger.Parse(tmpTransfer.Volume);
                if (RequiredBalanceInt > tokenCurrentBalance)
                {
                    return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                    {
                        ErrorNo = 2365,
                        ErrorText = "InsufficientBalance",
                        ID = string.Empty,
                        Result = Notus.Variable.Enum.BlockStatusCode.InsufficientBalance
                    });
                }
            }

            // transfer process status is saved
            string tmpTransferIdKey = NGF.GenerateTxUid();
            ObjMp_CryptoTranStatus.Add(
                tmpTransferIdKey,
                JsonSerializer.Serialize(
                    new Notus.Variable.Struct.CryptoTransferStatus()
                    {
                        Code = Notus.Variable.Enum.BlockStatusCode.InQueue,
                        RowNo = 0,
                        UID = "",
                        Text = "InQueue"
                    }
                ),
                transferTimeOut
            );

            Notus.Variable.Struct.CryptoTransactionStoreStruct recordStruct = new Notus.Variable.Struct.CryptoTransactionStoreStruct()
            {
                Version = 1000,
                TransferId = tmpTransferIdKey,
                CurrentTime = tmpTransfer.CurrentTime,
                UnlockTime = tmpTransfer.UnlockTime,
                Currency = tmpTransfer.Currency,
                Sender = tmpTransfer.Sender,
                Receiver = tmpTransfer.Receiver,
                Volume = tmpTransfer.Volume,
                Fee = transferFee.ToString(),
                PublicKey = tmpTransfer.PublicKey,
                Sign = tmpTransfer.Sign,
            };

            // transfer data saved for next step
            ObjMp_CryptoTransfer.Add(tmpTransferIdKey, JsonSerializer.Serialize(recordStruct), transferTimeOut);

            Obj_TransferStatusList.TryAdd(tmpTransferIdKey, Notus.Variable.Enum.BlockStatusCode.AddedToQueue);

            if (NVG.Settings.PrettyJson == true)
            {
                return JsonSerializer.Serialize(
                    new Notus.Variable.Struct.CryptoTransactionResult()
                    {
                        ErrorNo = 0,
                        ErrorText = "AddedToQueue",
                        ID = tmpTransferIdKey,
                        Result = Notus.Variable.Enum.BlockStatusCode.AddedToQueue,
                    }, Notus.Variable.Constant.JsonSetting
                );
            }
            return JsonSerializer.Serialize(
                new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 0,
                    ErrorText = "AddedToQueue",
                    ID = tmpTransferIdKey,
                    Result = Notus.Variable.Enum.BlockStatusCode.AddedToQueue,
                }
            );
        }
        public int RequestSend_ListCount()
        {
            return ObjMp_CryptoTransfer.Count();
        }
        public ConcurrentDictionary<string, Notus.Variable.Struct.MempoolDataList> RequestSend_DataList()
        {
            return ObjMp_CryptoTransfer.DataList;
        }
        public void RequestSend_Remove(string tmpKeyStr)
        {
            ObjMp_CryptoTransfer.Remove(tmpKeyStr);
        }

        private void RequestSend_Done(string TransferKeyUid, Int64 tmpBlockRowNo, string tmpBlockUid)
        {
            if (TransferKeyUid.Length > 0)
            {
                ObjMp_CryptoTranStatus.Set(
                    TransferKeyUid,
                    JsonSerializer.Serialize(
                        new Notus.Variable.Struct.CryptoTransferStatus()
                        {
                            Code = Notus.Variable.Enum.BlockStatusCode.Completed,
                            RowNo = tmpBlockRowNo,
                            UID = tmpBlockUid,
                            Text = "Completed"
                        }
                    ),
                    86400
                );
            }
        }

        private string Request_Block(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            bool prettyJson = PrettyCheckForRaw(IncomeData, 2);
            if (IncomeData.UrlList[1].Length == 90)
            {
                try
                {
                    Notus.Variable.Class.BlockData? tmpStoredBlock = NGF.BlockQueue.ReadFromChain(IncomeData.UrlList[1]);
                    if (tmpStoredBlock != null)
                    {
                        if (prettyJson == true)
                        {
                            return JsonSerializer.Serialize(tmpStoredBlock, Notus.Variable.Constant.JsonSetting);
                        }
                        return JsonSerializer.Serialize(tmpStoredBlock);
                    }
                }
                catch (Exception err)
                {
                    NP.Danger(NVG.Settings, "Error Text [4a821b]: " + err.Message);
                    return JsonSerializer.Serialize(false);
                }
            }

            Int64 BlockNumber = 0;
            bool isNumeric = Int64.TryParse(IncomeData.UrlList[1], out BlockNumber);
            if (isNumeric == true)
            {
                Notus.Variable.Class.BlockData? tmpResultBlock = GetBlockWithRowNo(BlockNumber);
                if (tmpResultBlock != null)
                {
                    if (prettyJson == true)
                    {
                        return JsonSerializer.Serialize(tmpResultBlock, Notus.Variable.Constant.JsonSetting);
                    }
                    return JsonSerializer.Serialize(tmpResultBlock);
                }
            }
            return JsonSerializer.Serialize(false);
        }
        private string Request_BlockHash(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            if (IncomeData.UrlList[2].Length == 90)
            {
                try
                {
                    Notus.Variable.Class.BlockData? tmpStoredBlock = NGF.BlockQueue.ReadFromChain(IncomeData.UrlList[2]);
                    if (tmpStoredBlock != null)
                    {
                        return tmpStoredBlock.info.uID + tmpStoredBlock.sign;
                    }
                }
                catch (Exception err)
                {
                    NP.Danger(NVG.Settings, "Error Text [1f95ce]: " + err.Message);
                }
                return JsonSerializer.Serialize(false);
            }
            //Int64 BlockNumber2 = 0;
            bool isNumeric2 = Int64.TryParse(IncomeData.UrlList[2], out Int64 BlockNumber2);
            if (isNumeric2 == true)
            {
                Notus.Variable.Class.BlockData? tmpResultBlock = GetBlockWithRowNo(BlockNumber2);
                if (tmpResultBlock != null)
                {
                    return tmpResultBlock.info.uID + tmpResultBlock.sign;
                }
            }
            return JsonSerializer.Serialize(false);
        }
        private bool PrettyCheckForRaw(Notus.Variable.Struct.HttpRequestDetails IncomeData, int indexNo)
        {
            bool prettyJson = NVG.Settings.PrettyJson;
            if (IncomeData.UrlList.Length > indexNo)
            {
                if (string.Equals(IncomeData.UrlList[indexNo].ToLower(), "raw"))
                {
                    prettyJson = false;
                }
            }
            return prettyJson;
        }

        private string Request_BlockLast(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            if (PrettyCheckForRaw(IncomeData, 2) == true)
            {
                return JsonSerializer.Serialize(NVG.Settings.LastBlock, Notus.Variable.Constant.JsonSetting);
            }
            return JsonSerializer.Serialize(NVG.Settings.LastBlock);
        }
        private string Request_BlockSummary(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            if (PrettyCheckForRaw(IncomeData, 2) == true)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.LastBlockInfo()
                {
                    RowNo = NVG.Settings.LastBlock.info.rowNo,
                    uID = NVG.Settings.LastBlock.info.uID,
                    Sign = NVG.Settings.LastBlock.sign
                }, Notus.Variable.Constant.JsonSetting);

            }
            return JsonSerializer.Serialize(new Notus.Variable.Struct.LastBlockInfo()
            {
                RowNo = NVG.Settings.LastBlock.info.rowNo,
                uID = NVG.Settings.LastBlock.info.uID,
                Sign = NVG.Settings.LastBlock.sign
            });
        }
        private string Request_GenerateToken(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            if (IncomeData.UrlList.Length > 2)
            {
                if (IncomeData.UrlList[1].ToLower() != "generate")
                {
                    return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponseStruct()
                    {
                        UID = "",
                        Code = Notus.Variable.Constant.ErrorNoList.UnknownError,
                        Status = JsonSerializer.Serialize(IncomeData.UrlList)
                    });
                }
                string WalletKeyStr = IncomeData.UrlList[2];
                if (IncomeData.PostParams.ContainsKey("data") == false)
                {
                    return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponseStruct()
                    {
                        UID = "",
                        Code = Notus.Variable.Constant.ErrorNoList.MissingArgument,
                        Status = "MissingArgument"
                    });
                }

                bool walletLocked = false;
                try
                {
                    string tmpTokenStr = IncomeData.PostParams["data"];
                    const int transferTimeOut = 86400;
                    string CurrentCurrency = NVG.Settings.Genesis.CoinInfo.Tag;
                    Notus.Variable.Struct.WalletBalanceStruct tmpGeneratorBalanceObj = NGF.Balance.Get(WalletKeyStr, 0);
                    if (tmpGeneratorBalanceObj.Balance.ContainsKey(CurrentCurrency) == false)
                    {
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponseStruct()
                        {
                            UID = "",
                            Code = Notus.Variable.Constant.ErrorNoList.NeedCoin,
                            Status = "NeedCoin"
                        });
                    }

                    Notus.Variable.Struct.BlockStruct_160 tmpTokenObj = JsonSerializer.Deserialize<Notus.Variable.Struct.BlockStruct_160>(tmpTokenStr);

                    if (Notus.Wallet.Block.Exist(NVG.Settings.Network, NVG.Settings.Layer, tmpTokenObj.Info.Tag) == true)
                    {
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponseStruct()
                        {
                            UID = "",
                            Code = Notus.Variable.Constant.ErrorNoList.TagExists,
                            Status = "TagExists"
                        });
                    }

                    string TokenRawDataForSignText = Notus.Core.MergeRawData.TokenGenerate(tmpTokenObj.Creation.PublicKey, tmpTokenObj.Info, tmpTokenObj.Reserve);

                    if (Notus.Wallet.ID.Verify(TokenRawDataForSignText, tmpTokenObj.Creation.Sign, tmpTokenObj.Creation.PublicKey) == false)
                    {
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponseStruct()
                        {
                            UID = "",
                            Code = Notus.Variable.Constant.ErrorNoList.WrongSign,
                            Status = "WrongSign"
                        });
                    }

                    string tmpOwnerWalletStr = Notus.Wallet.ID.GetAddressWithPublicKey(tmpTokenObj.Creation.PublicKey, NVG.Settings.Network);
                    if (string.Equals(WalletKeyStr, tmpOwnerWalletStr) == false)
                    {
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponseStruct()
                        {
                            UID = "",
                            Code = Notus.Variable.Constant.ErrorNoList.WrongAccount,
                            Status = "WrongAccount"
                        });
                    }

                    if (NGF.Balance.WalletUsageAvailable(WalletKeyStr) == false)
                    {
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                        {
                            UID = string.Empty,
                            Status = "WalletUsing",
                            Result = Notus.Variable.Enum.BlockStatusCode.WalletUsing
                        });
                    }

                    if (NGF.Balance.StartWalletUsage(WalletKeyStr) == false)
                    {
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                        {
                            UID = string.Empty,
                            Status = "AnErrorOccurred",
                            Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                        });
                    }
                    walletLocked = true;
                    BigInteger WalletBalanceInt = NGF.Balance.GetCoinBalance(tmpGeneratorBalanceObj, NVG.Settings.Genesis.CoinInfo.Tag);
                    Int64 tmpFeeVolume = Notus.Wallet.Fee.Calculate(tmpTokenObj, NVG.Settings.Network);
                    if (tmpFeeVolume > WalletBalanceInt)
                    {
                        NGF.Balance.StopWalletUsage(WalletKeyStr);
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponseStruct()
                        {
                            UID = "",
                            Code = Notus.Variable.Constant.ErrorNoList.NeedCoin,
                            Status = "NeedCoin"
                        });
                    }
                    /*
                    if (Func_AddToChainPool == null)
                    {
                        NGF.Balance.StopWalletUsage(WalletKeyStr);
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponseStruct()
                        {
                            UID = "",
                            Code = Notus.Variable.Constant.ErrorNoList.UnknownError,
                            Status = "UnknownError"
                        });
                    }
                    */
                    // buraya token sahibinin önceki bakiyesi yazılacak,
                    // burada out ile nihai bakiyede belirtilecek
                    // tmpTokenObj.Validator = NVG.Settings.NodeWallet.WalletKey;
                    // tmpTokenObj.Balance
                    tmpTokenObj.Balance = new Notus.Variable.Class.WalletBalanceStructForTransaction()
                    {
                        Wallet = tmpGeneratorBalanceObj.Wallet,
                        WitnessBlockUid = tmpGeneratorBalanceObj.UID,
                        WitnessRowNo = tmpGeneratorBalanceObj.RowNo,
                        Balance = tmpGeneratorBalanceObj.Balance
                    };
                    tmpTokenObj.Validator = new Notus.Variable.Struct.ValidatorStruct()
                    {
                        NodeWallet = NVG.Settings.NodeWallet.WalletKey,
                        Reward = tmpFeeVolume.ToString()
                    };
                    (bool tmpBalanceResult, Notus.Variable.Struct.WalletBalanceStruct tmpNewGeneratorBalance) =
                        NGF.Balance.SubtractVolumeWithUnlockTime(
                            NGF.Balance.Get(WalletKeyStr, 0),
                            tmpFeeVolume.ToString(),
                            NVG.Settings.Genesis.CoinInfo.Tag
                        );

                    tmpTokenObj.Out = tmpNewGeneratorBalance.Balance;

                    //private string Request_GenerateToken(Notus.Variable.Struct.HttpRequestDetails IncomeData)
                    bool tmpAddResult = NGF.BlockQueue.Add(new Notus.Variable.Struct.PoolBlockRecordStruct()
                    {
                        uid = NGF.GenerateTxUid(),
                        type = Notus.Variable.Enum.BlockTypeList.TokenGeneration,
                        data = JsonSerializer.Serialize(tmpTokenObj)
                    });
                    if (tmpAddResult == true)
                    {
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponseStruct()
                        {
                            UID = tmpTokenObj.Creation.UID,
                            Code = Notus.Variable.Constant.ErrorNoList.AddedToQueue,
                            Status = "AddedToQueue"
                        });
                    }

                    NGF.Balance.StopWalletUsage(WalletKeyStr);
                    return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponseStruct()
                    {
                        UID = "",
                        Code = Notus.Variable.Constant.ErrorNoList.UnknownError,
                        Status = "UnknownError"
                    });
                }
                catch (Exception err)
                {
                    if (walletLocked == true)
                    {
                        NGF.Balance.StopWalletUsage(WalletKeyStr);
                    }
                    return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponseStruct()
                    {
                        UID = "",
                        Code = Notus.Variable.Constant.ErrorNoList.UnknownError,
                        Status = "UnknownError"
                    });
                }
            }

            return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponseStruct()
            {
                UID = "",
                Code = Notus.Variable.Constant.ErrorNoList.UnknownError,
                Status = JsonSerializer.Serialize(IncomeData.UrlList)
            });
        }

        private string Request_ApproveMultiTransaction(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            // önce genel kontroller yapılıyor....
            if (IncomeData.PostParams.ContainsKey("data") == false)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 1111,
                    ErrorText = "WrongParameter",
                    Result = Notus.Variable.Enum.BlockStatusCode.WrongParameter
                });
            }
            /*
            if (Func_AddToChainPool == null)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 2398565,
                    ErrorText = "Unknown",
                    Result = Notus.Variable.Enum.BlockStatusCode.Unknown
                });
            }
            */
            if (NVG.Settings == null)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 2222,
                    ErrorText = "Unknown",
                    Result = Notus.Variable.Enum.BlockStatusCode.Unknown
                });
            }
            if (NVG.Settings.Genesis == null)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 3333,
                    ErrorText = "Unknown",
                    Result = Notus.Variable.Enum.BlockStatusCode.Unknown
                });
            }
            if (NVG.Settings.NodeWallet == null)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 4444,
                    ErrorText = "Unknown",
                    Result = Notus.Variable.Enum.BlockStatusCode.Unknown
                });
            }

            // gelen data dönüştürülüyor....
            string tmpLockAccountStr = IncomeData.PostParams["data"];
            Notus.Variable.Struct.MultiWalletTransactionApproveStruct? TransctionApproveObj =
                JsonSerializer.Deserialize<Notus.Variable.Struct.MultiWalletTransactionApproveStruct>(tmpLockAccountStr);
            if (TransctionApproveObj == null)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 5555,
                    ErrorText = "AnErrorOccurred",
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }


            string multiWalletKey = IncomeData.UrlList[3];
            string rawDataStr = Core.MergeRawData.ApproveMultiWalletTransaction(
                TransctionApproveObj.Approve,
                TransctionApproveObj.TransactionId,
                TransctionApproveObj.CurrentTime
            );

            // gelenn verinin doğruluğu test ediliyor.... 
            bool verifyTx = Notus.Wallet.ID.Verify(
                rawDataStr,
                TransctionApproveObj.Sign,
                TransctionApproveObj.PublicKey
            );
            if (verifyTx == false)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 7777,
                    ErrorText = "WrongSignature",
                    Result = Notus.Variable.Enum.BlockStatusCode.WrongSignature
                });
            }


            string voter_WalletKey = Notus.Wallet.ID.GetAddressWithPublicKey(
                TransctionApproveObj.PublicKey,
                NVG.Settings.Network
            );
            Dictionary<string, Notus.Variable.Enum.BlockStatusCode> SignList
                = new Dictionary<string, Notus.Variable.Enum.BlockStatusCode>();
            string multiTxText = string.Empty;
            ulong txTime = 0;
            string multiKeyId = string.Empty;
            ObjMp_MultiSignPool.Each((string tmpMultiKeyId, string multiTransferList) =>
            {
                if (multiTxText.Length == 0)
                {
                    Dictionary<ulong, Notus.Variable.Struct.MultiWalletTransactionVoteStruct>? uidList =
                        JsonSerializer.Deserialize<Dictionary<
                            ulong,
                            Notus.Variable.Struct.MultiWalletTransactionVoteStruct>
                        >(multiTransferList);
                    if (uidList != null)
                    {
                        foreach (KeyValuePair<ulong, Variable.Struct.MultiWalletTransactionVoteStruct> entry in uidList)
                        {
                            if (string.Equals(TransctionApproveObj.TransactionId, entry.Value.TransactionId))
                            {
                                txTime = entry.Key;
                                if (entry.Value.Approve.ContainsKey(voter_WalletKey))
                                {
                                    multiTxText = multiTransferList;
                                    multiKeyId = tmpMultiKeyId;
                                }
                            }
                        }
                    }
                }
            });

            if (multiTxText.Length == 0)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 8888,
                    ErrorText = "UnknownTransaction",
                    Result = Notus.Variable.Enum.BlockStatusCode.UnknownTransaction
                });
            }

            Dictionary<ulong, Notus.Variable.Struct.MultiWalletTransactionVoteStruct>? uidList =
                JsonSerializer.Deserialize<Dictionary<
                    ulong,
                    Notus.Variable.Struct.MultiWalletTransactionVoteStruct>
                >(multiTxText);

            if (uidList == null)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 9999,
                    ErrorText = "AnErrorOccurred",
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }

            // sadece bekliyor durumunda aşağıya devam edecek.
            if (uidList[txTime].Status != Variable.Enum.BlockStatusCode.Pending)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 1212,
                    ErrorText = uidList[txTime].Status.ToString(),
                    Result = uidList[txTime].Status
                });
            }

            // ilemi onaylayan kullanıcının imzası yapıya eşitlenecek...
            uidList[txTime].Approve[voter_WalletKey].Approve = TransctionApproveObj.Approve;
            uidList[txTime].Approve[voter_WalletKey].TransactionId = TransctionApproveObj.TransactionId;
            uidList[txTime].Approve[voter_WalletKey].CurrentTime = TransctionApproveObj.CurrentTime;
            uidList[txTime].Approve[voter_WalletKey].Sign = TransctionApproveObj.Sign;
            uidList[txTime].Approve[voter_WalletKey].PublicKey = TransctionApproveObj.PublicKey;


            // verilen oylar veya red'ler sayılacak
            int voterCount = 0;
            int approveCount = 0;
            int refuseCount = 0;
            foreach (KeyValuePair<string, Variable.Struct.MultiWalletTransactionApproveStruct> entry in uidList[txTime].Approve)
            {
                voterCount++;
                if (entry.Value.Approve == true)
                {
                    approveCount++;
                }
                else
                {
                    if (entry.Value.PublicKey.Length > 0)
                    {
                        refuseCount++;
                    }
                }
            }
            bool acceptTx = false;
            bool refuseTx = false;
            if (uidList[txTime].VoteType == Variable.Enum.MultiWalletType.AllRequired)
            {
                if (refuseCount == 0)
                {
                    if (voterCount == approveCount)
                    {
                        acceptTx = true;
                    }
                }
                else
                {
                    refuseTx = true;
                }
            }
            if (uidList[txTime].VoteType == Variable.Enum.MultiWalletType.MajorityRequired)
            {
                int needVote = System.Convert.ToInt32(Math.Ceiling((decimal)voterCount / 2)) + 1;
                if (approveCount >= needVote)
                {
                    acceptTx = true;
                }
            }

            if (acceptTx == true)
            {
                uidList[txTime].Status = Variable.Enum.BlockStatusCode.InProgress;
            }
            else
            {
                if (refuseTx == true)
                {
                    uidList[txTime].Status = Variable.Enum.BlockStatusCode.Rejected;
                }
                else
                {
                    uidList[txTime].Status = Variable.Enum.BlockStatusCode.Pending;
                }
            }

            // eğer verilen oy yeterli değilse havuzda beklemeye devam edecek
            if (uidList[txTime].Status != Variable.Enum.BlockStatusCode.InProgress)
            {
                ObjMp_MultiSignPool.Set(multiKeyId, JsonSerializer.Serialize(uidList));
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 0,
                    ErrorText = uidList[txTime].Status.ToString(),
                    Result = uidList[txTime].Status
                });
            }

            // tüm süreçler tamamsa, şimdi sırada
            // cüzdanların kilitlenmesi işlemi başlıyor
            string validatorWalletKey = NVG.Settings.NodeWallet.WalletKey;
            string receiverWalletKey = uidList[txTime].Sender.Receiver;
            if (
                NGF.Balance.WalletUsageAvailable(multiWalletKey) == false
                ||
                NGF.Balance.WalletUsageAvailable(receiverWalletKey) == false
                ||
                NGF.Balance.WalletUsageAvailable(validatorWalletKey) == false
            )
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 987987,
                    ErrorText = "WalletUsing",
                    Result = Notus.Variable.Enum.BlockStatusCode.WalletUsing
                });
            }

            if (
                NGF.Balance.StartWalletUsage(multiWalletKey) == false
                ||
                NGF.Balance.StartWalletUsage(receiverWalletKey) == false
                ||
                NGF.Balance.StartWalletUsage(validatorWalletKey) == false
            )
            {
                NGF.Balance.StopWalletUsage(multiWalletKey);
                NGF.Balance.StopWalletUsage(receiverWalletKey);
                NGF.Balance.StopWalletUsage(validatorWalletKey);
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 953268,
                    ErrorText = "AnErrorOccurred",
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }
            string transferCoinName = uidList[txTime].Sender.Currency;


            // burada gelen bakiyeyi zaman kiliti ile kontrol edecek.
            Notus.Variable.Struct.WalletBalanceStruct tmpValidatorWalletBalanceObj_Current = NGF.Balance.Get(NVG.Settings.NodeWallet.WalletKey, 0);
            Notus.Variable.Struct.WalletBalanceStruct tmpValidatorWalletBalanceObj_New = NGF.Balance.Get(NVG.Settings.NodeWallet.WalletKey, 0);

            Notus.Variable.Struct.WalletBalanceStruct tmpReceiverWalletBalanceObj_Current = NGF.Balance.Get(uidList[txTime].Sender.Receiver, 0);
            Notus.Variable.Struct.WalletBalanceStruct tmpReceiverWalletBalanceObj_New = NGF.Balance.Get(uidList[txTime].Sender.Receiver, 0);

            Notus.Variable.Struct.WalletBalanceStruct tmpMultiWalletBalanceObj_Current = NGF.Balance.Get(multiWalletKey, 0);
            Notus.Variable.Struct.WalletBalanceStruct tmpMultiWalletBalanceObj_New = NGF.Balance.Get(multiWalletKey, 0);

            // yeterli coin ve / veya token kontrolü yapılıyor
            Int64 transferFee = Notus.Wallet.Fee.Calculate(
                Notus.Variable.Enum.Fee.CryptoTransfer_MultiSign,
                NVG.Settings.Network, NVG.Settings.Layer
            );

            BigInteger tokenNeeded = 0;
            BigInteger coinNeeded = 0;
            BigInteger coinTransferVolume = 0;

            if (string.Equals(transferCoinName, NVG.Settings.Genesis.CoinInfo.Tag))
            {
                coinTransferVolume = BigInteger.Parse(uidList[txTime].Sender.Volume);
                coinNeeded = coinTransferVolume + transferFee;
                BigInteger CoinBalanceInt = NGF.Balance.GetCoinBalance(
                    tmpMultiWalletBalanceObj_New,
                    NVG.Settings.Genesis.CoinInfo.Tag
                );

                if (coinNeeded > CoinBalanceInt)
                {
                    NGF.Balance.StopWalletUsage(multiWalletKey);
                    NGF.Balance.StopWalletUsage(receiverWalletKey);
                    NGF.Balance.StopWalletUsage(validatorWalletKey);
                    return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                    {
                        ErrorNo = 2536,
                        ErrorText = "InsufficientBalance",
                        ID = string.Empty,
                        Result = Notus.Variable.Enum.BlockStatusCode.InsufficientBalance
                    });
                }
            }
            else
            {
                coinNeeded = transferFee;
                BigInteger coinFeeBalance = NGF.Balance.GetCoinBalance(
                    tmpMultiWalletBalanceObj_New,
                    NVG.Settings.Genesis.CoinInfo.Tag
                );
                if (transferFee > coinFeeBalance)
                {
                    NGF.Balance.StopWalletUsage(multiWalletKey);
                    NGF.Balance.StopWalletUsage(receiverWalletKey);
                    NGF.Balance.StopWalletUsage(validatorWalletKey);
                    return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                    {
                        ErrorNo = 7523,
                        ErrorText = "InsufficientBalance",
                        ID = string.Empty,
                        Result = Notus.Variable.Enum.BlockStatusCode.InsufficientBalance
                    });
                }
                BigInteger tokenCurrentBalance = NGF.Balance.GetCoinBalance(
                    tmpMultiWalletBalanceObj_New,
                    transferCoinName
                );
                tokenNeeded = BigInteger.Parse(uidList[txTime].Sender.Volume);
                if (tokenNeeded > tokenCurrentBalance)
                {
                    NGF.Balance.StopWalletUsage(multiWalletKey);
                    NGF.Balance.StopWalletUsage(receiverWalletKey);
                    NGF.Balance.StopWalletUsage(validatorWalletKey);
                    return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                    {
                        ErrorNo = 2365,
                        ErrorText = "InsufficientBalance",
                        ID = string.Empty,
                        Result = Notus.Variable.Enum.BlockStatusCode.InsufficientBalance
                    });
                }
            }


            // eklenecek ve çıkarılacak token / coinler hesaplardan çıkartılıyor...
            (bool volumeError, tmpMultiWalletBalanceObj_New) =
                NGF.Balance.SubtractVolumeWithUnlockTime(
                    tmpMultiWalletBalanceObj_New,
                    coinNeeded.ToString(),
                    NVG.Settings.Genesis.CoinInfo.Tag
                );
            if (volumeError == true)
            {
                NGF.Balance.StopWalletUsage(multiWalletKey);
                NGF.Balance.StopWalletUsage(receiverWalletKey);
                NGF.Balance.StopWalletUsage(validatorWalletKey);
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 9102,
                    ErrorText = "AnErrorOccurred",
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }
            if (tokenNeeded > 0)
            {
                (volumeError, tmpMultiWalletBalanceObj_New) =
                    NGF.Balance.SubtractVolumeWithUnlockTime(
                        tmpMultiWalletBalanceObj_New,
                        tokenNeeded.ToString(),
                        uidList[txTime].Sender.Currency
                    );

                tmpReceiverWalletBalanceObj_New = NGF.Balance.AddVolumeWithUnlockTime(
                    tmpReceiverWalletBalanceObj_New,
                    tokenNeeded.ToString(),
                    uidList[txTime].Sender.Currency,
                    uidList[txTime].Sender.UnlockTime
                );
            }
            else
            {
                tmpReceiverWalletBalanceObj_New = NGF.Balance.AddVolumeWithUnlockTime(
                    tmpReceiverWalletBalanceObj_New,
                    coinTransferVolume.ToString(),
                    NVG.Settings.Genesis.CoinInfo.Tag,
                    uidList[txTime].Sender.UnlockTime
                );
            }

            ulong validatorUnlockTime = Notus.Date.ToLong(
                Notus.Date.ToDateTime(uidList[txTime].Sender.CurrentTime).AddDays(30)
            );

            tmpValidatorWalletBalanceObj_New = NGF.Balance.AddVolumeWithUnlockTime(
                tmpValidatorWalletBalanceObj_New,
                transferFee.ToString(),
                NVG.Settings.Genesis.CoinInfo.Tag,
                validatorUnlockTime
            );

            // içeriği boş olan zaman bilgili alanlar Dictionary'den çıkartılıyor
            tmpMultiWalletBalanceObj_New.Balance[NVG.Settings.Genesis.CoinInfo.Tag] =
                NGF.Balance.RemoveZeroUnlockTime(
                    tmpMultiWalletBalanceObj_New.Balance[NVG.Settings.Genesis.CoinInfo.Tag]
                );

            if (string.Equals(uidList[txTime].Sender.Currency, NVG.Settings.Genesis.CoinInfo.Tag) != false)
            {
                tmpMultiWalletBalanceObj_New.Balance[uidList[txTime].Sender.Currency] =
                    NGF.Balance.RemoveZeroUnlockTime(
                        tmpMultiWalletBalanceObj_New.Balance[uidList[txTime].Sender.Currency]
                    );
            }

            tmpReceiverWalletBalanceObj_New.Balance[uidList[txTime].Sender.Currency] =
                NGF.Balance.RemoveZeroUnlockTime(
                    tmpReceiverWalletBalanceObj_New.Balance[uidList[txTime].Sender.Currency]
                );

            string transactionId = TransctionApproveObj.TransactionId;
            Dictionary<string, Notus.Variable.Struct.MultiWalletTransactionStruct> multiTx = new
                Dictionary<string, Notus.Variable.Struct.MultiWalletTransactionStruct>(){
                {
                    transactionId,
                    new Notus.Variable.Struct.MultiWalletTransactionStruct()
                    {
                        Sender = new Variable.Struct.CryptoTransaction()
                        {
                            Currency = uidList[txTime].Sender.Currency,
                            CurrentTime = uidList[txTime].Sender.CurrentTime,
                            CurveName = uidList[txTime].Sender.CurveName,
                            PublicKey = uidList[txTime].Sender.PublicKey,
                            Receiver = uidList[txTime].Sender.Receiver,
                            Sender = uidList[txTime].Sender.Sender,
                            Sign = uidList[txTime].Sender.Sign,
                            Volume = uidList[txTime].Sender.Volume
                        },
                        Approve = new Dictionary<string, MultiTransactionApproveStruct>(),
                        Before = new Dictionary<string, Variable.Struct.BeforeBalanceStruct>()
                        {
                            {
                                multiWalletKey,
                                new Variable.Struct.BeforeBalanceStruct(){
                                     Balance=tmpMultiWalletBalanceObj_Current.Balance,
                                     Witness=new Variable.Struct.WitnessBlock()
                                     {
                                        RowNo=tmpMultiWalletBalanceObj_Current.RowNo,
                                        UID=tmpMultiWalletBalanceObj_Current.UID
                                     }
                                }
                            },
                            {
                                receiverWalletKey,
                                new Variable.Struct.BeforeBalanceStruct(){
                                     Balance=tmpReceiverWalletBalanceObj_Current.Balance,
                                     Witness=new Variable.Struct.WitnessBlock()
                                     {
                                        RowNo=tmpReceiverWalletBalanceObj_Current.RowNo,
                                        UID=tmpReceiverWalletBalanceObj_Current.UID
                                     }
                                }
                            },
                            {
                                NVG.Settings.NodeWallet.WalletKey,
                                new Variable.Struct.BeforeBalanceStruct(){
                                     Balance=tmpValidatorWalletBalanceObj_Current.Balance,
                                     Witness=new Variable.Struct.WitnessBlock()
                                     {
                                        RowNo=tmpValidatorWalletBalanceObj_Current.RowNo,
                                        UID=tmpValidatorWalletBalanceObj_Current.UID
                                     }
                                }
                            }
                        },
                        After = new Dictionary<string, Dictionary<string, Dictionary<ulong, string>>>()
                        {
                            {
                                multiWalletKey, tmpMultiWalletBalanceObj_New.Balance
                            },
                            {
                                receiverWalletKey,tmpReceiverWalletBalanceObj_New.Balance
                            },
                            {
                                NVG.Settings.NodeWallet.WalletKey,tmpValidatorWalletBalanceObj_New.Balance
                            }
                        },
                        Fee = transferFee.ToString()
                    }
                }
            };
            foreach (KeyValuePair<string, MultiWalletTransactionApproveStruct> entry in uidList[txTime].Approve)
            {
                multiTx[transactionId].Approve.Add(
                    entry.Key,
                    new MultiTransactionApproveStruct()
                    {
                        Approve = entry.Value.Approve,
                        CurrentTime = entry.Value.CurrentTime,
                        PublicKey = entry.Value.PublicKey,
                        Sign = entry.Value.Sign
                    }
                );
            }

            bool tmpAddResult = NGF.BlockQueue.Add(new Notus.Variable.Struct.PoolBlockRecordStruct()
            {
                uid = multiKeyId,
                type = Notus.Variable.Enum.BlockTypeList.MultiWalletCryptoTransfer,
                data = JsonSerializer.Serialize(multiTx)
            });
            if (tmpAddResult == true)
            {
                ObjMp_MultiSignPool.Set(multiKeyId, JsonSerializer.Serialize(uidList));
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 0,
                    ErrorText = "AddedToQueue",
                    Result = Notus.Variable.Enum.BlockStatusCode.AddedToQueue
                });
            }
            NGF.Balance.StopWalletUsage(multiWalletKey);
            NGF.Balance.StopWalletUsage(receiverWalletKey);
            NGF.Balance.StopWalletUsage(validatorWalletKey);
            return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
            {
                ID = string.Empty,
                ErrorNo = 9879871,
                ErrorText = "AnErrorOccurred",
                Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
            });
        }

        private string Request_AddMultiWallet(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            if (IncomeData.PostParams.ContainsKey("data") == false)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "WrongParameter",
                    Result = Notus.Variable.Enum.BlockStatusCode.WrongParameter
                });
            }

            if (NVG.Settings == null)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = Notus.Variable.Enum.BlockStatusCode.Unknown
                });
            }
            if (NVG.Settings.Genesis == null)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = Notus.Variable.Enum.BlockStatusCode.Unknown
                });
            }
            if (NVG.Settings.NodeWallet == null)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = Notus.Variable.Enum.BlockStatusCode.Unknown
                });
            }

            string tmpLockAccountStr = IncomeData.PostParams["data"];
            Notus.Variable.Struct.MultiWalletStruct? WalletObj = JsonSerializer.Deserialize<Notus.Variable.Struct.MultiWalletStruct>(tmpLockAccountStr);
            if (WalletObj == null)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }

            if (2 > WalletObj.WalletList.Count)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "NotEnoughParticipant",
                    Result = Notus.Variable.Enum.BlockStatusCode.NotEnoughParticipant
                });
            }
            if (NGF.Balance.WalletUsageAvailable(WalletObj.Founder.WalletKey) == false)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "WalletUsing",
                    Result = Notus.Variable.Enum.BlockStatusCode.WalletUsing
                });
            }

            if (NGF.Balance.StartWalletUsage(WalletObj.Founder.WalletKey) == false)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }

            BigInteger howMuchCoinNeed = BigInteger.Parse((WalletObj.WalletList.Count * NVG.Settings.Genesis.Fee.MultiWallet.Addition).ToString());
            if (NGF.Balance.HasEnoughCoin(WalletObj.Founder.WalletKey, howMuchCoinNeed) == false)
            {
                NGF.Balance.StopWalletUsage(WalletObj.Founder.WalletKey);
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "InsufficientBalance",
                    Result = Notus.Variable.Enum.BlockStatusCode.InsufficientBalance
                });
            }
            /*
            if (Func_AddToChainPool == null)
            {
                NGF.Balance.StopWalletUsage(WalletObj.Founder.WalletKey);
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = Notus.Variable.Enum.BlockStatusCode.Unknown
                });
            }
            */
            if (Notus.Wallet.ID.CheckAddress(WalletObj.Founder.WalletKey, NVG.Settings.Network) == false)
            {
                NGF.Balance.StopWalletUsage(WalletObj.Founder.WalletKey);
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "WrongWallet",
                    Result = Notus.Variable.Enum.BlockStatusCode.WrongWallet
                });
            }

            Notus.Variable.Struct.WalletBalanceStruct tmpGeneratorBalanceObj =
                NGF.Balance.Get(WalletObj.Founder.WalletKey, 0);

            BigInteger currentVolume = NGF.Balance.GetCoinBalance(
                tmpGeneratorBalanceObj,
                NVG.Settings.Genesis.CoinInfo.Tag
            );

            if (howMuchCoinNeed > currentVolume)
            {
                NGF.Balance.StopWalletUsage(WalletObj.Founder.WalletKey);
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "InsufficientBalance",
                    Result = Notus.Variable.Enum.BlockStatusCode.InsufficientBalance
                });
            }

            // cüzdanın kilitlenme ve açılma işlemleri eklenecek
            (bool volumeError, Notus.Variable.Struct.WalletBalanceStruct newBalance) =
                NGF.Balance.SubtractVolumeWithUnlockTime(
                    tmpGeneratorBalanceObj,
                    howMuchCoinNeed.ToString(),
                    NVG.Settings.Genesis.CoinInfo.Tag
                );
            if (volumeError == true)
            {
                NGF.Balance.StopWalletUsage(WalletObj.Founder.WalletKey);
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }
            string tmpChunkIdKey = Notus.Block.Key.Generate(
                ND.NowObj(),
                NVG.Settings.NodeWallet.WalletKey
            );
            Notus.Variable.Struct.MultiWalletStoreStruct tmpLockObj = new Notus.Variable.Struct.MultiWalletStoreStruct()
            {
                UID = tmpChunkIdKey,
                Founder = new Variable.Struct.MultiWalletFounderStruct()
                {
                    PublicKey = WalletObj.Founder.PublicKey,
                    WalletKey = WalletObj.Founder.WalletKey
                },
                MultiWalletKey = WalletObj.MultiWalletKey,
                VoteType = WalletObj.VoteType,
                WalletList = WalletObj.WalletList,
                Sign = WalletObj.Sign,
                Fee = howMuchCoinNeed.ToString(),
                Balance = tmpGeneratorBalanceObj,
                Out = newBalance.Balance
            };

            bool tmpAddResult = NGF.BlockQueue.Add(new Notus.Variable.Struct.PoolBlockRecordStruct()
            {
                type = Notus.Variable.Enum.BlockTypeList.MultiWalletContract,
                data = JsonSerializer.Serialize(tmpLockObj)
            });
            if (tmpAddResult == true)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = tmpChunkIdKey,
                    Status = "AddedToQueue",
                    Result = Notus.Variable.Enum.BlockStatusCode.AddedToQueue
                });
            }
            NGF.Balance.StopWalletUsage(WalletObj.Founder.WalletKey);
            return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
            {
                UID = string.Empty,
                Status = "Unknown",
                Result = Notus.Variable.Enum.BlockStatusCode.Rejected
            });
        }
        private string Request_LockAccount(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            if (IncomeData.PostParams.ContainsKey("data") == false)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "WrongParameter",
                    Result = Notus.Variable.Enum.BlockStatusCode.WrongParameter
                });
            }

            if (NVG.Settings == null)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = Notus.Variable.Enum.BlockStatusCode.Unknown
                });
            }
            if (NVG.Settings.Genesis == null)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = Notus.Variable.Enum.BlockStatusCode.Unknown
                });
            }
            if (NVG.Settings.NodeWallet == null)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = Notus.Variable.Enum.BlockStatusCode.Unknown
                });
            }

            string tmpLockAccountStr = IncomeData.PostParams["data"];
            Notus.Variable.Struct.LockWalletStruct? LockObj = JsonSerializer.Deserialize<Notus.Variable.Struct.LockWalletStruct>(tmpLockAccountStr);
            if (LockObj == null)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }

            bool hasCoin = NGF.Balance.HasEnoughCoin(
                LockObj.WalletKey,
                BigInteger.Parse(NVG.Settings.Genesis.Fee.BlockAccount.ToString())
            );
            if (hasCoin == false)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "InsufficientBalance",
                    Result = Notus.Variable.Enum.BlockStatusCode.InsufficientBalance
                });
            }

            /*
            if (Func_AddToChainPool == null)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = Notus.Variable.Enum.BlockStatusCode.Unknown
                });
            }
            */
            if (Notus.Wallet.ID.CheckAddress(LockObj.WalletKey, NVG.Settings.Network) == false)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "WrongWallet",
                    Result = Notus.Variable.Enum.BlockStatusCode.WrongWallet
                });
            }

            if (NGF.Balance.WalletUsageAvailable(LockObj.WalletKey) == false)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "WalletUsing",
                    Result = Notus.Variable.Enum.BlockStatusCode.WalletUsing
                });
            }

            if (NGF.Balance.StartWalletUsage(LockObj.WalletKey) == false)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }
            string tmpChunkIdKey = NGF.GenerateTxUid();
            BigInteger howMuchCoinNeed = BigInteger.Parse(NVG.Settings.Genesis.Fee.BlockAccount.ToString());
            Notus.Variable.Struct.WalletBalanceStruct tmpGeneratorBalanceObj = NGF.Balance.Get(LockObj.WalletKey, 0);

            BigInteger currentVolume = NGF.Balance.GetCoinBalance(tmpGeneratorBalanceObj, NVG.Settings.Genesis.CoinInfo.Tag);
            if (howMuchCoinNeed > currentVolume)
            {
                NGF.Balance.StopWalletUsage(LockObj.WalletKey);
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "InsufficientBalance",
                    Result = Notus.Variable.Enum.BlockStatusCode.InsufficientBalance
                });
            }

            Notus.Variable.Struct.LockWalletBeforeStruct tmpLockObj = new Notus.Variable.Struct.LockWalletBeforeStruct()
            {
                UID = tmpChunkIdKey,
                WalletKey = LockObj.WalletKey,
                UnlockTime = LockObj.UnlockTime,
                PublicKey = LockObj.PublicKey,
                Sign = LockObj.Sign
            };

            bool tmpAddResult = NGF.BlockQueue.Add(new Notus.Variable.Struct.PoolBlockRecordStruct()
            {
                uid = tmpChunkIdKey,
                type = Notus.Variable.Enum.BlockTypeList.LockAccount,
                data = JsonSerializer.Serialize(tmpLockObj)
            });
            if (tmpAddResult == true)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = tmpChunkIdKey,
                    Status = "AddedToQueue",
                    Result = Notus.Variable.Enum.BlockStatusCode.AddedToQueue
                });
            }
            NGF.Balance.StopWalletUsage(LockObj.WalletKey);
            return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
            {
                UID = string.Empty,
                Status = "Unknown",
                Result = Notus.Variable.Enum.BlockStatusCode.Rejected
            });
        }

        private string Request_Balance(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            Notus.Variable.Struct.WalletBalanceStruct balanceResult = new Notus.Variable.Struct.WalletBalanceStruct()
            {
                Balance = new Dictionary<string, Dictionary<ulong, string>>(){
                    {
                        NVG.Settings.Genesis.CoinInfo.Tag,
                        new Dictionary<ulong, string>(){
                            {
                                NVG.NOW.Int ,
                                "0"
                            }
                        }
                    }
                },
                UID = "",
                Wallet = IncomeData.UrlList[1],
                RowNo = 0
            }
            ;
            if (IncomeData.UrlList[1].Length == Notus.Variable.Constant.SingleWalletTextLength)
            {
                balanceResult = NGF.Balance.Get(IncomeData.UrlList[1], 0);
            }

            if (PrettyCheckForRaw(IncomeData, 2) == true)
            {
                return JsonSerializer.Serialize(balanceResult, Notus.Variable.Constant.JsonSetting);
            }
            return JsonSerializer.Serialize(balanceResult);
        }

        private string Request_NFTImageList(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            string tmpWalletKey = IncomeData.UrlList[2];

            string tmpListingDir = Notus.IO.GetFolderName(
                NVG.Settings.Network,
                NVG.Settings.Layer,
                Notus.Variable.Constant.StorageFolderName.Storage
            ) + tmpWalletKey + System.IO.Path.DirectorySeparatorChar;
            Notus.IO.CreateDirectory(tmpListingDir);

            List<string> imageListId = new List<string>();
            string[] fileLists = Directory.GetFiles(tmpListingDir, "*.*");
            foreach (string fileName in fileLists)
            {
                string extension = Path.GetExtension(fileName);
                if (string.Equals(".marked", extension) == false)
                {
                    string tmpOnlyFileName = Path.GetFileName(fileName);
                    tmpOnlyFileName = tmpOnlyFileName.Substring(0, tmpOnlyFileName.Length - extension.Length);
                    imageListId.Add(tmpOnlyFileName);
                }
            }
            return JsonSerializer.Serialize(imageListId);
        }

        private string Request_NFTPublicImageDetail_SubFunction(string tmpWalletKey, string tmpStorageId)
        {
            string tmpListingDir = Notus.IO.GetFolderName(
                NVG.Settings.Network,
                NVG.Settings.Layer,
                Notus.Variable.Constant.StorageFolderName.Storage
            ) + tmpWalletKey + System.IO.Path.DirectorySeparatorChar;
            Notus.IO.CreateDirectory(tmpListingDir);

            string[] fileLists = Directory.GetFiles(tmpListingDir, tmpStorageId + ".*");
            foreach (string fileName in fileLists)
            {
                string extension = Path.GetExtension(fileName);
                if (string.Equals(".marked", extension) == true)
                {
                    //string tmpOnlyFileName = fileName.Substring(0, tmpOnlyFileName.Length - extension.Length);
                    using (FileStream reader = new FileStream(fileName, FileMode.Open))
                    {
                        byte[] buffer = new byte[reader.Length];
                        reader.Read(buffer, 0, (int)reader.Length);
                        return System.Convert.ToBase64String(buffer);

                        //burada dosya türü bulunacak ve base64 metni tam olarak yazılı gönderilecek.
                        //burada dosya türü bulunacak ve base64 metni tam olarak yazılı gönderilecek.
                        //burada dosya türü bulunacak ve base64 metni tam olarak yazılı gönderilecek.
                        //return "data:image/" + extension.Substring(1) + ";base64," + Convert.ToBase64String(buffer);
                    }
                }
            }
            return JsonSerializer.Serialize("");
        }
        private string Request_NFTPublicImageDetail(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            return Request_NFTPublicImageDetail_SubFunction(IncomeData.UrlList[2], IncomeData.UrlList[3]);
        }

        private string Request_NFTPrivateImageDetail(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            if (IncomeData.PostParams.ContainsKey("data") == true)
            {
                Notus.Variable.Struct.GenericSignStruct signData = JsonSerializer.Deserialize<Notus.Variable.Struct.GenericSignStruct>(IncomeData.PostParams["data"]);
                string tmpWalletKey = Notus.Wallet.ID.GetAddressWithPublicKey(signData.PublicKey, NVG.Settings.Network);

                string tmpNftStorageId = IncomeData.UrlList[2];
                string publicKey = "";
                string signStr = "";
                string timeStr = "";

                string tmpListingDir = Notus.IO.GetFolderName(
                    NVG.Settings.Network,
                    NVG.Settings.Layer,
                    Notus.Variable.Constant.StorageFolderName.Storage
                ) + tmpWalletKey + System.IO.Path.DirectorySeparatorChar;
                string[] fileLists = Directory.GetFiles(tmpListingDir, tmpNftStorageId + ".*");
                foreach (string fileName in fileLists)
                {
                    string extension = Path.GetExtension(fileName);
                    if (string.Equals(".marked", extension) == false)
                    {

                        using (FileStream reader = new FileStream(fileName, FileMode.Open))
                        {
                            byte[] buffer = new byte[reader.Length];
                            reader.Read(buffer, 0, (int)reader.Length);
                            return "data:image/" + extension.Substring(1) + ";base64," + System.Convert.ToBase64String(buffer);
                        }
                    }
                }
            }
            return JsonSerializer.Serialize("");
        }

        // return metrics and system status
        private string Request_Main()
        {
            if (NVG.Settings.PrettyJson == true)
            {
                return JsonSerializer.Serialize(
                    GiveMeList(
                        Notus.Variable.Enum.NetworkNodeType.Main
                    ), Notus.Variable.Constant.JsonSetting
                );
            }
            return JsonSerializer.Serialize(
                GiveMeList(
                    Notus.Variable.Enum.NetworkNodeType.Main
                )
            );
        }
        private string Request_Replicant()
        {
            if (NVG.Settings.PrettyJson == true)
            {
                return JsonSerializer.Serialize(
                    GiveMeList(
                        Notus.Variable.Enum.NetworkNodeType.Replicant
                    ), Notus.Variable.Constant.JsonSetting
                );
            }
            return JsonSerializer.Serialize(
                GiveMeList(
                    Notus.Variable.Enum.NetworkNodeType.Replicant
                )
            );
        }
        private string Request_Master()
        {
            if (NVG.Settings.PrettyJson == true)
            {
                return JsonSerializer.Serialize(
                    GiveMeList(
                        Notus.Variable.Enum.NetworkNodeType.Master
                    ), Notus.Variable.Constant.JsonSetting
                );
            }
            return JsonSerializer.Serialize(
                GiveMeList(
                    Notus.Variable.Enum.NetworkNodeType.Master
                )
            );
        }
        private string Request_Node()
        {
            if (NVG.Settings.PrettyJson == true)
            {
                return JsonSerializer.Serialize(
                    GiveMeList(
                        Notus.Variable.Enum.NetworkNodeType.All
                    ), Notus.Variable.Constant.JsonSetting
                );
            }
            return JsonSerializer.Serialize(
                GiveMeList(
                    Notus.Variable.Enum.NetworkNodeType.All
                )
            );
        }
        private string Request_Metrics(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {

            if (IncomeData.UrlList.Length > 1)
            {
                if (IncomeData.UrlList[1].ToLower() == "node")
                {
                    UInt64 tmpTotalBlock = (UInt64)GiveMeList(Notus.Variable.Enum.NetworkNodeType.All).Count;
                    if (NVG.Settings.PrettyJson == true)
                    {
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }, Notus.Variable.Constant.JsonSetting);
                    }
                    return JsonSerializer.Serialize(
                        new Notus.Variable.Struct.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }
                    );
                }
                if (IncomeData.UrlList[1].ToLower() == "master")
                {
                    UInt64 tmpTotalBlock = (UInt64)GiveMeList(Notus.Variable.Enum.NetworkNodeType.Master).Count;
                    if (NVG.Settings.PrettyJson == true)
                    {
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }, Notus.Variable.Constant.JsonSetting);
                    }
                    return JsonSerializer.Serialize(
                        new Notus.Variable.Struct.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }
                    );
                }
                if (IncomeData.UrlList[1].ToLower() == "main")
                {
                    UInt64 tmpTotalBlock = (UInt64)GiveMeList(Notus.Variable.Enum.NetworkNodeType.Main).Count;
                    if (NVG.Settings.PrettyJson == true)
                    {
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }, Notus.Variable.Constant.JsonSetting);
                    }
                    return JsonSerializer.Serialize(
                        new Notus.Variable.Struct.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }
                    );
                }
                if (IncomeData.UrlList[1].ToLower() == "replicant")
                {
                    UInt64 tmpTotalBlock = (UInt64)GiveMeList(Notus.Variable.Enum.NetworkNodeType.Replicant).Count;
                    if (NVG.Settings.PrettyJson == true)
                    {
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }, Notus.Variable.Constant.JsonSetting);
                    }
                    return JsonSerializer.Serialize(
                        new Notus.Variable.Struct.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }
                    );
                }
                if (IncomeData.UrlList[1].ToLower() == "block")
                {
                    UInt64 tmpTotalBlock = (UInt64)NVG.Settings.LastBlock.info.rowNo;
                    if (NVG.Settings.PrettyJson == true)
                    {
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }, Notus.Variable.Constant.JsonSetting);
                    }
                    return JsonSerializer.Serialize(
                        new Notus.Variable.Struct.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }
                    );
                }
            }

            return JsonSerializer.Serialize(false);
        }
        private string Request_Online(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            if (PrettyCheckForRaw(IncomeData, 1))
            {
                return JsonSerializer.Serialize(IncomeData, Notus.Variable.Constant.JsonSetting);
            }
            return JsonSerializer.Serialize(IncomeData);
        }

        private int GiveMeCount(Notus.Variable.Enum.NetworkNodeType WhichList)
        {
            if (WhichList == Notus.Variable.Enum.NetworkNodeType.Main)
            {
                return AllMainList.Count;
            }
            if (WhichList == Notus.Variable.Enum.NetworkNodeType.Master)
            {
                return AllMasterList.Count;
            }
            if (WhichList == Notus.Variable.Enum.NetworkNodeType.Replicant)
            {
                return AllReplicantList.Count;
            }
            if (WhichList == Notus.Variable.Enum.NetworkNodeType.Connectable)
            {
                return AllMasterList.Count + AllMainList.Count;
            }

            return AllMasterList.Count + AllMainList.Count + AllReplicantList.Count;
        }

        private List<string> GiveMeList(Notus.Variable.Enum.NetworkNodeType WhichList)
        {
            if (WhichList == Notus.Variable.Enum.NetworkNodeType.Main)
            {
                return AllMainList;
            }
            if (WhichList == Notus.Variable.Enum.NetworkNodeType.Master)
            {
                return AllMasterList;
            }
            if (WhichList == Notus.Variable.Enum.NetworkNodeType.Replicant)
            {
                return AllReplicantList;
            }
            if (WhichList == Notus.Variable.Enum.NetworkNodeType.Connectable)
            {
                List<string> tmpFullList = new List<string>();
                for (int a = 0; a < AllMainList.Count; a++)
                {
                    tmpFullList.Add(AllMainList[a]);
                }
                for (int a = 0; a < AllMasterList.Count; a++)
                {
                    tmpFullList.Add(AllMasterList[a]);
                }
                return tmpFullList;
            }
            if (WhichList == Notus.Variable.Enum.NetworkNodeType.All)
            {
                List<string> tmpFullList = GiveMeList(Notus.Variable.Enum.NetworkNodeType.Connectable);
                for (int a = 0; a < AllReplicantList.Count; a++)
                {
                    tmpFullList.Add(AllReplicantList[a]);
                }
                return tmpFullList;
            }
            return new List<string>();
        }
        public Api()
        {

        }
        ~Api()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (NGF.Balance != null)
            {
                try
                {
                    NGF.Balance.Dispose();
                }
                catch (Exception err)
                {
                }
            }
        }
    }
}
