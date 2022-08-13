using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Numerics;
using System.Text.Json;

namespace Notus.Validator
{
    public class Main : IDisposable
    {
        private Notus.Variable.Common.ClassSetting Obj_Settings;
        public Notus.Variable.Common.ClassSetting Settings
        {
            get { return Obj_Settings; }
            set { Obj_Settings = value; }
        }

        //this variable hold current processing block number
        private long CurrentBlockRowNo = 1;
        private int SelectedPortVal = 0;

        //bu nesnenin görevi network'e bağlı nodeların listesini senkronize etmek
        //private Notus.Network.Controller ControllerObj = new Notus.Network.Controller();
        private Notus.Communication.Http HttpObj = new Notus.Communication.Http(true);
        private Notus.Block.Integrity Obj_Integrity;
        private Notus.Validator.Api Obj_Api;
        //private Notus.Cache.Main Obj_MainCache;
        //private Notus.Token.Storage Obj_TokenStorage;

        //blok durumlarını tutan değişken
        private Dictionary<string, Notus.Variable.Struct.BlockStatus> Obj_BlockStatusList = new Dictionary<string, Notus.Variable.Struct.BlockStatus>();
        public Dictionary<string, Notus.Variable.Struct.BlockStatus> BlockStatusList
        {
            get { return BlockStatusList; }
        }

        private bool CryptoTransferTimerIsRunning = false;
        private DateTime CryptoTransferTime = DateTime.Now;

        private bool EmptyBlockNotMyTurnPrinted = false;
        private bool EmptyBlockTimerIsRunning = false;
        private DateTime EmptyBlockGeneratedTime = new DateTime(2000, 01, 1, 0, 00, 00);

        private bool FileStorageTimerIsRunning = false;
        private DateTime FileStorageTime = DateTime.Now;

        //bu liste diğer nodelardan gelen yeni blokları tutan liste
        public SortedDictionary<long, Notus.Variable.Class.BlockData> IncomeBlockList = new SortedDictionary<long, Notus.Variable.Class.BlockData>();
        //public ConcurrentQueue<Notus.Variable.Class.BlockData> IncomeBlockList = new ConcurrentQueue<Notus.Variable.Class.BlockData>();
        private Notus.Block.Queue Obj_BlockQueue = new Notus.Block.Queue();
        private Notus.Validator.Queue ValidatorQueueObj = new Notus.Validator.Queue();

        //private System.Action<string, Notus.Variable.Class.BlockData> OnReadFromChainFuncObj = null;
        public void EmptyBlockTimerFunc()
        {
            Notus.Print.Basic(Obj_Settings, "Empty Block Timer Has Started");
            Notus.Threads.Timer TimerObj = new Notus.Threads.Timer(1000);
            TimerObj.Start(() =>
            {
                if (EmptyBlockTimerIsRunning == false)
                {
                    EmptyBlockTimerIsRunning = true;
                    int howManySeconds = Obj_Settings.Genesis.Empty.Interval.Time;

                    if (Obj_Settings.Genesis.Empty.SlowBlock.Count >= Obj_Integrity.EmptyBlockCount)
                    {
                        howManySeconds = (
                            Obj_Settings.Genesis.Empty.Interval.Time
                                *
                            Obj_Settings.Genesis.Empty.SlowBlock.Multiply
                        );
                    }

                    //blok zamanı ve utc zamanı çakışıyor
                    DateTime tmpLastTime = Notus.Date.ToDateTime(
                        Obj_Settings.LastBlock.info.time
                    ).AddSeconds(howManySeconds);

                    // get utc time from validatır Queue
                    DateTime utcTime = ValidatorQueueObj.GetUtcTime();
                    if (utcTime > tmpLastTime)
                    {
                        if (ValidatorQueueObj.MyTurn)
                        {
                            if ((DateTime.Now - EmptyBlockGeneratedTime).TotalSeconds > 30)
                            {
                                //Console.WriteLine((DateTime.Now - EmptyBlockGeneratedTime).TotalSeconds);
                                EmptyBlockGeneratedTime = DateTime.Now;
                                Notus.Print.Success(Obj_Settings, "Empty Block Executed");
                                Obj_BlockQueue.AddEmptyBlock();
                            }
                            EmptyBlockNotMyTurnPrinted = false;
                        }
                        else
                        {
                            if (EmptyBlockNotMyTurnPrinted == false)
                            {
                                //Notus.Print.Warning(Obj_Settings, "Not My Turn For Empty Block");
                                EmptyBlockNotMyTurnPrinted = true;
                            }
                        }
                        EmptyBlockTimerIsRunning = false;
                    }
                }
            }, true);
        }
        public void FileStorageTimer()
        {
            Notus.Print.Basic(Obj_Settings, "File Storage Timer Has Started");

            Notus.Threads.Timer TimerObj = new Notus.Threads.Timer(2000);
            TimerObj.Start(() =>
            {
                if (FileStorageTimerIsRunning == false)
                {
                    FileStorageTimerIsRunning = true;
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
                        ObjMp_FileStatus.Each((string tmpStorageId, string rawStatusStr) =>
                        {
                            Notus.Variable.Enum.BlockStatusCode tmpDataStatus = JsonSerializer.Deserialize<Notus.Variable.Enum.BlockStatusCode>(rawStatusStr);
                            if (tmpDataStatus == Notus.Variable.Enum.BlockStatusCode.Pending)
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

                                    string tmpStorageStructStr = ObjMp_FileList.Get(tmpStorageId, "");
                                    Notus.Variable.Struct.FileTransferStruct tmpFileObj = JsonSerializer.Deserialize<Notus.Variable.Struct.FileTransferStruct>(tmpStorageStructStr);

                                    string tmpCurrentList = ObjMp_FileList.Get(tmpStorageId + "_chunk", "");
                                    //try
                                    //{
                                    string tmpWalletKey = Notus.Wallet.ID.GetAddressWithPublicKey(tmpFileObj.PublicKey, Obj_Settings.Network);
                                    string tmpOutputFolder = Notus.IO.GetFolderName(
                                        Obj_Settings.Network,
                                        Obj_Settings.Layer,
                                        Notus.Variable.Constant.StorageFolderName.Storage
                                    ) + tmpWalletKey + System.IO.Path.DirectorySeparatorChar +
                                    tmpStorageId + System.IO.Path.DirectorySeparatorChar;
                                    Notus.IO.CreateDirectory(tmpOutputFolder);
                                    string outputFileName = tmpOutputFolder + tmpFileObj.FileName;
                                    using (FileStream fs = new FileStream(outputFileName, FileMode.Create, FileAccess.ReadWrite))
                                    {
                                        Dictionary<int, string> tmpChunkList = JsonSerializer.Deserialize<Dictionary<int, string>>(tmpCurrentList);
                                        foreach (KeyValuePair<int, string> entry in tmpChunkList)
                                        {
                                            string tmpChunkIdKey = entry.Value;
                                            int tmpStorageNo = Notus.Block.Key.CalculateStorageNumber(
                                                Notus.Convert.Hex2BigInteger(tmpChunkIdKey).ToString()
                                            );
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
                                                string tmpRawDataStr = ObjMp_FileChunkList.Get(tmpChunkIdKey);
                                                byte[] tmpByteBuffer = System.Convert.FromBase64String(System.Uri.UnescapeDataString(tmpRawDataStr));
                                                fs.Write(tmpByteBuffer, 0, tmpByteBuffer.Length);
                                            }
                                        }
                                        fs.Close();
                                    }

                                    Obj_BlockQueue.Add(new Notus.Variable.Struct.PoolBlockRecordStruct()
                                    {
                                        type = 250,
                                        data = outputFileName
                                    });

                                    ObjMp_FileStatus.Set(tmpStorageId, JsonSerializer.Serialize(Notus.Variable.Enum.BlockStatusCode.InProgress));
                                    try
                                    {
                                        File.Delete(outputFileName);
                                    }
                                    catch (Exception err3)
                                    {
                                        Notus.Print.Danger(Obj_Settings, "Error Text : [9abc546ac] : " + err3.Message);
                                    }
                                    //}
                                    //catch (Exception err)
                                    //{
                                    //Console.WriteLine("Notus.Node.Validator.Main -> Convertion Error - Line 271");
                                    //Console.WriteLine(err.Message);
                                    //Console.WriteLine("Notus.Node.Validator.Main -> Convertion Error - Line 271");
                                    //}
                                }
                            }
                        }, 0);
                    }
                    FileStorageTimerIsRunning = false;
                }
            }, true);
        }
        private Dictionary<string, Dictionary<ulong, string>> GetWalletBalanceDictionary(string WalletKey, ulong timeYouCanUse)
        {
            Notus.Variable.Struct.WalletBalanceStruct tmpWalletBalanceObj = Obj_Api.BalanceObj.Get(WalletKey, timeYouCanUse);
            return tmpWalletBalanceObj.Balance;
        }

        public void CryptoTransferTimerFunc()
        {
            Notus.Print.Success(Obj_Settings, "Crypto Transfer Timer Has Started");
            Notus.Threads.Timer TimerObj = new Notus.Threads.Timer(1000);
            TimerObj.Start(() =>
            {
                if (CryptoTransferTimerIsRunning == false)
                {
                    CryptoTransferTimerIsRunning = true;
                    bool executedCryptoTransfer = false;
                    //int howManySeconds = (int)Math.Floor((DateTime.Now - EmptyBlockTime).TotalSeconds);
                    int tmpRequestSend_ListCount = Obj_Api.RequestSend_ListCount();
                    if (tmpRequestSend_ListCount > 0)
                    {
                        Console.WriteLine(tmpRequestSend_ListCount);
                        ulong unlockTimeForNodeWallet = Notus.Time.NowToUlong();
                        Notus.Variable.Struct.WalletBalanceStruct tmpValidatorWalletBalance = Obj_Api.BalanceObj.Get(Obj_Settings.NodeWallet.WalletKey, unlockTimeForNodeWallet);
                        //control-point
                        // aynı anda sadece ödeme alma veya ödeme yapma işlemi gerçekleştirilecek.
                        // bu liste aynı hesapların birden fazla kez gönderme alma işlemini engellemek için kullanılacak.
                        // işlem yapılan her hesap bu listeye atılacak
                        // eğer işlem yapılacak hesap bu listede mevcut ise bir sonraki tur da işlem yapılması için es geçilecek

                        // kısaca,
                        // alıcı veya gönderici hesap, aynı blok içinde sadece 1 kere gönderim veya alım işlemi yapabilir
                        //List<string> tmpWalletList = new List<string>() { Obj_Settings.NodeWallet.WalletKey };
                        List<string> tmpWalletList = new List<string>() { };
                        tmpWalletList.Clear();

                        List<string> tmpKeyList = new List<string>();
                        tmpKeyList.Clear();
                        BigInteger totalBlockReward = 0;

                        Notus.Variable.Class.BlockStruct_120 tmpBlockCipherData = new Notus.Variable.Class.BlockStruct_120()
                        {
                            In = new Dictionary<string, Notus.Variable.Class.BlockStruct_120_In_Struct>(),
                            //                  who                 coin               time   volume
                            Out = new Dictionary<string, Dictionary<string, Dictionary<ulong, string>>>(),
                            Validator = new Notus.Variable.Struct.ValidatorStruct()
                            {
                                NodeWallet = Obj_Settings.NodeWallet.WalletKey,
                                Reward = totalBlockReward.ToString()
                            }
                        };

                        Dictionary<string, Notus.Variable.Struct.MempoolDataList> tmpTransactionList = Obj_Api.RequestSend_DataList();

                        // wallet balances are assigned
                        Int64 transferFee = Notus.Wallet.Fee.Calculate(
                            Notus.Variable.Enum.Fee.CryptoTransfer,
                            Obj_Settings.Network,
                            Obj_Settings.Layer
                        );
                        ulong transactionCount = 0;
                        foreach (KeyValuePair<string, Notus.Variable.Struct.MempoolDataList> entry in tmpTransactionList)
                        {
                            bool walletHaveEnoughCoinOrToken = true;
                            Notus.Variable.Struct.CryptoTransactionStoreStruct tmpObjPoolCrypto = JsonSerializer.Deserialize<Notus.Variable.Struct.CryptoTransactionStoreStruct>(entry.Value.Data);

                            bool senderExist = tmpWalletList.IndexOf(tmpObjPoolCrypto.Sender) >= 0 ? true : false;
                            bool receiverExist = tmpWalletList.IndexOf(tmpObjPoolCrypto.Receiver) >= 0 ? true : false;
                            if (senderExist == false && receiverExist == false)
                            {
                                tmpWalletList.Add(tmpObjPoolCrypto.Sender);
                                tmpWalletList.Add(tmpObjPoolCrypto.Receiver);
                            }
                            if (senderExist == false && receiverExist == false)
                            {
                                Notus.Variable.Struct.WalletBalanceStruct tmpSenderBalance = Obj_Api.BalanceObj.Get(tmpObjPoolCrypto.Sender, unlockTimeForNodeWallet);
                                Notus.Variable.Struct.WalletBalanceStruct tmpReceiverBalance = Obj_Api.BalanceObj.Get(tmpObjPoolCrypto.Receiver, unlockTimeForNodeWallet);
                                string tmpTokenTagStr = "";
                                BigInteger tmpTokenVolume = 0;

                                if (string.Equals(tmpObjPoolCrypto.Currency, Obj_Settings.Genesis.CoinInfo.Tag))
                                {
                                    tmpTokenTagStr = Obj_Settings.Genesis.CoinInfo.Tag;
                                    BigInteger WalletBalanceInt = Obj_Api.BalanceObj.GetCoinBalance(tmpSenderBalance, tmpTokenTagStr);
                                    BigInteger RequiredBalanceInt = BigInteger.Parse(tmpObjPoolCrypto.Volume);
                                    tmpTokenVolume = RequiredBalanceInt;
                                    if ((RequiredBalanceInt + transferFee) > WalletBalanceInt)
                                    {
                                        walletHaveEnoughCoinOrToken = false;
                                    }
                                }
                                else
                                {
                                    if (tmpSenderBalance.Balance.ContainsKey(Obj_Settings.Genesis.CoinInfo.Tag) == false)
                                    {
                                        walletHaveEnoughCoinOrToken = false;
                                    }
                                    else
                                    {
                                        BigInteger coinFeeBalance = Obj_Api.BalanceObj.GetCoinBalance(tmpSenderBalance, Obj_Settings.Genesis.CoinInfo.Tag);
                                        if (transferFee > coinFeeBalance)
                                        {
                                            walletHaveEnoughCoinOrToken = false;
                                        }
                                        else
                                        {
                                            BigInteger tokenCurrentBalance = Obj_Api.BalanceObj.GetCoinBalance(tmpSenderBalance, tmpObjPoolCrypto.Currency);
                                            BigInteger RequiredBalanceInt = BigInteger.Parse(tmpObjPoolCrypto.Volume);
                                            if (RequiredBalanceInt > tokenCurrentBalance)
                                            {
                                                walletHaveEnoughCoinOrToken = false;
                                            }
                                            else
                                            {
                                                tmpTokenTagStr = tmpObjPoolCrypto.Currency;
                                                tmpTokenVolume = RequiredBalanceInt;
                                            }
                                        }
                                    }
                                }


                                if (walletHaveEnoughCoinOrToken == false)
                                {
                                    Obj_Api.RequestSend_Remove(entry.Key);
                                    Obj_Api.CryptoTranStatus.Set(entry.Key, JsonSerializer.Serialize(
                                        new Notus.Variable.Struct.CryptoTransferStatus()
                                        {
                                            Code = Notus.Variable.Enum.BlockStatusCode.Rejected,
                                            RowNo = 0,
                                            UID = "",
                                            Text = "Rejected"
                                        }
                                    ));
                                }
                                else
                                {
                                    totalBlockReward = totalBlockReward + transferFee;
                                    transactionCount++;
                                    if (tmpBlockCipherData.Out.ContainsKey(tmpObjPoolCrypto.Sender) == false)
                                    {
                                        tmpBlockCipherData.Out.Add(tmpObjPoolCrypto.Sender, GetWalletBalanceDictionary(tmpObjPoolCrypto.Sender, unlockTimeForNodeWallet));
                                    }
                                    if (tmpBlockCipherData.Out.ContainsKey(tmpObjPoolCrypto.Receiver) == false)
                                    {
                                        tmpBlockCipherData.Out.Add(tmpObjPoolCrypto.Receiver, GetWalletBalanceDictionary(tmpObjPoolCrypto.Receiver, unlockTimeForNodeWallet));
                                    }

                                    tmpBlockCipherData.In.Add(entry.Key, new Notus.Variable.Class.BlockStruct_120_In_Struct()
                                    {
                                        Fee = tmpObjPoolCrypto.Fee,
                                        PublicKey = tmpObjPoolCrypto.PublicKey,
                                        Sign = tmpObjPoolCrypto.Sign,
                                        Volume = tmpObjPoolCrypto.Volume,
                                        Currency = tmpObjPoolCrypto.Currency,
                                        Receiver = new Notus.Variable.Class.WalletBalanceStructForTransaction()
                                        {
                                            Balance = Obj_Api.BalanceObj.ReAssign(tmpReceiverBalance.Balance),
                                            Wallet = tmpObjPoolCrypto.Receiver,
                                            WitnessBlockUid = tmpReceiverBalance.UID,
                                            WitnessRowNo = tmpReceiverBalance.RowNo
                                        },
                                        Sender = new Notus.Variable.Class.WalletBalanceStructForTransaction()
                                        {
                                            Balance = Obj_Api.BalanceObj.ReAssign(tmpSenderBalance.Balance),
                                            Wallet = tmpObjPoolCrypto.Sender,
                                            WitnessBlockUid = tmpSenderBalance.UID,
                                            WitnessRowNo = tmpSenderBalance.RowNo
                                        }
                                    });

                                    // transfer fee added to validator wallet

                                    tmpValidatorWalletBalance = Obj_Api.BalanceObj.AddVolumeWithUnlockTime(
                                        tmpValidatorWalletBalance,
                                        transferFee.ToString(),
                                        Obj_Settings.Genesis.CoinInfo.Tag,
                                        unlockTimeForNodeWallet
                                    );
                                    //tmpBlockCipherData.Out[Obj_Settings.NodeWallet.WalletKey] = tmpValidatorWalletBalance.Balance;

                                    // sender pays transfer fee
                                    (bool tmpErrorStatusForFee, Notus.Variable.Struct.WalletBalanceStruct tmpNewResultForFee) =
                                    Obj_Api.BalanceObj.SubtractVolumeWithUnlockTime(
                                        tmpSenderBalance,
                                        transferFee.ToString(),
                                        Obj_Settings.Genesis.CoinInfo.Tag,
                                        unlockTimeForNodeWallet
                                    );
                                    if (tmpErrorStatusForFee == true)
                                    {
                                        Console.WriteLine("Coin Needed - Main.Cs -> Line 498");
                                        Console.WriteLine("Coin Needed - Main.Cs -> Line 498");
                                        Console.ReadLine();
                                    }

                                    // sender give coin or token
                                    (bool tmpErrorStatusForTransaction, Notus.Variable.Struct.WalletBalanceStruct tmpNewResultForTransaction) =
                                    Obj_Api.BalanceObj.SubtractVolumeWithUnlockTime(
                                        tmpNewResultForFee,
                                        tmpTokenVolume.ToString(),
                                        tmpTokenTagStr,
                                        unlockTimeForNodeWallet
                                    );
                                    if (tmpErrorStatusForTransaction == true)
                                    {
                                        Console.WriteLine("Coin Needed - Main.Cs -> Line 498");
                                        Console.WriteLine("Coin Needed - Main.Cs -> Line 498");
                                        Console.ReadLine();
                                    }
                                    tmpBlockCipherData.Out[tmpObjPoolCrypto.Sender] = tmpNewResultForTransaction.Balance;

                                    //receiver get coin or token
                                    Notus.Variable.Struct.WalletBalanceStruct tmpNewReceiverBalance = Obj_Api.BalanceObj.AddVolumeWithUnlockTime(
                                        tmpReceiverBalance,
                                        tmpObjPoolCrypto.Volume,
                                        tmpObjPoolCrypto.Currency,
                                        tmpObjPoolCrypto.UnlockTime
                                    );
                                    tmpBlockCipherData.Out[tmpObjPoolCrypto.Receiver] = tmpNewReceiverBalance.Balance;
                                }
                            }
                        }
                        if (transactionCount > 0)
                        {
                            foreach (KeyValuePair<string, Dictionary<string, Dictionary<ulong, string>>> walletEntry in tmpBlockCipherData.Out)
                            {
                                foreach (KeyValuePair<string, Dictionary<ulong, string>> currencyEntry in walletEntry.Value)
                                {
                                    List<ulong> tmpRemoveList = new List<ulong>();
                                    foreach (KeyValuePair<ulong, string> balanceEntry in currencyEntry.Value)
                                    {
                                        if (balanceEntry.Value == "0")
                                        {
                                            tmpRemoveList.Add(balanceEntry.Key);
                                        }
                                    }
                                    for (int innerForCount = 0; innerForCount < tmpRemoveList.Count; innerForCount++)
                                    {
                                        tmpBlockCipherData.Out[walletEntry.Key][currencyEntry.Key].Remove(tmpRemoveList[innerForCount]);
                                    }
                                }
                            }
                            tmpBlockCipherData.Validator.Reward = totalBlockReward.ToString();

                            Obj_BlockQueue.Add(new Notus.Variable.Struct.PoolBlockRecordStruct()
                            {
                                type = 120,
                                data = JsonSerializer.Serialize(tmpBlockCipherData)
                            });
                        }
                        foreach (KeyValuePair<string, Notus.Variable.Class.BlockStruct_120_In_Struct> entry in tmpBlockCipherData.In)
                        {
                            Obj_Api.RequestSend_Remove(entry.Key);
                        }
                    }  //if (ObjMp_CryptoTransfer.Count() > 0)


                    if (executedCryptoTransfer == true)
                    {

                    }  //if (executedCryptoTransfer == true)

                    CryptoTransferTime = DateTime.Now;
                    CryptoTransferTimerIsRunning = false;

                }  //if (CryptoTransferTimerIsRunning == false)
            }, true);  //TimerObj.Start(() =>
        }
        private void SetTimeStatusForBeginSync(bool status)
        {
            if (Obj_Settings.GenesisCreated == false)
            {
                if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
                {
                    EmptyBlockTimerIsRunning = status;
                    CryptoTransferTimerIsRunning = status;
                }
                if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer2)
                {
                }
                if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer3)
                {
                }
            }
        }
        private void WaitUntilEnoughNode()
        {
            if (Obj_Settings.GenesisCreated == false)
            {
                SetTimeStatusForBeginSync(true);        // stop timer
                while (ValidatorQueueObj.WaitForEnoughNode == true)
                {
                    Thread.Sleep(1);
                }
                SetTimeStatusForBeginSync(false);       // release timer
            }
        }
        public void Start()
        {
            Obj_Integrity = new Notus.Block.Integrity();
            Obj_Integrity.Settings = Obj_Settings;
            Obj_Integrity.ControlGenesisBlock(); // we check and compare genesis with onther node
            Obj_Integrity.GetLastBlock();        // get last block from current node

            Obj_Settings.GenesisCreated = Obj_Integrity.Settings.GenesisCreated;
            Obj_Settings.LastBlock = Obj_Integrity.Settings.LastBlock;
            Obj_Settings.Genesis = Obj_Integrity.Settings.Genesis;

            if (Obj_Settings.Genesis == null)
            {
                Notus.Print.Basic(Obj_Settings, "Notus.Validator.Main -> Genesis Is NULL");
            }
            Obj_Api = new Notus.Validator.Api();
            Obj_Api.Settings = Obj_Settings;

            Obj_BlockQueue.Settings = Obj_Settings;
            Obj_BlockQueue.Start();

            Obj_Api.Func_OnReadFromChain = blockKeyIdStr =>
            {
                (bool tmpBlockExist, Notus.Variable.Class.BlockData tmpBlockResult) = Obj_BlockQueue.ReadFromChain(blockKeyIdStr);
                if (tmpBlockExist == true)
                {
                    return tmpBlockResult;
                }
                return null;
            };
            Obj_Api.Func_AddToChainPool = blockStructForQueue =>
            {
                Obj_BlockQueue.Add(blockStructForQueue);
                return true;
            };
            Obj_Api.Prepare();

            //Obj_MainCache = new Notus.Cache.Main();
            //Obj_MainCache.Settings = Obj_Settings;
            //Obj_MainCache.Start();
            // Obj_TokenStorage = new Notus.Token.Storage();
            // Obj_TokenStorage.Settings = Obj_Settings;

            if (Obj_Settings.GenesisCreated == false && Obj_Settings.Genesis != null)
            {
                Notus.Print.Basic(Obj_Settings, "Last Block Row No : " + Obj_Settings.LastBlock.info.rowNo.ToString());
                using (Notus.Mempool ObjMp_BlockOrder =
                    new Notus.Mempool(
                        Notus.IO.GetFolderName(Obj_Settings.Network, Obj_Settings.Layer, Notus.Variable.Constant.StorageFolderName.Common) +
                        "block_order_list"
                    )
                )
                {
                    ObjMp_BlockOrder.Each((string blockUniqueId, string BlockText) =>
                    {
                        KeyValuePair<string, string> BlockOrder = JsonSerializer.Deserialize<KeyValuePair<string, string>>(BlockText);
                        if (string.Equals(new Notus.Hash().CommonSign("sha1", BlockOrder.Key + Obj_Settings.HashSalt), BlockOrder.Value))
                        {
                            using (Notus.Block.Storage Obj_Storage = new Notus.Block.Storage(false))
                            {
                                Obj_Storage.Network = Obj_Settings.Network;
                                Obj_Storage.Layer = Obj_Settings.Layer;
                                (bool tmpBlockExist, Notus.Variable.Class.BlockData tmpBlockData) = Obj_Storage.ReadBlock(BlockOrder.Key);
                                if (tmpBlockExist == true)
                                {
                                    ProcessBlock(tmpBlockData, 1);
                                }
                                else
                                {
                                    Notus.Print.Danger(Obj_Settings, "Notus.Block.Integrity -> Block Does Not Exist");
                                    Notus.Print.Danger(Obj_Settings, "Reset Block");
                                    Notus.Print.ReadLine(Obj_Settings);
                                }
                            }
                        }
                        else
                        {
                            Notus.Print.Danger(Obj_Settings, "Hash calculation error");
                        }
                    }, 0
                    );
                    Notus.Print.Info(Obj_Settings, "All Blocks Loaded");
                }
                SelectedPortVal = Notus.Toolbox.Network.GetNetworkPort(Obj_Settings);
            }
            else
            {
                SelectedPortVal = Notus.Toolbox.Network.FindFreeTcpPort();
            }

            HttpObj.DefaultResult_OK = "null";
            HttpObj.DefaultResult_ERR = "null";
            //Notus.Print.Basic(Settings.InfoMode,"empty count : " + Obj_Integrity.EmptyBlockCount);
            if (Obj_Settings.GenesisCreated == false)
            {
                Notus.Print.Basic(Obj_Settings, "Main Validator Started");
            }
            Obj_BlockQueue.Settings.LastBlock = Obj_Settings.LastBlock;
            //BlockStatObj = Obj_BlockQueue.CurrentBlockStatus();
            Start_HttpListener();
            ValidatorQueueObj.Settings = Obj_Settings;
           
            // her gelen blok bir listeye eklenmeli ve o liste ile sıra ile eklenmeli
            ValidatorQueueObj.Func_NewBlockIncome = tmpNewBlockIncome =>
            {
                ProcessBlock(tmpNewBlockIncome, 2);
                //Notus.Print.Info(Obj_Settings, "Arrived New Block : " + tmpNewBlockIncome.info.uID);
                return true;
            };

            if (Obj_Settings.GenesisCreated == false)
            {
                ValidatorQueueObj.PreStart(
                    Obj_Settings.LastBlock.info.rowNo,
                    Obj_Settings.LastBlock.info.uID,
                    Obj_Settings.LastBlock.sign,
                    Obj_Settings.LastBlock.prev
                );

                Notus.Print.Info(Obj_Settings, "Waiting For Node Sync", false);
                ValidatorQueueObj.PingOtherNodes();
                //burada ping ve pong yaparak bekleyecek
            }

            ValidatorQueueObj.Start();

            if (Obj_Settings.GenesisCreated == false)
            {
                Notus.Print.Info(Obj_Settings, "Node Blocks Are Checking For Sync");
                Notus.Sync.Block(
                    Obj_Settings, ValidatorQueueObj.GiveMeNodeList(),
                    tmpNewBlockIncome =>
                    {
                        ProcessBlock(tmpNewBlockIncome, 3);
                        //Notus.Print.Info(Obj_Settings, "Temprorary Arrived New Block : " + tmpNewBlockIncome.info.uID);
                    }
                );
                //Console.WriteLine("ValidatorQueueObj.MyNodeIsReady();");
                //Console.WriteLine("ValidatorQueueObj.MyNodeIsReady();");
                Console.WriteLine("Control-Point-1");
                ValidatorQueueObj.MyNodeIsReady();
            }


            if (Obj_Settings.GenesisCreated == false)
            {
                if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
                {
                    EmptyBlockTimerFunc();
                    CryptoTransferTimerFunc();
                }
                if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer2)
                {
                }
                if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer3)
                {
                    FileStorageTimer();
                }
                Notus.Print.Success(Obj_Settings, "First Synchronization Is Done");
            }

            DateTime LastPrintTime = DateTime.Now;
            bool tmpStartWorkingPrinted = false;
            bool tmpExitMainLoop = false;
            while (tmpExitMainLoop == false)
            {
                //Console.WriteLine(EmptyBlockTimerIsRunning);
                WaitUntilEnoughNode();
                if (tmpStartWorkingPrinted == false)
                {
                    tmpStartWorkingPrinted = true;
                    Notus.Print.Success(Obj_Settings, "Node Starts");
                }
                if (ValidatorQueueObj.MyTurn == true || Obj_Settings.GenesisCreated == true)
                {
                    // geçerli utc zaman bilgisini alıp block oluşturma işlemi için parametre olarak gönder böylece
                    // her blok utc zamanı ile oluşturulmuş olsun
                    DateTime currentUtcTime = ValidatorQueueObj.GetUtcTime();
                    (bool bStatus, Notus.Variable.Struct.PoolBlockRecordStruct TmpBlockStruct) = Obj_BlockQueue.Get(currentUtcTime);
                    if (bStatus == true)
                    {
                        Notus.Variable.Class.BlockData? PreBlockData = JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(TmpBlockStruct.data);

                        // omergoksoy
                        //Notus.Variable.Enum.ValfwidatorOrder NodeOrder = ValidatorQueueObj.Distrubute(PreBlockData);

                        /*
                        if (NodeOrder == Notus.Variable.Enum.ValidatorOrder.Primary)
                        {

                        }
                        */

                        //blok sıra ve önceki değerleri düzenleniyor...
                        if (PreBlockData != null)
                        {
                            PreBlockData = Obj_BlockQueue.OrganizeBlockOrder(PreBlockData);
                            Notus.Variable.Class.BlockData PreparedBlockData = new Notus.Block.Generate(Obj_Settings.NodeWallet.WalletKey).Make(PreBlockData, 1000);
                            ProcessBlock(PreparedBlockData, 4);
                            ValidatorQueueObj.Distrubute(PreBlockData.info.rowNo);
                            Thread.Sleep(1);
                        }
                        else
                        {
                            Notus.Print.Danger(Obj_Settings, "Pre Block Is NULL");
                        }
                    }
                    else
                    {
                        if ((DateTime.Now - LastPrintTime).TotalSeconds > 20)
                        {
                            LastPrintTime = DateTime.Now;
                            if (Obj_Settings.GenesisCreated == true)
                            {
                                tmpExitMainLoop = true;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.DarkGreen;
                                Console.Write("+");
                                Thread.Sleep(1);
                            }
                        }
                    }
                }
                else
                {
                    if(Obj_Settings.GenesisCreated == false)
                    {
                        if ((DateTime.Now - LastPrintTime).TotalSeconds > 20)
                        {
                            LastPrintTime = DateTime.Now;
                            Console.ForegroundColor = ConsoleColor.DarkMagenta;
                            Console.Write(".");
                            Thread.Sleep(1);
                        }
                    }
                }
            }
            if (Obj_Settings.GenesisCreated == true)
            {
                Notus.Print.Warning(Obj_Settings, "Main Class Temporary Ended");
            }
            else
            {
                Notus.Print.Warning(Obj_Settings, "Main Class Ended");
            }
        }

        private bool ProcessBlock(Notus.Variable.Class.BlockData blockData, int blockSource)
        {
            if (blockSource == 1)
            {
                if (
                    blockData.info.type != 300
                    &&
                    blockData.info.type != 360
                )
                {
                    Notus.Print.Status(Obj_Settings, "Block Came From The Loading DB");
                }
            }
            if (blockSource == 2)
            {
                Notus.Print.Status(Obj_Settings, "Block Came From The Validator Queue");
            }
            if (blockSource == 3)
            {
                Notus.Print.Status(Obj_Settings, "Block Came From The Block Sync");
            }
            if (blockSource == 4)
            {
                Notus.Print.Status(Obj_Settings, "Block Came From The Main Loop");
            }
            if (blockSource == 5)
            {
                Notus.Print.Status(Obj_Settings, "Block Came From The Dictionary List");
            }
            if (blockData.info.rowNo > CurrentBlockRowNo)
            {
                Notus.Variable.Class.BlockData? tmpBlockData =
                    JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(
                        JsonSerializer.Serialize(blockData)
                    );
                if (tmpBlockData != null)
                {
                    IncomeBlockList.Add(blockData.info.rowNo, tmpBlockData);
                    Notus.Print.Status(Obj_Settings, "Insert Block To Temporary Block List");
                }
                return true;
            }
            if (CurrentBlockRowNo > blockData.info.rowNo)
            {
                Notus.Print.Warning(Obj_Settings, "We Already Processed The Block");
                return true;
            }

            if (blockData.info.rowNo > Obj_Settings.LastBlock.info.rowNo)
            {
                if (blockData.info.type == 300)
                {
                    EmptyBlockGeneratedTime = Notus.Date.ToDateTime(blockData.info.time);
                }

                Obj_BlockQueue.Settings.LastBlock = blockData;
                Obj_Settings.LastBlock = blockData;

                Obj_Api.Settings.LastBlock = Obj_Settings.LastBlock;

                Obj_BlockQueue.AddToChain(blockData);
                if (blockData.info.type == 250)
                {
                    Obj_Api.Layer3_StorageFileDone(blockData.info.uID);
                }
                if (blockData.info.type == 240)
                {
                    Console.WriteLine("Notus.Main.OrganizeEachBlock -> Line 705");
                    Console.WriteLine("Notus.Main.OrganizeEachBlock -> Line 705");
                    Console.WriteLine("Make request and add file to layer 3");
                    Console.WriteLine(JsonSerializer.Serialize(blockData, new JsonSerializerOptions() { WriteIndented = true }));

                    Notus.Variable.Struct.StorageOnChainStruct tmpStorageOnChain = JsonSerializer.Deserialize<Notus.Variable.Struct.StorageOnChainStruct>(System.Text.Encoding.UTF8.GetString(
                        System.Convert.FromBase64String(
                            blockData.cipher.data
                        )
                    ));
                    Console.WriteLine("----------------------------------------------------------");
                    Console.WriteLine(JsonSerializer.Serialize(tmpStorageOnChain));
                    Console.WriteLine("----------------------------------------------------------");

                    int calculatedChunkCount = (int)Math.Ceiling(System.Convert.ToDouble(tmpStorageOnChain.Size / Notus.Variable.Constant.DefaultChunkSize));
                    Notus.Variable.Struct.FileTransferStruct tmpFileData = new Notus.Variable.Struct.FileTransferStruct()
                    {
                        BlockType = 240,
                        ChunkSize = Notus.Variable.Constant.DefaultChunkSize,
                        ChunkCount = calculatedChunkCount,
                        FileHash = tmpStorageOnChain.Hash,
                        FileName = tmpStorageOnChain.Name,
                        FileSize = tmpStorageOnChain.Size,
                        Level = Notus.Variable.Enum.ProtectionLevel.Low,
                        PublicKey = tmpStorageOnChain.PublicKey,
                        Sign = tmpStorageOnChain.Sign,
                        StoreEncrypted = tmpStorageOnChain.Encrypted,
                        WaterMarkIsLight = true
                    };

                    string responseData = Notus.Network.Node.FindAvailableSync(
                        "storage/file/new/" + blockData.info.uID,
                        new Dictionary<string, string>()
                        {
                    {
                        "data",
                        JsonSerializer.Serialize(tmpFileData)
                    }
                        },
                        Obj_Settings.Network,
                        Notus.Variable.Enum.NetworkLayer.Layer3,
                        Obj_Settings
                    );
                    Console.WriteLine(responseData);
                }
                Notus.Print.Success(Obj_Settings, "Generated Last Block UID  [" + blockData.info.type.ToString() + "] : " + Obj_Settings.LastBlock.info.uID.Substring(0, 10) + "...." + Obj_Settings.LastBlock.info.uID.Substring(80) + " -> " + Obj_Settings.LastBlock.info.rowNo.ToString());
            }

            Obj_Api.AddForCache(blockData);

            if (IncomeBlockList.ContainsKey(CurrentBlockRowNo))
            {
                IncomeBlockList.Remove(CurrentBlockRowNo);
            }

            CurrentBlockRowNo++;

            if (IncomeBlockList.ContainsKey(CurrentBlockRowNo))
            {
                ProcessBlock(IncomeBlockList[CurrentBlockRowNo], 5);
            }
            return true;
        }

        private void Start_HttpListener()
        {
            if (Obj_Settings.LocalNode == true)
            {
                Notus.Print.Basic(Obj_Settings, "Listining : " +
                Notus.Network.Node.MakeHttpListenerPath(Obj_Settings.IpInfo.Local, SelectedPortVal), false);
            }
            else
            {
                Notus.Print.Basic(Obj_Settings, "Listining : " +
                Notus.Network.Node.MakeHttpListenerPath(Obj_Settings.IpInfo.Public, SelectedPortVal), false);
            }
            HttpObj.OnReceive(Fnc_OnReceiveData);
            HttpObj.ResponseType = "application/json";
            IPAddress NodeIpAddress = IPAddress.Parse(
                (
                    Obj_Settings.LocalNode == false ?
                    Obj_Settings.IpInfo.Public :
                    Obj_Settings.IpInfo.Local
                )
            );
            HttpObj.Settings = Obj_Settings;
            HttpObj.StoreUrl = false;
            HttpObj.Start(NodeIpAddress, SelectedPortVal);
            Notus.Print.Success(Obj_Settings, "Http Has Started", false);
        }

        private string Fnc_OnReceiveData(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            string resultData = Obj_Api.Interpret(IncomeData);
            if (string.Equals(resultData, "queue-data"))
            {
                resultData = ValidatorQueueObj.Process(IncomeData);
            }
            return resultData;
        }

        public Main()
        {
        }
        ~Main()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (Obj_BlockQueue != null)
            {
                try
                {
                    Obj_BlockQueue.Dispose();
                }
                catch { }
            }

            if (ValidatorQueueObj != null)
            {
                try
                {
                    ValidatorQueueObj.Dispose();
                }
                catch { }
            }

            if (Obj_Api != null)
            {
                try
                {
                    Obj_Api.Dispose();
                }
                catch { }
            }

            if (HttpObj != null)
            {
                try
                {
                    HttpObj.Dispose();
                }
                catch { }
            }

            if (Obj_Integrity != null)
            {
                try
                {
                    Obj_Integrity.Dispose();
                }
                catch { }
            }

        }
    }
}
