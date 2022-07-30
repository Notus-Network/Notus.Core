using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Numerics;
using System.Text.Json;
using System.Threading;

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
        private int SelectedPortVal = 0;

        //bu nesnenin görevi network'e bağlı nodeların listesini senkronize etmek
        //private Notus.Network.Controller ControllerObj = new Notus.Network.Controller();
        private Notus.Communication.Http HttpObj = new Notus.Communication.Http(true);
        private Notus.Block.Integrity Obj_Integrity;
        private Notus.Validator.Api Obj_Api;
        //private Notus.Cache.Main Obj_MainCache;
        //private Notus.Token.Storage Obj_TokenStorage;
        public bool EmptyTimerActive = false;
        public bool CryptoTimerActive = false;

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

        //private bool ImageWaterMarkTimerIsRunning = false;
        //private DateTime ImageWaterMarkTime = DateTime.Now;

        private bool FileStorageTimerIsRunning = false;
        private DateTime FileStorageTime = DateTime.Now;

        public ConcurrentQueue<Notus.Variable.Class.BlockData> IncomeBlockList = new ConcurrentQueue<Notus.Variable.Class.BlockData>();
        private Notus.Block.Queue Obj_BlockQueue = new Notus.Block.Queue();
        private Notus.Validator.Queue ValidatorQueueObj = new Notus.Validator.Queue();

        //private System.Action<string, Notus.Variable.Class.BlockData> OnReadFromChainFuncObj = null;
        public void EmptyBlockTimerFunc()
        {
            Console.WriteLine("EmptyBlockTimerFunc - Line");
            Console.WriteLine(Obj_Settings.Genesis.Empty.Active);
            {
                Notus.Print.Basic(Obj_Settings, "Timer Has Started");

                Notus.Threads.Timer TimerObj = new Notus.Threads.Timer(1000);
                TimerObj.Start(() =>
                {
                    if (EmptyBlockTimerIsRunning == false)
                    {
                        EmptyBlockTimerIsRunning = true;
                        int howManySeconds = Obj_Settings.Genesis.Empty.Interval.Time;

                        if (Obj_Settings.Genesis.Empty.SlowBlock.Count >= Obj_Integrity.EmptyBlockCount)
                        {
                            howManySeconds = (Obj_Settings.Genesis.Empty.Interval.Time * Obj_Settings.Genesis.Empty.SlowBlock.Multiply);
                        }
                        //blok zamanı ve utc zamanı çakışıyor
                        DateTime tmpLastTime = Notus.Date.ToDateTime(Obj_Settings.LastBlock.info.time).AddSeconds(howManySeconds);
                        Console.WriteLine(
                            ValidatorQueueObj.GetUtcTime().ToString(Notus.Variable.Constant.DefaultDateTimeFormatText) + 
                            " - " + 
                            tmpLastTime.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText)
                        );
                        // get utc time from validatır Queue
                        if (ValidatorQueueObj.GetUtcTime() > tmpLastTime)
                        {
                            if (ValidatorQueueObj.MyTurn)
                            {
                                if ((DateTime.Now - EmptyBlockGeneratedTime).TotalSeconds > 30)
                                {
                                    Notus.Print.Basic(Obj_Settings, "Empty Block Executed");
                                    Obj_BlockQueue.AddEmptyBlock();
                                    EmptyBlockGeneratedTime = DateTime.Now;
                                }
                                EmptyBlockNotMyTurnPrinted = false;
                            }
                            else
                            {
                                if (EmptyBlockNotMyTurnPrinted == false)
                                {
                                    Notus.Print.Basic(Obj_Settings, "Not My Turn For Empty Block");
                                    EmptyBlockNotMyTurnPrinted = true;
                                }
                            }
                        }
                        EmptyBlockTimerIsRunning = false;
                    }
                }, true);
            }
        }
        /*
        public void ImageWaterMarkTimer()
        {
            Notus.Print.Basic(Obj_Settings, "Water Mark Timer Has Started");

            Notus.Threads.Timer TimerObj = new Notus.Threads.Timer(2000);
            TimerObj.Start(() =>
            {
                if (ImageWaterMarkTimerIsRunning == false)
                {
                    ImageWaterMarkTimerIsRunning = true;
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
                                    try
                                    {
                                        string tmpWalletKey = Notus.Wallet.ID.GetAddressWithPublicKey(tmpFileObj.PublicKey);
                                        string tmpOutputFileName = Notus.IO.GetFolderName(
                                            Obj_Settings.Network,
                                            Obj_Settings.Layer,
                                            Notus.Variable.Constant.StorageFolderName.Storage
                                        ) + tmpWalletKey + System.IO.Path.DirectorySeparatorChar;
                                        Notus.IO.CreateDirectory(tmpOutputFileName);
                                        string fileExtensionStr = Path.GetExtension(tmpFileObj.FileName);
                                        string outputFileName = tmpOutputFileName + tmpStorageId + fileExtensionStr;
                                        FileStream fs = new FileStream(outputFileName, FileMode.Create, FileAccess.ReadWrite);
                                        Dictionary<int, string> tmpChunkList = JsonSerializer.Deserialize<Dictionary<int, string>>(tmpCurrentList);
                                        foreach (KeyValuePair<int, string> entry in tmpChunkList)
                                        {
                                            string tmpChunkIdKey = entry.Value;
                                            int tmpStorageNo = Notus.Core.Function.CalculateStorageNumber(
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
                                        string destinationFileName = tmpOutputFileName + tmpStorageId + ".marked";
                                        Notus.IO.AddWatermarkToImage(outputFileName, destinationFileName, tmpWalletKey, tmpFileObj.Level, !tmpFileObj.WaterMarkIsLight);
                                        ObjMp_FileStatus.Set(tmpStorageId, JsonSerializer.Serialize(Notus.Variable.Enum.BlockStatusCode.Completed));
                                        Console.WriteLine("Watermark Function Executed");
                                    }
                                    catch (Exception err)
                                    {
                                        Console.WriteLine("Notus.Node.Validator.Main -> Convertion Error - Line 175");
                                        Console.WriteLine(err.Message);
                                        Console.WriteLine("Notus.Node.Validator.Main -> Convertion Error - Line 175");
                                    }
                                }
                            }
                        }, 0);
                    }
                    ImageWaterMarkTimerIsRunning = false;
                }
            }, true);
        }
        */
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
                                    string tmpWalletKey = Notus.Wallet.ID.GetAddressWithPublicKey(tmpFileObj.PublicKey,Obj_Settings.Network);
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
                                        Notus.Print.Basic(Obj_Settings.DebugMode, "Error Text : [9abc546ac] : " + err3.Message);
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
            Notus.Print.Basic(Obj_Settings, "Crypto Transfer Timer Has Started");
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
                        ulong unlockTimeForNodeWallet = Notus.Time.NowToUlong();
                        Notus.Variable.Struct.WalletBalanceStruct tmpValidatorWalletBalance = Obj_Api.BalanceObj.Get(Obj_Settings.NodeWallet.WalletKey, unlockTimeForNodeWallet);

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
                        Int64 transferFee = Notus.Wallet.Fee.Calculate(Notus.Variable.Enum.Fee.CryptoTransfer);
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
                    if (EmptyTimerActive == true)
                    {
                        EmptyBlockTimerIsRunning = status;
                    }
                    if (CryptoTimerActive == true)
                    {
                        CryptoTransferTimerIsRunning = status;
                    }
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
            SetTimeStatusForBeginSync(true);        // stop timer
            while (ValidatorQueueObj.WaitForEnoughNode == true)
            {
                Thread.Sleep(100);
            }
            SetTimeStatusForBeginSync(false);       // release timer
        }
        public void Start()
        {
            // control-point
            // controlpoint

            //Console.Write("--------------------------------------------------------------------");
            //Console.Write("Notus.Node.Validator.Main.Start - Line 283");
            //Console.Write(JsonSerializer.Serialize(Obj_Settings, new JsonSerializerOptions() { WriteIndented = true }));
            //Console.Write("--------------------------------------------------------------------");

            Obj_Integrity = new Notus.Block.Integrity();
            Obj_Integrity.Settings = Obj_Settings;
            Obj_Integrity.GetLastBlock();

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
            /*
            System.Func<string, Notus.Variable.Class.BlockData> Delegate_ReadFromChain = blockKeyIdStr =>
            {
                (bool tmpBlockExist, Notus.Variable.Class.BlockData tmpBlockResult)=Obj_BlockQueue.ReadFromChain(blockKeyIdStr);
                if (tmpBlockExist == true)
                {
                    return tmpBlockResult;
                }
                return null;
            };
            Obj_Api.Func_OnReadFromChain = Delegate_ReadFromChain;
            */
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
                                    OrganizeEachBlock(tmpBlockData, false);
                                }
                                else
                                {
                                    Notus.Print.Danger(Obj_Settings, "Notus.Block.Integrity -> Block Does Not Exist");
                                }
                            }
                        }
                        else
                        {
                            Notus.Print.Danger(Obj_Settings, "Hash calculation error");
                        }
                    }, 0
                    );
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

            Notus.Print.Info(Obj_Settings, "Waiting For Node Sync", false);
            ValidatorQueueObj.Settings = Obj_Settings;
            /*
            Obj_Api.Func_OnReadFromChain = blockKeyIdStr =>
            {
                (bool tmpBlockExist, Notus.Variable.Class.BlockData tmpBlockResult) = Obj_BlockQueue.ReadFromChain(blockKeyIdStr);
                if (tmpBlockExist == true)
                {
                    return tmpBlockResult;
                }
                return null;
            };
            
            ValidatorQueueObj.Func_NewBlockIncome
            */
            ValidatorQueueObj.Func_NewBlockIncome = tmpNewBlockIncome =>
            {
                Console.WriteLine("Arrived New Block : " + tmpNewBlockIncome.info.uID);
                AddedNewBlock(tmpNewBlockIncome);
                return true;
            };
            //Console.ReadLine();

            if (Obj_Settings.GenesisCreated == true)
            {
                ValidatorQueueObj.PreStart(0, 
                    string.Empty,
                    string.Empty,
                    string.Empty
                );
            }
            else
            {
                ValidatorQueueObj.PreStart(
                    Obj_Settings.LastBlock.info.rowNo,
                    Obj_Settings.LastBlock.info.uID,
                    Obj_Settings.LastBlock.sign,
                    Obj_Settings.LastBlock.prev
                );
            }
            ValidatorQueueObj.Start();
            Console.WriteLine("Step-Control-1111");
            while (ValidatorQueueObj.IncomeBlockListDone == false)
            {
                Thread.Sleep(50);
            }
            Console.WriteLine("Step-Control-2222");
            bool quitFromWhileLoop = false;
            while (quitFromWhileLoop == false)
            {
                quitFromWhileLoop = true;
                if (ValidatorQueueObj.IncomeBlockList.TryDequeue(out Variable.Class.BlockData? retValue))
                {
                    if (retValue != null)
                    {
                        IncomeBlockList.Enqueue(retValue);
                        quitFromWhileLoop = false;
                    }
                }
            }
            Console.WriteLine("Step-Control-3333");
            quitFromWhileLoop = false;
            while (quitFromWhileLoop == false)
            {
                quitFromWhileLoop = true;
                if (IncomeBlockList.TryDequeue(out Variable.Class.BlockData? retValue))
                {
                    if (retValue != null)
                    {
                        AddedNewBlock(retValue);
                        quitFromWhileLoop = false;
                    }
                }
            }
            Console.WriteLine("Step-Control-4444");
            //burada hangi node'un empty timer'dan sorumlu olacağı seçiliyor...
            /*
            if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
            {
                Notus.Variable.Enum.ValidatorOrder EmptyNodeOrder = ValidatorQueueObj.EmptyTimer();
                if (EmptyNodeOrder == Notus.Variable.Enum.ValidatorOrder.Primary)
                {

                }
            }
            */

            EmptyTimerActive = true;
            Console.WriteLine("Obj_Settings.GenesisCreated");
            Console.WriteLine(Obj_Settings.GenesisCreated);
            if (Obj_Settings.GenesisCreated == false)
            {
                Console.WriteLine(Obj_Settings.Layer.ToString());
                if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
                {
                    if (EmptyTimerActive == true)
                    {
                        EmptyBlockTimerFunc();
                    }
                    if (CryptoTimerActive == true)
                    {
                        CryptoTransferTimerFunc();
                    }
                }
                if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer2)
                {
                    /*
                    ImageWaterMarkTimer();
                    */
                }
                if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer3)
                {
                    FileStorageTimer();
                }
            }
            Notus.Print.Basic(Obj_Settings, "First Synchronization Is Done");

            DateTime LastPrintTime = DateTime.Now;
            bool tmpExitMainLoop = false;
            while (tmpExitMainLoop == false)
            {
                WaitUntilEnoughNode();
                if (ValidatorQueueObj.MyTurn==true)
                {
                    // geçerli utc zaman bilgisini alıp block oluşturma işlemi için parametre olarak gönder böylece
                    // her blok utc zamanı ile oluşturulmuş olsun
                    DateTime currentUtcTime = ValidatorQueueObj.GetUtcTime();
                    (bool bStatus, Notus.Variable.Struct.PoolBlockRecordStruct TmpBlockStruct) = Obj_BlockQueue.Get(currentUtcTime);
                    if (bStatus == true)
                    {
                        Notus.Variable.Class.BlockData PreBlockData = JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(TmpBlockStruct.data);

                        // oluşturulan blok burada diğer node'lara dağıtılmalı
                        // oluşturulan blok burada diğer node'lara dağıtılmalı
                        // oluşturulan blok burada diğer node'lara dağıtılmalı
                        // oluşturulan blok burada diğer node'lara dağıtılmalı
                        // omergoksoy
                        // omergoksoy

                        // omergoksoy
                        //Notus.Variable.Enum.ValfwidatorOrder NodeOrder = ValidatorQueueObj.Distrubute(PreBlockData);

                        /*
                        if (NodeOrder == Notus.Variable.Enum.ValidatorOrder.Primary)
                        {

                        }
                        */

                        //blok sıra ve önceki değerleri düzenleniyor...
                        PreBlockData = Obj_BlockQueue.OrganizeBlockOrder(PreBlockData);

                        //Notus.Print.Basic(Obj_Settings, "NodeOrder : " + NodeOrder.ToString());
                        Notus.Variable.Class.BlockData PreparedBlockData = new Notus.Block.Generate(Obj_Settings.NodeWallet.WalletKey).Make(PreBlockData, 1000);
                        AddedNewBlock(PreparedBlockData);
                        ValidatorQueueObj.Distrubute(PreBlockData);
                        Thread.Sleep(500);
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
                                Console.Write(".");
                                //Console.WriteLine("ValidatorQueueObj.TotalNodeCount : " + ValidatorQueueObj.TotalNodeCount.ToString());
                                //Console.WriteLine("ValidatorQueueObj.OnlineNodeCount : " + ValidatorQueueObj.OnlineNodeCount.ToString());

                                // Notus.Print.Basic(DebugModeActive, "Wait For Request");
                                Thread.Sleep(5);
                            }
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

        private void AddedNewBlock(Notus.Variable.Class.BlockData Obj_BlockData)
        {
            Obj_BlockQueue.Settings.LastBlock = Obj_BlockData;
            Obj_Settings.LastBlock = Obj_BlockData;

            Obj_Api.Settings.LastBlock = Obj_Settings.LastBlock;

            Notus.Print.Basic(Obj_Settings, "Block Generated [" + Obj_BlockData.info.type.ToString() + "]: " + Obj_BlockData.info.uID);
            OrganizeEachBlock(Obj_BlockData, true);
        }
        private void OrganizeEachBlock(Notus.Variable.Class.BlockData Obj_BlockData, bool NewBlock)
        {
            if (NewBlock == true)
            {
                Obj_BlockQueue.AddToChain(Obj_BlockData);

                if (Obj_BlockData.info.type == 250)
                {
                    Obj_Api.Layer3_StorageFileDone(Obj_BlockData.info.uID);
                }
                if (Obj_BlockData.info.type == 240)
                {
                    Console.WriteLine("Notus.Main.OrganizeEachBlock -> Line 705");
                    Console.WriteLine("Notus.Main.OrganizeEachBlock -> Line 705");
                    Console.WriteLine("Make request and add file to layer 3");
                    Console.WriteLine(JsonSerializer.Serialize(Obj_BlockData, new JsonSerializerOptions() { WriteIndented = true }));

                    Notus.Variable.Struct.StorageOnChainStruct tmpStorageOnChain = JsonSerializer.Deserialize<Notus.Variable.Struct.StorageOnChainStruct>(System.Text.Encoding.UTF8.GetString(
                        System.Convert.FromBase64String(
                            Obj_BlockData.cipher.data
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
                        "storage/file/new/" + Obj_BlockData.info.uID,
                        new Dictionary<string, string>()
                        {
                    {
                        "data",
                        JsonSerializer.Serialize(tmpFileData)
                    }
                        },
                        Obj_Settings.Network,
                        Notus.Variable.Enum.NetworkLayer.Layer3
                    );
                    Console.WriteLine(responseData);
                }
            }

            Obj_Api.AddForCache(Obj_BlockData);
        }
        private void Start_HttpListener()
        {
            /*
            if (Obj_Settings.LocalNode == true)
            {
                Console.WriteLine("Listining : " + Notus.Network.Node.MakeHttpListenerPath(Obj_Settings.IpInfo.Local, SelectedPortVal));
            }
            else
            {
                Console.WriteLine( "Listining : " +Notus.Network.Node.MakeHttpListenerPath(Obj_Settings.IpInfo.Public, SelectedPortVal));
            }
            */

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

            IPAddress NodeIpAddress = IPAddress.Parse(Obj_Settings.IpInfo.Public);
            if (Obj_Settings.LocalNode == true)
            {
                NodeIpAddress = IPAddress.Parse(Obj_Settings.IpInfo.Local);
            }
            HttpObj.DebugMode = Obj_Settings.DebugMode;
            HttpObj.InfoMode = Obj_Settings.InfoMode;
            HttpObj.Settings = Obj_Settings;
            HttpObj.StoreUrl = false;
            HttpObj.Start(NodeIpAddress, SelectedPortVal);
            Notus.Print.Basic(Obj_Settings, "Http Has Started", false);
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
