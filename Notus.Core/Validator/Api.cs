using System;
using System.Numerics;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;

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


        private Notus.Variable.Common.ClassSetting Obj_Settings;
        public Notus.Variable.Common.ClassSetting Settings
        {
            get { return Obj_Settings; }
            set { Obj_Settings = value; }
        }

        private string ValidatorKeyStr = "validator-key";
        public string ValidatorKey
        {
            get { return ValidatorKeyStr; }
            set { ValidatorKeyStr = value; }
        }

        private Notus.Wallet.Balance Obj_Balance = new Notus.Wallet.Balance();
        public Notus.Wallet.Balance BalanceObj
        {
            get { return Obj_Balance; }
            set { Obj_Balance = value; }
        }
        private Notus.Mempool ObjMp_CryptoTranStatus;
        public Notus.Mempool CryptoTranStatus
        {
            get { return ObjMp_CryptoTranStatus; }
            set { ObjMp_CryptoTranStatus = value; }
        }
        private Notus.Mempool ObjMp_CryptoTransfer;
        private Dictionary<string, Notus.Variable.Enum.BlockStatusCode> Obj_TransferStatusList;

        public System.Func<int, List<Notus.Variable.Struct.List_PoolBlockRecordStruct>?>? Func_GetPoolList = null;
        public System.Func<string, Notus.Variable.Class.BlockData?>? Func_OnReadFromChain = null;
        public System.Func<Notus.Variable.Struct.PoolBlockRecordStruct, bool>? Func_AddToChainPool = null;

        private bool PrepareExecuted = false;

        //ffb_CurrencyList Currency list buffer
        private List<Notus.Variable.Struct.CurrencyList> ffb_CurrencyList = new List<Notus.Variable.Struct.CurrencyList>();
        private DateTime ffb_CurrencyList_LastCheck = DateTime.Now.Subtract(TimeSpan.FromDays(1));
        //private bool ffb_CurrencyList_Defined = false;
        private Notus.Variable.Enum.NetworkType ffb_CurrencyList_Network = Notus.Variable.Enum.NetworkType.MainNet;
        private Notus.Variable.Enum.NetworkLayer ffb_CurrencyList_Layer = Notus.Variable.Enum.NetworkLayer.Layer1;

        private void Prepare_Layer1()
        {
            if (Obj_Settings.GenesisCreated == false)
            {
                Obj_TransferStatusList = new Dictionary<string, Notus.Variable.Enum.BlockStatusCode>();
                Obj_Balance.Settings = Obj_Settings;
                Obj_Balance.Start();
                ObjMp_CryptoTransfer = new Notus.Mempool(Notus.IO.GetFolderName(Obj_Settings.Network, Obj_Settings.Layer, Notus.Variable.Constant.StorageFolderName.Common) + "crypto_transfer");
                ObjMp_CryptoTranStatus = new Notus.Mempool(Notus.IO.GetFolderName(Obj_Settings.Network, Obj_Settings.Layer, Notus.Variable.Constant.StorageFolderName.Common) + "crypto_transfer_status");

                ObjMp_CryptoTransfer.AsyncActive = false;
                ObjMp_CryptoTranStatus.AsyncActive = false;
            }
        }
        private void Prepare_Layer2()
        {
            if (Obj_Settings.GenesisCreated == false)
            {
                Obj_TransferStatusList = new Dictionary<string, Notus.Variable.Enum.BlockStatusCode>();
                Obj_Balance.Settings = Obj_Settings;
                Obj_Balance.Start();
                ObjMp_CryptoTransfer = new Notus.Mempool(Notus.IO.GetFolderName(Obj_Settings.Network, Obj_Settings.Layer, Notus.Variable.Constant.StorageFolderName.Common) + "crypto_transfer");
                ObjMp_CryptoTranStatus = new Notus.Mempool(Notus.IO.GetFolderName(Obj_Settings.Network, Obj_Settings.Layer, Notus.Variable.Constant.StorageFolderName.Common) + "crypto_transfer_status");

                ObjMp_CryptoTransfer.AsyncActive = false;
                ObjMp_CryptoTranStatus.AsyncActive = false;
            }
        }
        private void Prepare_Layer3()
        {
            if (Obj_Settings.GenesisCreated == false)
            {
                Obj_TransferStatusList = new Dictionary<string, Notus.Variable.Enum.BlockStatusCode>();
                Obj_Balance.Settings = Obj_Settings;
                Obj_Balance.Start();
                ObjMp_CryptoTransfer = new Notus.Mempool(Notus.IO.GetFolderName(Obj_Settings.Network, Obj_Settings.Layer, Notus.Variable.Constant.StorageFolderName.Common) + "crypto_transfer");
                ObjMp_CryptoTranStatus = new Notus.Mempool(Notus.IO.GetFolderName(Obj_Settings.Network, Obj_Settings.Layer, Notus.Variable.Constant.StorageFolderName.Common) + "crypto_transfer_status");

                ObjMp_CryptoTransfer.AsyncActive = false;
                ObjMp_CryptoTranStatus.AsyncActive = false;
            }
        }
        public void Prepare()
        {
            if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
            {
                Prepare_Layer1();
            }
            if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer2)
            {
                Prepare_Layer2();
            }
            if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer3)
            {
                Prepare_Layer3();
            }

            PrepareExecuted = true;
        }

        public void AddForCache(Notus.Variable.Class.BlockData Obj_BlockData)
        {
            // Console.WriteLine("Api - Line 120");
            // Console.WriteLine(JsonSerializer.Serialize(Obj_Balance));
            // Console.WriteLine(JsonSerializer.Serialize(Obj_BlockData));
            Obj_Balance.Control(Obj_BlockData);
            if (Obj_BlockData.info.type == 120)
            {
                Notus.Variable.Class.BlockStruct_120 tmpBalanceVal = JsonSerializer.Deserialize<Notus.Variable.Class.BlockStruct_120>(System.Text.Encoding.UTF8.GetString(
                    System.Convert.FromBase64String(
                        Obj_BlockData.cipher.data
                    )
                ));

                Console.WriteLine("Node.Api.AddToBalanceDB [cba09834] : " + Obj_BlockData.info.type);
                foreach (KeyValuePair<string, Notus.Variable.Class.BlockStruct_120_In_Struct> entry in tmpBalanceVal.In)
                {
                    RequestSend_Done(entry.Key, Obj_BlockData.info.rowNo, Obj_BlockData.info.uID);
                }
            }
        }

        //layer -1 kontrolünü sağla
        private string Interpret_Layer1(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            return "";
        }

        public string Interpret(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            if (PrepareExecuted == false)
            {
                Prepare();
            }
            //Notus.Print.Basic(Obj_Settings.DebugMode, "Request Income - API Class");
            if (Obj_Settings.DebugMode == true)
            {
                //Console.WriteLine(JsonSerializer.Serialize(IncomeData, new JsonSerializerOptions() { WriteIndented = true }));
            }

            if (IncomeData.UrlList.Length == 0)
            {
                return JsonSerializer.Serialize(false);
            }

            if (IncomeData.UrlList.Length > 2)
            {
                if (string.Equals(IncomeData.UrlList[0].ToLower(), "storage"))
                {
                    //Console.WriteLine(JsonSerializer.Serialize(IncomeData, new JsonSerializerOptions() { WriteIndented = true }));
                    //Console.WriteLine(JsonSerializer.Serialize(IncomeData));
                    if (string.Equals(IncomeData.UrlList[1].ToLower(), "file"))
                    {
                        //this parts need to organize
                        if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
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
                        if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer2)
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

                        if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer3)
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

            if (IncomeData.UrlList.Length > 0)
            {
                if (IncomeData.UrlList[0].ToLower() == "ping")
                {
                    return "pong";
                }
                if (IncomeData.UrlList[0].ToLower() == "metrics")
                {
                    return Request_Metrics(IncomeData);
                }

                if (IncomeData.UrlList[0].ToLower() == "online")
                {
                    return Request_Online(IncomeData);
                }

                if (IncomeData.UrlList[0].ToLower() == "node")
                {
                    return Request_Node();
                }

                if (IncomeData.UrlList[0].ToLower() == "main")
                {
                    return Request_Main();
                }

                if (IncomeData.UrlList[0].ToLower() == "master")
                {
                    return Request_Master();
                }

                if (IncomeData.UrlList[0].ToLower() == "replicant")
                {
                    return Request_Replicant();
                }

                if (IncomeData.UrlList[0].ToLower() == "token")
                {
                    return Request_GenerateToken(IncomeData);
                }

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

                if (IncomeData.UrlList.Length > 1)
                {
                    if (IncomeData.UrlList[0].ToLower() == "pool")
                    {
                        if (int.TryParse(IncomeData.UrlList[1], out int blockTypeNo))
                        {
                            if (Func_GetPoolList != null)
                            {
                                List<Variable.Struct.List_PoolBlockRecordStruct>? tmpPoolList = Func_GetPoolList(blockTypeNo);
                                if (tmpPoolList != null)
                                {
                                    if(tmpPoolList.Count>0)
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
                        return JsonSerializer.Serialize(false);
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
                        if ((DateTime.Now - ffb_CurrencyList_LastCheck).TotalMinutes > 1)
                        {
                            ffb_CurrencyList_LastCheck = DateTime.Now;
                            ffb_CurrencyList = Notus.Wallet.Block.GetList(Obj_Settings.Network, Obj_Settings.Layer);
                        }
                        else
                        {
                            if (Obj_Settings.Network != ffb_CurrencyList_Network || Obj_Settings.Layer != ffb_CurrencyList_Layer)
                            {
                                ffb_CurrencyList = Notus.Wallet.Block.GetList(Obj_Settings.Network, Obj_Settings.Layer);
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

                if (string.Equals(IncomeData.UrlList[0].ToLower(), "testnet"))
                {
                    if (string.Equals(IncomeData.UrlList[1].ToLower(), "airdrop"))
                    {
                        return AirDropRequest(IncomeData);
                    }
                    if (IncomeData.UrlList.Length > 2)
                    {
                        if (string.Equals(IncomeData.UrlList[1].ToLower(), "transfer"))
                        {
                            string tmpPrivateKeyStr = IncomeData.UrlList[2];
                            string tmpReceiverAddress = IncomeData.UrlList[3];
                            string tmpVolume = IncomeData.UrlList[4];

                            string tmpSenderWalletKey = Notus.Wallet.ID.GetAddress(tmpPrivateKeyStr, Obj_Settings.Network);

                            Notus.Variable.Struct.CryptoTransactionStruct tmpSignedTrans = Notus.Wallet.Transaction.Sign(new Notus.Variable.Struct.CryptoTransactionBeforeStruct()
                            {
                                Currency = Obj_Settings.Genesis.CoinInfo.Tag,
                                PrivateKey = tmpPrivateKeyStr,
                                Receiver = tmpReceiverAddress,
                                Sender = tmpSenderWalletKey,
                                Volume = tmpVolume,
                                Network = Obj_Settings.Network,
                                CurveName = Notus.Variable.Constant.Default_EccCurveName
                            }
                            );
                            Notus.Variable.Struct.CryptoTransactionResult tmpSendedTrans = Notus.Wallet.Transaction.Send(tmpSignedTrans, Obj_Settings.Network).GetAwaiter().GetResult();
                            return JsonSerializer.Serialize(tmpSendedTrans);
                        }
                    }
                }

                if (string.Equals(IncomeData.UrlList[0].ToLower(), "info"))
                {
                    if (IncomeData.UrlList.Length > 1)
                    {
                        if (string.Equals(IncomeData.UrlList[1].ToLower(), "genesis"))
                        {
                            return JsonSerializer.Serialize(Obj_Settings.Genesis);
                        }
                        if (string.Equals(IncomeData.UrlList[1].ToLower(), "reserve"))
                        {
                            return JsonSerializer.Serialize(Obj_Settings.Genesis.Reserve);
                        }
                        if (string.Equals(IncomeData.UrlList[1].ToLower(), "transfer"))
                        {
                            return JsonSerializer.Serialize(Obj_Settings.Genesis.Fee);
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
            string tmpKeyPair = string.Empty;
            using (Notus.Mempool ObjMp_Genesis =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(Obj_Settings.Network, Obj_Settings.Layer, Notus.Variable.Constant.StorageFolderName.Common) +
                    "genesis_accounts"
                )
            )
            {
                tmpKeyPair = ObjMp_Genesis.Get("seed_key");
            }
            if (tmpKeyPair.Length == 0)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 6728,
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }
            Notus.Variable.Struct.EccKeyPair? KeyPair_PreSeed = JsonSerializer.Deserialize<Notus.Variable.Struct.EccKeyPair>(tmpKeyPair);
            if (KeyPair_PreSeed == null)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 8259,
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }

            string airdropStr = "2000000";
            if (Notus.Variable.Constant.AirDropVolume.ContainsKey(Obj_Settings.Layer))
            {
                if (Notus.Variable.Constant.AirDropVolume[Obj_Settings.Layer].ContainsKey(Obj_Settings.Network))
                {
                    airdropStr = Notus.Variable.Constant.AirDropVolume[Obj_Settings.Layer][Obj_Settings.Network];
                }
            }
            string ReceiverWalletKey = IncomeData.UrlList[1];
            Notus.Variable.Struct.CryptoTransactionStruct tmpSignedTrans = Notus.Wallet.Transaction.Sign(
                new Notus.Variable.Struct.CryptoTransactionBeforeStruct()
                {
                    Currency = Obj_Settings.Genesis.CoinInfo.Tag,
                    PrivateKey = KeyPair_PreSeed.PrivateKey,
                    Receiver = ReceiverWalletKey,
                    Sender = KeyPair_PreSeed.WalletKey,
                    UnlockTime = Date.ToLong(DateTime.Now),
                    Volume = airdropStr,
                    Network = Obj_Settings.Network,
                    CurveName = Notus.Variable.Constant.Default_EccCurveName
                }
            );
            if (IncomeData.PostParams.ContainsKey("data") == false)
            {
                IncomeData.PostParams.Add("data", "");
            }

            IncomeData.PostParams["data"] = JsonSerializer.Serialize(tmpSignedTrans);
            return Request_Send(IncomeData);
        }
        private (bool, Notus.Variable.Class.BlockData) GetBlockWithRowNo(Int64 BlockRowNo)
        {
            if (Func_OnReadFromChain == null)
            {
                //Console.WriteLine("Func_OnReadFromChain = NULL");
                return (false, null);
            }
            if (Obj_Settings.LastBlock.info.rowNo >= BlockRowNo)
            {
                if (Obj_Settings.LastBlock.info.rowNo == BlockRowNo)
                {
                    return (true, Obj_Settings.LastBlock);
                }
                bool exitPrevWhile = false;
                string PrevBlockIdStr = Obj_Settings.LastBlock.prev;
                while (exitPrevWhile == false)
                {
                    Notus.Variable.Class.BlockData tmpStoredBlock = Func_OnReadFromChain(PrevBlockIdStr.Substring(0, 90));
                    if (tmpStoredBlock != null)
                    {
                        if (tmpStoredBlock.info.rowNo == BlockRowNo)
                        {
                            return (true, tmpStoredBlock);
                        }
                        PrevBlockIdStr = tmpStoredBlock.prev;
                    }
                    else
                    {
                        exitPrevWhile = true;
                    }
                }
            }
            return (false, null);
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
                Notus.Print.Danger(Obj_Settings, "Error Text [a46cbe8d9] : " + err.Message);
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
                        Obj_Settings.Network,
                        Obj_Settings.Layer,
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
                        Obj_Settings.Network,
                        Obj_Settings.Layer,
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
                                    Obj_Settings.Network,
                                    Obj_Settings.Layer,
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
                Notus.Print.Danger(Obj_Settings, "Error Text [a354cd67] : " + err.Message);
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }

            string tmpStorageIdKey = tmpChunkData.UID;
            string tmpChunkIdKey = Notus.Block.Key.Generate(GetNtpTime(), Obj_Settings.NodeWallet.WalletKey);
            int tmpStorageNo = Notus.Block.Key.CalculateStorageNumber(Notus.Convert.Hex2BigInteger(tmpChunkIdKey).ToString());

            using (Notus.Mempool ObjMp_FileChunkList =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        Obj_Settings.Network,
                        Obj_Settings.Layer,
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
                        Obj_Settings.Network,
                        Obj_Settings.Layer,
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
                //Console.WriteLine("([" + tmpCurrentList + "])");
                //Console.WriteLine(tmpCurrentList.Length);
                if (tmpCurrentList.Length > 0)
                {
                    try
                    {
                        tmpChunkList = JsonSerializer.Deserialize<Dictionary<int, string>>(tmpCurrentList);
                    }
                    catch
                    {
                        Console.WriteLine("Notus.Validator.Api.Request_StoreFileUpdate -> Convertion Error - Line 595");
                        Console.WriteLine("Notus.Validator.Api.Request_StoreFileUpdate -> Convertion Error - Line 595");
                    }
                }
                //Console.WriteLine(tmpChunkIdKey);
                //Console.WriteLine(JsonSerializer.Serialize(tmpChunkList));
                tmpChunkList.Add(tmpChunkData.Count, tmpChunkIdKey);
                ObjMp_FileList.Set(tmpStorageIdKey + "_chunk", JsonSerializer.Serialize(tmpChunkList));

                //Console.WriteLine(calculatedChunkLength.ToString() + " -> " + tmpChunkData.Count.ToString());
                if (calculatedChunkLength == tmpChunkData.Count)
                {
                    //Console.WriteLine("Status Update Key : " + tmpStorageIdKey);
                    using (Notus.Mempool ObjMp_FileStatus =
                        new Notus.Mempool(
                            Notus.IO.GetFolderName(
                                Obj_Settings.Network,
                                Obj_Settings.Layer,
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
                        Obj_Settings.Network,
                        Obj_Settings.Layer,
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
                catch
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
                Notus.Print.Danger(Obj_Settings, "Error Text [a46cbe8d9] : " + err.Message);
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }

            string tmpTransferIdKey = Notus.Block.Key.Generate(GetNtpTime(), Obj_Settings.NodeWallet.WalletKey);
            using (Notus.Mempool ObjMp_FileChunkList =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        Obj_Settings.Network,
                        Obj_Settings.Layer,
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
                        Obj_Settings.Network,
                        Obj_Settings.Layer,
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
            //Console.WriteLine(JsonSerializer.Serialize(IncomeData.PostParams, new JsonSerializerOptions() { WriteIndented = true }));
            */
            try
            {
                tmpChunkData = JsonSerializer.Deserialize<Notus.Variable.Struct.FileChunkStruct>(System.Uri.UnescapeDataString(IncomeData.PostParams["data"]));
            }
            catch (Exception err)
            {
                Notus.Print.Danger(Obj_Settings, "Error Text [a354cd67] : " + err.Message);
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }

            string tmpStorageIdKey = tmpChunkData.UID;
            string tmpChunkIdKey = Notus.Block.Key.Generate(GetNtpTime(), Obj_Settings.NodeWallet.WalletKey);
            int tmpStorageNo = Notus.Block.Key.CalculateStorageNumber(Notus.Convert.Hex2BigInteger(tmpChunkIdKey).ToString());

            using (Notus.Mempool ObjMp_FileChunkList =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        Obj_Settings.Network,
                        Obj_Settings.Layer,
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
                        Obj_Settings.Network,
                        Obj_Settings.Layer,
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
                //Console.WriteLine("([" + tmpCurrentList + "])");
                //Console.WriteLine(tmpCurrentList.Length);
                if (tmpCurrentList.Length > 0)
                {
                    try
                    {
                        tmpChunkList = JsonSerializer.Deserialize<Dictionary<int, string>>(tmpCurrentList);
                    }
                    catch
                    {
                        Console.WriteLine("Notus.Validator.Api.Request_StoreFileUpdate -> Convertion Error - Line 595");
                        Console.WriteLine("Notus.Validator.Api.Request_StoreFileUpdate -> Convertion Error - Line 595");
                    }
                }
                //Console.WriteLine(tmpChunkIdKey);
                //Console.WriteLine(JsonSerializer.Serialize(tmpChunkList));
                tmpChunkList.Add(tmpChunkData.Count, tmpChunkIdKey);
                ObjMp_FileList.Set(tmpStorageIdKey + "_chunk", JsonSerializer.Serialize(tmpChunkList));

                //Console.WriteLine(calculatedChunkLength.ToString() + " -> " + tmpChunkData.Count.ToString());
                if (calculatedChunkLength == tmpChunkData.Count)
                {
                    //Console.WriteLine("Status Update Key : " + tmpStorageIdKey);
                    using (Notus.Mempool ObjMp_FileStatus =
                        new Notus.Mempool(
                            Notus.IO.GetFolderName(
                                Obj_Settings.Network,
                                Obj_Settings.Layer,
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
                        Obj_Settings.Network,
                        Obj_Settings.Layer,
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
                catch
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
                Notus.Print.Danger(Obj_Settings, "Error Text [bad849506] : " + err.Message);
                return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }

            //Console.WriteLine(JsonSerializer.Serialize(Obj_Settings.Genesis.Fee, new JsonSerializerOptions() { WriteIndented = true }));
            //Console.WriteLine("Control_Point_4-a");
            // 1500 * 44304
            long StorageFee = Obj_Settings.Genesis.Fee.Data * tmpStorageData.Size;
            if (tmpStorageData.Encrypted == true)
            {
                StorageFee = StorageFee * 2;
            }

            string tmpWalletKey = Notus.Wallet.ID.GetAddressWithPublicKey(tmpStorageData.PublicKey);
            Notus.Variable.Struct.WalletBalanceStruct tmpWalletBalance = Obj_Balance.Get(tmpWalletKey);
            
            BigInteger tmpCurrentBalance = Obj_Balance.GetCoinBalance(tmpWalletBalance, Notus.Variable.Struct.MainCoinTagName);
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

            tmpWalletBalance.Balance[Obj_Settings.Genesis.CoinInfo.Tag] = tmpCoinLeft.ToString();

            tmpStorageData.Balance.Balance = tmpWalletBalance.Balance;
            tmpStorageData.Balance.RowNo = tmpWalletBalance.RowNo;
            tmpStorageData.Balance.UID = tmpWalletBalance.UID;
            tmpStorageData.Balance.Wallet = tmpWalletBalance.Wallet;
            tmpStorageData.Balance.Fee = StorageFee.ToString();

            Console.WriteLine(JsonSerializer.Serialize(tmpStorageData, new JsonSerializerOptions() { WriteIndented = true }));

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
                        Obj_Settings.Network,
                        Obj_Settings.Layer,
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
                catch
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

        private string Request_Send(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            Notus.Variable.Struct.CryptoTransactionStruct? tmpTransfer;
            try
            {
                tmpTransfer = JsonSerializer.Deserialize<Notus.Variable.Struct.CryptoTransactionStruct>(IncomeData.PostParams["data"]);
            }
            catch (Exception err)
            {
                Notus.Print.Danger(Obj_Settings, "Error Text [abc875768] : " + err.Message);
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 9618,
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }
            if (tmpTransfer == null)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 78945,
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred
                });
            }

            if (
                tmpTransfer.Volume == null ||
                tmpTransfer.Sign == null ||
                tmpTransfer.PublicKey == null ||
                tmpTransfer.Sender == null ||
                tmpTransfer.Currency == null ||
                tmpTransfer.Receiver == null
            )
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 4928,
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.WrongParameter
                });
            }

            const int transferTimeOut = 0;
            if (tmpTransfer.Sender.Length != Notus.Variable.Constant.SingleWalletTextLength)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 7546,
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
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.WrongWallet_Receiver
                });
            }

            if (string.Equals(tmpTransfer.Receiver, tmpTransfer.Sender))
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 5245,
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.WrongWallet_Receiver
                });
            }

            if (Int64.TryParse(tmpTransfer.Volume, out _) == false)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 3652,
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.WrongVolume
                });
            }

            // burada gelen bakiyeyi zaman kiliti ile kontrol edecek.
            Notus.Variable.Struct.WalletBalanceStruct tmpSenderBalanceObj = Obj_Balance.Get(tmpTransfer.Sender, 0);

            if (tmpSenderBalanceObj.Balance.ContainsKey(tmpTransfer.Currency) == false)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 7854,
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.InsufficientBalance
                });
            }

            // if wallet wants to send coin then control only coin balance
            Int64 transferFee = Notus.Wallet.Fee.Calculate(
                Notus.Variable.Enum.Fee.CryptoTransfer,
                Obj_Settings.Network,
                Obj_Settings.Layer
            );
            if (string.Equals(tmpTransfer.Currency, Obj_Settings.Genesis.CoinInfo.Tag))
            {
                BigInteger RequiredBalanceInt = BigInteger.Parse(tmpTransfer.Volume) + transferFee;
                BigInteger CoinBalanceInt = Obj_Balance.GetCoinBalance(tmpSenderBalanceObj, Obj_Settings.Genesis.CoinInfo.Tag);

                if (RequiredBalanceInt > CoinBalanceInt)
                {
                    return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                    {
                        ErrorNo = 2536,
                        ID = string.Empty,
                        Result = Notus.Variable.Enum.BlockStatusCode.InsufficientBalance
                    });
                }
            }
            else
            {
                if (tmpSenderBalanceObj.Balance.ContainsKey(Obj_Settings.Genesis.CoinInfo.Tag) == false)
                {
                    return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                    {
                        ErrorNo = 7854,
                        ID = string.Empty,
                        Result = Notus.Variable.Enum.BlockStatusCode.InsufficientBalance
                    });
                }
                BigInteger coinFeeBalance = Obj_Balance.GetCoinBalance(tmpSenderBalanceObj, Obj_Settings.Genesis.CoinInfo.Tag);
                if (transferFee > coinFeeBalance)
                {
                    return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                    {
                        ErrorNo = 7523,
                        ID = string.Empty,
                        Result = Notus.Variable.Enum.BlockStatusCode.InsufficientBalance
                    });
                }
                BigInteger tokenCurrentBalance = Obj_Balance.GetCoinBalance(tmpSenderBalanceObj, tmpTransfer.Currency);
                BigInteger RequiredBalanceInt = BigInteger.Parse(tmpTransfer.Volume);
                if (RequiredBalanceInt > tokenCurrentBalance)
                {
                    return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                    {
                        ErrorNo = 2365,
                        ID = string.Empty,
                        Result = Notus.Variable.Enum.BlockStatusCode.InsufficientBalance
                    });
                }
            }

            //transaction sign
            if (
                Notus.Wallet.ID.Verify(
                    Notus.Core.MergeRawData.Transaction(
                        tmpTransfer.Sender,
                        tmpTransfer.Receiver,
                        tmpTransfer.Volume,
                        tmpTransfer.UnlockTime.ToString(),
                        tmpTransfer.Currency
                    ),
                    tmpTransfer.Sign,
                    tmpTransfer.PublicKey
                ) == false)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 7314,
                    ID = string.Empty,
                    Result = Notus.Variable.Enum.BlockStatusCode.WrongSignature
                });
            }

            // transfer process status is saved
            string tmpTransferIdKey = Notus.Block.Key.Generate(GetNtpTime(), Obj_Settings.NodeWallet.WalletKey);
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

            // controlpoint
            Notus.Variable.Struct.CryptoTransactionStoreStruct recordStruct = new Notus.Variable.Struct.CryptoTransactionStoreStruct()
            {
                Version = 1000,
                TransferId = tmpTransferIdKey,
                UnlockTime = tmpTransfer.UnlockTime,
                Currency = tmpTransfer.Currency,
                Sender = tmpTransfer.Sender,
                Receiver = tmpTransfer.Receiver,
                Volume = tmpTransfer.Volume,
                Fee = transferFee.ToString(),
                PublicKey = tmpTransfer.PublicKey,
                Sign = tmpTransfer.Sign,
            };
            //Console.WriteLine("Notus.Node.Validator.Api -> Line 546");
            //Console.WriteLine(JsonSerializer.Serialize(recordStruct, new JsonSerializerOptions() { WriteIndented = true }));

            // transfer data saved for next step
            ObjMp_CryptoTransfer.Add(tmpTransferIdKey, JsonSerializer.Serialize(recordStruct), transferTimeOut);

            Obj_TransferStatusList.Add(tmpTransferIdKey, Notus.Variable.Enum.BlockStatusCode.AddedToQueue);

            if (Obj_Settings.PrettyJson == true)
            {
                return JsonSerializer.Serialize(
                    new Notus.Variable.Struct.CryptoTransactionResult()
                    {
                        ErrorNo = 0,
                        ID = tmpTransferIdKey,
                        Result = Notus.Variable.Enum.BlockStatusCode.AddedToQueue,
                    },
                    new JsonSerializerOptions()
                    {
                        WriteIndented = true
                    }
                );
            }
            return JsonSerializer.Serialize(
                new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 0,
                    ID = tmpTransferIdKey,
                    Result = Notus.Variable.Enum.BlockStatusCode.AddedToQueue,
                }
            );
        }
        public int RequestSend_ListCount()
        {
            return ObjMp_CryptoTransfer.Count();
        }
        public System.Collections.Generic.Dictionary<string, Notus.Variable.Struct.MempoolDataList> RequestSend_DataList()
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
            bool prettyJson = Obj_Settings.PrettyJson;
            if (IncomeData.UrlList.Length > 2)
            {
                if (string.Equals(IncomeData.UrlList[2].ToLower(), "raw"))
                {
                    prettyJson = false;
                }
            }
            if (IncomeData.UrlList[1].Length == 90)
            {
                try
                {
                    Notus.Variable.Class.BlockData tmpStoredBlock = Func_OnReadFromChain(IncomeData.UrlList[1]);
                    if (tmpStoredBlock != null)
                    {
                        if (prettyJson == true)
                        {
                            return JsonSerializer.Serialize(tmpStoredBlock, new JsonSerializerOptions() { WriteIndented = true });
                        }
                        return JsonSerializer.Serialize(tmpStoredBlock);
                    }
                }
                catch (Exception err)
                {
                    Notus.Print.Danger(Obj_Settings, "Error Text [4a821b]: " + err.Message);
                    return JsonSerializer.Serialize(false);
                }
            }

            Int64 BlockNumber = 0;
            bool isNumeric = Int64.TryParse(IncomeData.UrlList[1], out BlockNumber);
            if (isNumeric == true)
            {
                (bool blockFound, Notus.Variable.Class.BlockData tmpResultBlock) = GetBlockWithRowNo(BlockNumber);
                if (blockFound == true)
                {
                    if (prettyJson == true)
                    {
                        return JsonSerializer.Serialize(tmpResultBlock, new JsonSerializerOptions() { WriteIndented = true });
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
                    Notus.Variable.Class.BlockData tmpStoredBlock = Func_OnReadFromChain(IncomeData.UrlList[2]);
                    if (tmpStoredBlock != null)
                    {
                        return tmpStoredBlock.info.uID + tmpStoredBlock.sign;
                    }
                }
                catch (Exception err)
                {
                    Notus.Print.Danger(Obj_Settings, "Error Text [1f95ce]: " + err.Message);
                }
                return JsonSerializer.Serialize(false);
            }
            Int64 BlockNumber2 = 0;
            bool isNumeric2 = Int64.TryParse(IncomeData.UrlList[2], out BlockNumber2);
            if (isNumeric2 == true)
            {
                (bool blockFound, Notus.Variable.Class.BlockData tmpResultBlock) = GetBlockWithRowNo(BlockNumber2);
                if (blockFound == true)
                {
                    return tmpResultBlock.info.uID + tmpResultBlock.sign;
                }
            }
            return JsonSerializer.Serialize(false);
        }
        private bool PrettyCheckForRaw(Notus.Variable.Struct.HttpRequestDetails IncomeData,int indexNo)
        {
            bool prettyJson = Obj_Settings.PrettyJson;
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
                return JsonSerializer.Serialize(Obj_Settings.LastBlock, new JsonSerializerOptions() { WriteIndented = true });
            }
            return JsonSerializer.Serialize(Obj_Settings.LastBlock);
        }
        private string Request_BlockSummary(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            if (PrettyCheckForRaw(IncomeData, 2) == true)
            {
                return JsonSerializer.Serialize(new Notus.Variable.Struct.LastBlockInfo()
                {
                    RowNo = Obj_Settings.LastBlock.info.rowNo,
                    uID = Obj_Settings.LastBlock.info.uID,
                    Sign = Obj_Settings.LastBlock.sign
                }, new JsonSerializerOptions() { WriteIndented = true });
            }
            return JsonSerializer.Serialize(new Notus.Variable.Struct.LastBlockInfo()
            {
                RowNo = Obj_Settings.LastBlock.info.rowNo,
                uID = Obj_Settings.LastBlock.info.uID,
                Sign = Obj_Settings.LastBlock.sign
            });
        }
        private string Request_GenerateToken(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            if (IncomeData.UrlList.Length > 2)
            {
                if (IncomeData.UrlList[1].ToLower() != "generate")
                {
                    return JsonSerializer.Serialize(false);
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

                try
                {
                    string tmpTokenStr = IncomeData.PostParams["data"];
                    const int transferTimeOut = 86400;
                    string CurrentCurrency = Obj_Settings.Genesis.CoinInfo.Tag;
                    Notus.Variable.Struct.WalletBalanceStruct tmpGeneratorBalanceObj = Obj_Balance.Get(WalletKeyStr, 0);
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

                    if (Notus.Wallet.Block.Exist(Obj_Settings.Network, Obj_Settings.Layer, tmpTokenObj.Info.Tag) == true)
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

                    string tmpOwnerWalletStr = Notus.Wallet.ID.GetAddressWithPublicKey(tmpTokenObj.Creation.PublicKey, Obj_Settings.Network);
                    if (string.Equals(WalletKeyStr, tmpOwnerWalletStr) == false)
                    {
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponseStruct()
                        {
                            UID = "",
                            Code = Notus.Variable.Constant.ErrorNoList.WrongAccount,
                            Status = "WrongAccount"
                        });
                    }

                    BigInteger WalletBalanceInt = Obj_Balance.GetCoinBalance(tmpGeneratorBalanceObj, Obj_Settings.Genesis.CoinInfo.Tag);
                    Int64 tmpFeeVolume = Notus.Wallet.Fee.Calculate(tmpTokenObj, Obj_Settings.Network);
                    if (tmpFeeVolume > WalletBalanceInt)
                    {
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponseStruct()
                        {
                            UID = "",
                            Code = Notus.Variable.Constant.ErrorNoList.NeedCoin,
                            Status = "NeedCoin"
                        });
                    }

                    if (Func_AddToChainPool != null)
                    {
                        // buraya token sahibinin önceki bakiyesi yazılacak,
                        // burada out ile nihai bakiyede belirtilecek
                        // tmpTokenObj.Validator = Obj_Settings.NodeWallet.WalletKey;
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
                            NodeWallet = Obj_Settings.NodeWallet.WalletKey,
                            Reward = tmpFeeVolume.ToString()
                        };
                        (bool tmpBalanceResult, Notus.Variable.Struct.WalletBalanceStruct tmpNewGeneratorBalance) =
                            BalanceObj.SubtractVolumeWithUnlockTime(
                                Obj_Balance.Get(WalletKeyStr, 0),
                                tmpFeeVolume.ToString(),
                                Obj_Settings.Genesis.CoinInfo.Tag,
                                0
                            );

                        tmpTokenObj.Out = tmpNewGeneratorBalance.Balance;
                        bool tmpAddResult = Func_AddToChainPool(new Notus.Variable.Struct.PoolBlockRecordStruct()
                        {
                            type = 160,
                            data = JsonSerializer.Serialize(tmpTokenObj)
                        });

                        return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponseStruct()
                        {
                            UID = tmpTokenObj.Creation.UID,
                            Code = Notus.Variable.Constant.ErrorNoList.AddedToQueue,
                            Status = "AddedToQueue"
                        });
                    }

                    return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponseStruct()
                    {
                        UID = "",
                        Code = Notus.Variable.Constant.ErrorNoList.UnknownError,
                        Status = "UnknownError"
                    });
                }
                catch (Exception err)
                {
                    Console.WriteLine("Notus.Validator.Api - Line 843 [ 897abcd ] : " + err.Message);
                    return JsonSerializer.Serialize(new Notus.Variable.Struct.BlockResponseStruct()
                    {
                        UID = "",
                        Code = Notus.Variable.Constant.ErrorNoList.UnknownError,
                        Status = "UnknownError"
                    });
                }
            }
            return JsonSerializer.Serialize(false);
        }

        private string Request_Balance(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            bool prettyJson = Obj_Settings.PrettyJson;
            if (IncomeData.UrlList.Length > 2)
            {
                if (string.Equals(IncomeData.UrlList[2] , "raw"))
                {
                    prettyJson = false;
                }
            }

            Notus.Variable.Struct.WalletBalanceStruct balanceResult = new Notus.Variable.Struct.WalletBalanceStruct()
            {
                Balance = new Dictionary<string, Dictionary<ulong, string>>(){
                        {
                            Obj_Settings.Genesis.CoinInfo.Tag,
                            new Dictionary<ulong, string>(){
                                {
                                    Notus.Time.NowToUlong() ,
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
                balanceResult = Obj_Balance.Get(IncomeData.UrlList[1], 0);
            }

            if (prettyJson == true)
            {
                return JsonSerializer.Serialize(balanceResult, new JsonSerializerOptions() { WriteIndented = true });
            }
            return JsonSerializer.Serialize(balanceResult);
        }

        private string Request_NFTImageList(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            string tmpWalletKey = IncomeData.UrlList[2];

            string tmpListingDir = Notus.IO.GetFolderName(
                Obj_Settings.Network,
                Obj_Settings.Layer,
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
                Obj_Settings.Network,
                Obj_Settings.Layer,
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
                string tmpWalletKey = Notus.Wallet.ID.GetAddressWithPublicKey(signData.PublicKey, Obj_Settings.Network);

                string tmpNftStorageId = IncomeData.UrlList[2];
                string publicKey = "";
                string signStr = "";
                string timeStr = "";

                string tmpListingDir = Notus.IO.GetFolderName(
                    Obj_Settings.Network,
                    Obj_Settings.Layer,
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
            if (Obj_Settings.PrettyJson == true)
            {
                return JsonSerializer.Serialize(
                    GiveMeList(
                        Notus.Variable.Enum.NetworkNodeType.Main
                    ), new JsonSerializerOptions() { WriteIndented = true }
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
            if (Obj_Settings.PrettyJson == true)
            {
                return JsonSerializer.Serialize(
                    GiveMeList(
                        Notus.Variable.Enum.NetworkNodeType.Replicant
                    ), new JsonSerializerOptions() { WriteIndented = true }
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
            if (Obj_Settings.PrettyJson == true)
            {
                return JsonSerializer.Serialize(
                    GiveMeList(
                        Notus.Variable.Enum.NetworkNodeType.Master
                    ), new JsonSerializerOptions() { WriteIndented = true }
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
            if (Obj_Settings.PrettyJson == true)
            {
                return JsonSerializer.Serialize(
                    GiveMeList(
                        Notus.Variable.Enum.NetworkNodeType.All
                    ), new JsonSerializerOptions() { WriteIndented = true }
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
                    if (Obj_Settings.PrettyJson == true)
                    {
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }, new JsonSerializerOptions() { WriteIndented = true });
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
                    if (Obj_Settings.PrettyJson == true)
                    {
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }, new JsonSerializerOptions() { WriteIndented = true });
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
                    if (Obj_Settings.PrettyJson == true)
                    {
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }, new JsonSerializerOptions() { WriteIndented = true });
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
                    if (Obj_Settings.PrettyJson == true)
                    {
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }, new JsonSerializerOptions() { WriteIndented = true });
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
                    UInt64 tmpTotalBlock = (UInt64)Obj_Settings.LastBlock.info.rowNo;
                    if (Obj_Settings.PrettyJson == true)
                    {
                        return JsonSerializer.Serialize(new Notus.Variable.Struct.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }, new JsonSerializerOptions() { WriteIndented = true });
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
            if (Obj_Settings.PrettyJson == true)
            {
                return JsonSerializer.Serialize(IncomeData, new JsonSerializerOptions() { WriteIndented = true });
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
        private DateTime GetNtpTime()
        {
            if (
                string.Equals(
                    LastNtpTime.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText),
                    Notus.Variable.Constant.DefaultTime.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText)
                )
            )
            {
                LastNtpTime = Notus.Time.GetFromNtpServer();
                DateTime tmpNtpCheckTime = DateTime.Now;
                NodeTimeAfterNtpTime = (tmpNtpCheckTime > LastNtpTime);
                NtpTimeDifference = (NodeTimeAfterNtpTime == true ? (tmpNtpCheckTime - LastNtpTime) : (LastNtpTime - tmpNtpCheckTime));
                return LastNtpTime;
            }

            if (NodeTimeAfterNtpTime == true)
            {
                LastNtpTime = DateTime.Now.Subtract(NtpTimeDifference);
                return LastNtpTime;
            }
            LastNtpTime = DateTime.Now.Add(NtpTimeDifference);
            return LastNtpTime;
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
            if (Obj_Balance != null)
            {
                try
                {
                    Obj_Balance.Dispose();
                }
                catch { }
            }
        }
    }
}
