using Notus.Compression.TGZ;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using NGF = Notus.Variable.Globals.Functions;
using NVG = Notus.Variable.Globals;
using NP = Notus.Print;

namespace Notus.Block
{
    public class Queue : IDisposable
    {
        public bool CheckPoolDb = false;                // time difference before or after NTP Server

        private Notus.Mempool MP_BlockPoolList;
        private Notus.Block.Storage BS_Storage;
        private ConcurrentDictionary<string, byte> PoolIdList = new ConcurrentDictionary<string, byte>();
        private ConcurrentDictionary<int, List<Notus.Variable.Struct.List_PoolBlockRecordStruct>> Obj_PoolTransactionList =
            new ConcurrentDictionary<int, List<Notus.Variable.Struct.List_PoolBlockRecordStruct>>();
        private Queue<Notus.Variable.Struct.List_PoolBlockRecordStruct> Queue_PoolTransaction = new Queue<Notus.Variable.Struct.List_PoolBlockRecordStruct>();

        //bu foknsiyonun görevi blok sırası ve önceki değerlerini blok içeriğine eklemek
        public List<Notus.Variable.Struct.List_PoolBlockRecordStruct>? GetPoolList(int BlockType)
        {
            if (Obj_PoolTransactionList.ContainsKey(BlockType))
            {
                return Obj_PoolTransactionList[BlockType];
            }
            return null;
        }
        public Dictionary<int, int>? GetPoolCount()
        {
            Dictionary<int, int> resultList = new Dictionary<int, int>();
            foreach (KeyValuePair<int, List<Notus.Variable.Struct.List_PoolBlockRecordStruct>> entry in Obj_PoolTransactionList)
            {
                resultList.Add(entry.Key, entry.Value.Count);
            }
            return resultList;
        }

        public Notus.Variable.Class.BlockData OrganizeBlockOrder(Notus.Variable.Class.BlockData CurrentBlock)
        {
            CurrentBlock.info.rowNo = NVG.Settings.LastBlock.info.rowNo + 1;

            CurrentBlock.prev = NVG.Settings.LastBlock.info.uID + NVG.Settings.LastBlock.sign;
            CurrentBlock.info.prevList.Clear();
            foreach (KeyValuePair<int, string> entry in NVG.Settings.LastBlock.info.prevList)
            {
                if (entry.Value != "")
                {
                    CurrentBlock.info.prevList.Add(entry.Key, entry.Value);
                }
            }
            if (CurrentBlock.info.prevList.ContainsKey(CurrentBlock.info.type))
            {
                CurrentBlock.info.prevList[CurrentBlock.info.type] = CurrentBlock.prev;
            }
            else
            {
                CurrentBlock.info.prevList.Add(CurrentBlock.info.type, CurrentBlock.prev);
            }
            return CurrentBlock;
        }


        //bu fonksiyon ile işlem yapılacak aynı türden bloklar sırası ile listeden çekilip geri gönderilecek
        public Notus.Variable.Struct.PoolBlockRecordStruct? Get(
            ulong WaitingForPool
            //, 
            //DateTime BlockGenerationTime
        )
        {
            if (Queue_PoolTransaction.Count == 0)
            {
                //Console.WriteLine("sifir");
                //PoolIdList.Clear();
                return null;
            }

            int diffBetween = System.Convert.ToInt32(MP_BlockPoolList.Count() / Queue_PoolTransaction.Count);
            if (diffBetween > 10)
            {
                CheckPoolDb = true;
                //Console.WriteLine("CheckPoolDb = true;");
            }
            else
            {
                //Console.WriteLine("CheckPoolDb = false;");
                if (MP_BlockPoolList.Count() < 10)
                {
                    CheckPoolDb = true;
                }
                if (Queue_PoolTransaction.Count < 10)
                {
                    CheckPoolDb = true;
                }
            }

            int CurrentBlockType = -1;
            List<string> TempWalletList = new List<string>() { NVG.Settings.NodeWallet.WalletKey };

            List<string> TempBlockList = new List<string>();
            List<Notus.Variable.Struct.List_PoolBlockRecordStruct> TempPoolTransactionList = new List<Notus.Variable.Struct.List_PoolBlockRecordStruct>();
            bool exitLoop = false;
            string transactionId = string.Empty;
            while (exitLoop == false)
            {
                //NGF.UpdateUtcNowValue();
                if (Queue_PoolTransaction.Count > 0)
                {
                    Notus.Variable.Struct.List_PoolBlockRecordStruct? TmpPoolRecord = Queue_PoolTransaction.Peek();
                    if (TmpPoolRecord == null)
                    {
                        exitLoop = true;
                    }
                    else
                    {
                        if (CurrentBlockType == -1)
                        {
                            CurrentBlockType = TmpPoolRecord.type;
                        }

                        if (CurrentBlockType == TmpPoolRecord.type)
                        {
                            bool addToList = true;
                            if (CurrentBlockType == Notus.Variable.Enum.BlockTypeList.MultiWalletCryptoTransfer)
                            {
                                Dictionary<string, Notus.Variable.Struct.MultiWalletTransactionStruct>? multiTx =
                                    JsonSerializer.Deserialize<
                                        Dictionary<string, Notus.Variable.Struct.MultiWalletTransactionStruct>
                                    >(TmpPoolRecord.data);
                                if (multiTx == null)
                                {
                                    addToList = false;
                                }
                                else
                                {
                                    foreach (var iEntry in multiTx)
                                    {
                                        if (transactionId.Length == 0)
                                        {
                                            transactionId = iEntry.Key;
                                        }
                                    }
                                }
                            }

                            if (CurrentBlockType == Notus.Variable.Enum.BlockTypeList.AirDrop)
                            {
                                Notus.Variable.Class.BlockStruct_125? tmpBlockCipherData = JsonSerializer.Deserialize<Notus.Variable.Class.BlockStruct_125>(TmpPoolRecord.data);
                                if (tmpBlockCipherData == null)
                                {
                                    addToList = false;
                                }
                                else
                                {
                                    // out işlemindeki cüzdanları kontrol ediyor...
                                    foreach (KeyValuePair<string, Dictionary<string, Dictionary<ulong, string>>> tmpEntry in tmpBlockCipherData.Out)
                                    {
                                        if (TempWalletList.IndexOf(tmpEntry.Key) == -1)
                                        {
                                            TempWalletList.Add(tmpEntry.Key);
                                        }
                                        else
                                        {
                                            addToList = false;
                                        }
                                    }

                                    if (addToList == false)
                                    {
                                        Queue_PoolTransaction.Enqueue(TmpPoolRecord);
                                        Obj_PoolTransactionList[CurrentBlockType].Add(TmpPoolRecord);
                                    }
                                }
                            }

                            if (CurrentBlockType == Notus.Variable.Enum.BlockTypeList.CryptoTransfer)
                            {
                                Notus.Variable.Class.BlockStruct_120? tmpBlockCipherData = JsonSerializer.Deserialize<Notus.Variable.Class.BlockStruct_120>(TmpPoolRecord.data);
                                if (tmpBlockCipherData == null)
                                {
                                    addToList = false;
                                }
                                else
                                {
                                    // out işlemindeki cüzdanları kontrol ediyor...
                                    foreach (KeyValuePair<string, Dictionary<string, Dictionary<ulong, string>>> tmpEntry in tmpBlockCipherData.Out)
                                    {
                                        if (TempWalletList.IndexOf(tmpEntry.Key) == -1)
                                        {
                                            TempWalletList.Add(tmpEntry.Key);
                                        }
                                        else
                                        {
                                            addToList = false;
                                        }
                                    }

                                    if (addToList == false)
                                    {
                                        Queue_PoolTransaction.Enqueue(TmpPoolRecord);
                                        Obj_PoolTransactionList[CurrentBlockType].Add(TmpPoolRecord);
                                        //exitLoop = true;
                                    }
                                }
                            }

                            if (addToList == true)
                            {
                                TempPoolTransactionList.Add(TmpPoolRecord);
                                TempBlockList.Add(TmpPoolRecord.data);
                            }

                            Queue_PoolTransaction.Dequeue();
                            Obj_PoolTransactionList[CurrentBlockType].RemoveAt(0);
                            if (
                                TempPoolTransactionList.Count == Notus.Variable.Constant.BlockTransactionLimit ||
                                CurrentBlockType == 240 || // layer1 - > dosya ekleme isteği
                                CurrentBlockType == 250 || // layer3 - > dosya içeriği
                                CurrentBlockType == Notus.Variable.Enum.BlockTypeList.EmptyBlock ||
                                CurrentBlockType == Notus.Variable.Enum.BlockTypeList.MultiWalletCryptoTransfer
                            )
                            {
                                exitLoop = true;
                            }
                        }
                        else
                        {
                            exitLoop = true;
                        }
                    }
                }
                else
                {
                    exitLoop = true;
                }
                if (NVG.NOW.Int >= WaitingForPool)
                {
                    exitLoop = true;
                }
            }
            if (TempPoolTransactionList.Count == 0)
            {
                return null;
            }

            Notus.Variable.Class.BlockData BlockStruct = Notus.Variable.Class.Block.GetEmpty();

            if (Notus.Variable.Constant.BlockNonceType.ContainsKey(CurrentBlockType) == true)
            {
                BlockStruct.info.nonce.type = Notus.Variable.Constant.BlockNonceType[CurrentBlockType];     // 1-Slide, 2-Bounce
            }
            else
            {
                BlockStruct.info.nonce.type = Notus.Variable.Constant.Default_BlockNonceType;     // 1-Slide, 2-Bounce
            }

            if (Notus.Variable.Constant.BlockNonceMethod.ContainsKey(CurrentBlockType) == true)
            {
                BlockStruct.info.nonce.method = Notus.Variable.Constant.BlockNonceMethod[CurrentBlockType];   // which hash algorithm
            }
            else
            {
                BlockStruct.info.nonce.method = Notus.Variable.Constant.Default_BlockNonceMethod;   // which hash algorithm
            }

            if (Notus.Variable.Constant.BlockDifficulty.ContainsKey(CurrentBlockType) == true)
            {
                BlockStruct.info.nonce.difficulty = Notus.Variable.Constant.BlockDifficulty[CurrentBlockType];  // block difficulty level
            }
            else
            {
                BlockStruct.info.nonce.difficulty = Notus.Variable.Constant.Default_BlockDifficulty;  // block difficulty level
            }

            string LongNonceText = string.Empty;

            BlockStruct.cipher.ver = "NE";
            if (transactionId.Length == 0)
            {
                BlockStruct.info.uID = NGF.GenerateTxUid();
                //BlockStruct.info.uID = Notus.Block.Key.Generate(GetNtpTime(), NVG.Settings.NodeWallet.WalletKey);
            }
            else
            {
                BlockStruct.info.uID = transactionId;
            }

            if (CurrentBlockType == Notus.Variable.Enum.BlockTypeList.GenesisBlock)
            {
                LongNonceText = TempPoolTransactionList[0].data;
                BlockStruct.info.rowNo = 1;
                BlockStruct.info.multi = false;
                BlockStruct.info.uID = Notus.Variable.Constant.GenesisBlockUid;
            }
            else
            {
                //BLOCK UNIQUE ID'Sİ BURADA EKLENİYOR....
                //BLOCK UNIQUE ID'Sİ BURADA EKLENİYOR....
                //BLOCK UNIQUE ID'Sİ BURADA EKLENİYOR....
                //BLOCK UNIQUE ID'Sİ BURADA EKLENİYOR....
                //BLOCK UNIQUE ID'Sİ BURADA EKLENİYOR....
                // buraya UTC time verisi parametre olarak gönderilecek
                // böylece blok için alınan zaman bilgisi ortak bir zaman olacak
                //Notus.Variable.Enum.BlockTypeList.CryptoTransfer

                if (CurrentBlockType == 240)
                {
                    List<string> tmpUploadStatus = JsonSerializer.Deserialize<List<string>>(TempBlockList[0]);
                    BlockStruct.info.uID = tmpUploadStatus[0];
                    TempBlockList.Clear();
                    TempBlockList.Add(tmpUploadStatus[1]);
                }

                if (CurrentBlockType == 250)
                {
                    string tmpFileName = TempBlockList[0];
                    TempBlockList.Clear();
                    TempBlockList.Add(
                        System.Convert.ToBase64String(File.ReadAllBytes(tmpFileName))
                    );
                }

                if (CurrentBlockType == Notus.Variable.Enum.BlockTypeList.LockAccount)
                {
                    string tmpLockWalletKey = TempBlockList[0];

                    Notus.Variable.Struct.LockWalletBeforeStruct? tmpLockWalletStruct = JsonSerializer.Deserialize<Notus.Variable.Struct.LockWalletBeforeStruct>(tmpLockWalletKey);
                    TempBlockList.Clear();
                    if (tmpLockWalletStruct == null)
                    {
                        TempBlockList.Add(
                            JsonSerializer.Serialize(
                                new Notus.Variable.Struct.LockWalletStruct()
                                {
                                    WalletKey = "",
                                    Balance = null,
                                    Out = null,
                                    UnlockTime = 0,
                                    PublicKey = "",
                                    Sign = "",
                                }
                            )
                        );
                    }
                    else
                    {
                        Console.WriteLine("Queue.Cs -> Line 354");
                        Console.WriteLine(tmpLockWalletStruct.WalletKey);
                        string lockAccountFee = NVG.Settings.Genesis.Fee.BlockAccount.ToString();
                        Notus.Variable.Struct.WalletBalanceStruct currentBalance =
                            NGF.Balance.Get(tmpLockWalletStruct.WalletKey, 0);
                        (bool tmpBalanceResult, Notus.Variable.Struct.WalletBalanceStruct tmpNewGeneratorBalance) =
                            NGF.Balance.SubtractVolumeWithUnlockTime(
                                NGF.Balance.Get(tmpLockWalletStruct.WalletKey, 0),
                                lockAccountFee,
                                NVG.Settings.Genesis.CoinInfo.Tag
                            );
                        if (tmpBalanceResult == false)
                        {
                            /*
                            foreach (KeyValuePair<string, Dictionary<ulong, string>> curEntry in tmpNewGeneratorBalance.Balance)
                            {
                                foreach (KeyValuePair<ulong, string> balanceEntry in curEntry.Value)
                                {
                                    if (tmpLockWalletStruct.UnlockTime > balanceEntry.Key){

                                        tmpNewGeneratorBalance.Balance[curEntry.Key]
                                    }
                                }
                            }
                            */

                            TempBlockList.Add(
                                JsonSerializer.Serialize(
                                    new Notus.Variable.Struct.LockWalletStruct()
                                    {
                                        WalletKey = tmpLockWalletStruct.WalletKey,
                                        Balance = new Notus.Variable.Class.WalletBalanceStructForTransaction()
                                        {
                                            Balance = NGF.Balance.ReAssign(currentBalance.Balance),
                                            Wallet = tmpLockWalletStruct.WalletKey,
                                            WitnessBlockUid = currentBalance.UID,
                                            WitnessRowNo = currentBalance.RowNo
                                        },
                                        Out = tmpNewGeneratorBalance.Balance,
                                        Fee = lockAccountFee,
                                        UnlockTime = tmpLockWalletStruct.UnlockTime,
                                        PublicKey = tmpLockWalletStruct.PublicKey,
                                        Sign = tmpLockWalletStruct.Sign
                                    }
                                )
                            );
                        }
                        else
                        {
                            Console.WriteLine("Balance result true");
                            Console.WriteLine("burada true dönüş yapınca, JSON convert işlemi hata veriyor");
                            Console.WriteLine("buraya true dönmesi durumunda bloğu oluşturmamak için kontrol eklensin");
                            Console.WriteLine("Balance result true");
                        }
                        BlockStruct.info.uID = tmpLockWalletStruct.UID;
                    }
                }

                if (CurrentBlockType == Notus.Variable.Enum.BlockTypeList.AirDrop)
                {
                    //Console.WriteLine(JsonSerializer.Serialize( TempBlockList));
                    //Console.ReadLine();
                    if (TempBlockList.Count > 1)
                    {
                        Notus.Variable.Class.BlockStruct_125 tmpBlockCipherData = new Variable.Class.BlockStruct_125()
                        {
                            //Sender=Notus.Variable.Constant.NetworkProgramWallet
                            In = new Dictionary<string, Notus.Variable.Struct.WalletBalanceStruct>(),
                            Out = new Dictionary<string, Dictionary<string, Dictionary<ulong, string>>>(),
                            Validator = string.Empty
                        };

                        for (int i = 0; i < TempBlockList.Count; i++)
                        {
                            Notus.Variable.Class.BlockStruct_125? tmpInnerData = JsonSerializer.Deserialize<Notus.Variable.Class.BlockStruct_125>(TempBlockList[i]);
                            if (tmpInnerData != null)
                            {
                                foreach (var iEntry in tmpInnerData.In)
                                {
                                    tmpBlockCipherData.In.Add(iEntry.Key, iEntry.Value);
                                }
                                foreach (var iEntry in tmpInnerData.Out)
                                {
                                    tmpBlockCipherData.Out.Add(iEntry.Key, iEntry.Value);
                                }
                                tmpBlockCipherData.Validator = tmpInnerData.Validator;
                            }
                            else
                            {
                                Console.WriteLine("TempBlockList[i] IS NULL");
                                Console.WriteLine(TempBlockList[i]);
                                Console.WriteLine("TempBlockList[i] IS NULL");
                            }
                        }
                        TempBlockList.Clear();
                        //Console.WriteLine(JsonSerializer.Serialize(tmpBlockCipherData, Notus.Variable.Constant.JsonSetting));
                        //Console.ReadLine();
                        TempBlockList.Add(JsonSerializer.Serialize(tmpBlockCipherData));
                    }
                    //Console.WriteLine(TempBlockList);
                    //Console.ReadLine();
                }
                if (CurrentBlockType == Notus.Variable.Enum.BlockTypeList.CryptoTransfer)
                {
                    if (TempBlockList.Count > 1)
                    {
                        //Console.WriteLine(JsonSerializer.Serialize( TempBlockList));
                        Notus.Variable.Class.BlockStruct_120 tmpBlockCipherData = new Variable.Class.BlockStruct_120()
                        {
                            In = new Dictionary<string, Variable.Class.BlockStruct_120_In_Struct>(),
                            Out = new Dictionary<string, Dictionary<string, Dictionary<ulong, string>>>(),
                            Validator = new Variable.Struct.ValidatorStruct()
                        };

                        bool validatorAssigned = false;
                        for (int i = 0; i < TempBlockList.Count; i++)
                        {
                            Notus.Variable.Class.BlockStruct_120? tmpInnerData = JsonSerializer.Deserialize<Notus.Variable.Class.BlockStruct_120>(TempBlockList[i]);
                            if (tmpInnerData != null)
                            {
                                if (validatorAssigned == false)
                                {
                                    tmpBlockCipherData.Validator = tmpInnerData.Validator;
                                    validatorAssigned = true;
                                }
                                else
                                {
                                    BigInteger tmpFee =
                                        BigInteger.Parse(tmpBlockCipherData.Validator.Reward)
                                        +
                                        BigInteger.Parse(tmpInnerData.Validator.Reward);
                                    tmpBlockCipherData.Validator.Reward = tmpFee.ToString();
                                }

                                foreach (KeyValuePair<string, Variable.Class.BlockStruct_120_In_Struct> iEntry in tmpInnerData.In)
                                {
                                    tmpBlockCipherData.In.Add(iEntry.Key, iEntry.Value);
                                }
                                foreach (KeyValuePair<string, Dictionary<string, Dictionary<ulong, string>>> iEntry in tmpInnerData.Out)
                                {
                                    tmpBlockCipherData.Out.Add(iEntry.Key, iEntry.Value);
                                }
                            }
                            else
                            {
                                Console.WriteLine("TempBlockList[i] IS NULL");
                                Console.WriteLine(TempBlockList[i]);
                                Console.WriteLine("TempBlockList[i] IS NULL");
                            }
                        }
                        TempBlockList.Clear();
                        //Console.WriteLine(tmpBlockCipherData.Out.Count)
                        TempBlockList.Add(JsonSerializer.Serialize(tmpBlockCipherData));
                    }
                }
                LongNonceText = string.Join(Notus.Variable.Constant.CommonDelimeterChar, TempBlockList.ToArray());
            }

            BlockStruct.prev = "";
            BlockStruct.info.prevList.Clear();

            BlockStruct.info.time = Notus.Block.Key.GetTimeFromKey(BlockStruct.info.uID, true);

            BlockStruct.info.type = CurrentBlockType;
            /*
            if (CurrentBlockType == 300)
            {
                BlockStruct.cipher.data = LongNonceText;
            }
            else
            {
            }
            */
            BlockStruct.cipher.data = System.Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes(
                    LongNonceText
                )
            );

            //burası pooldaki kayıtların fazla birikmesi ve para transferi işlemlerinin key'lerinin örtüşmemesinden
            //dolayı eklendi
            for (int i = 0; i < TempPoolTransactionList.Count; i++)
            {
                //Console.WriteLine("Control-Point-a001");
                //Console.WriteLine("Remove Key : " + TempPoolTransactionList[i].key);
                MP_BlockPoolList.Remove(TempPoolTransactionList[i].key,true);
                PoolIdList.TryRemove(TempPoolTransactionList[i].key, out _);
            }

            return
                new Notus.Variable.Struct.PoolBlockRecordStruct()
                {
                    type = CurrentBlockType,
                    data = JsonSerializer.Serialize(BlockStruct)
                };
        }


        /*
        
        buraya blok sıra numarası ile okuma işlemi eklenecek
        public (bool, Notus.Variable.Class.BlockData) ReadWithRowNo(Int64 BlockRowNo)
        {
            if (Obj_LastBlock.info.rowNo >= BlockNumber)
            {
                bool exitPrevWhile = false;
                string PrevBlockIdStr = Obj_LastBlock.prev;
                while (exitPrevWhile == false)
                {
                    PrevBlockIdStr = PrevBlockIdStr.Substring(0, 90);
                    (bool BlockExist, Notus.Variable.Class.BlockData tmpStoredBlock) = ReadFromChain(PrevBlockIdStr);
                    if (BlockExist == true)
                    {
                        if (tmpStoredBlock.info.rowNo == BlockRowNo)
                        {
                            return (true,tmpStoredBlock);
                        }
                        else
                        {
                            PrevBlockIdStr = tmpStoredBlock.prev;
                        }
                    }
                    else
                    {
                        exitPrevWhile = true;
                    }
                }
            }
        }
        */

        public Notus.Variable.Class.BlockData? ReadFromChain(string BlockId)
        {
            //tgz-exception
            return BS_Storage.ReadBlock(BlockId);
        }
        //yeni blok hesaplanması tamamlandığı zaman buraya gelecek ve geçerli blok ise eklenecek.
        public void AddToChain(Notus.Variable.Class.BlockData NewBlock)
        {
            /*
            if (NewBlock.prev.Length < 20)
            {
                NP.Info("Block Added To Chain -> " +
                    NewBlock.info.rowNo.ToString() + " -> " +
                    "Prev is Empty [ " + NewBlock.prev + " ]"
                );
            }
            else
            {
                NP.Info("Block Added To Chain -> " + 
                    NewBlock.info.rowNo.ToString() + " -> " + 
                    NewBlock.prev.Substring(0,20)
                );
            }
            */
            BS_Storage.AddSync(NewBlock);

            string rawDataStr = Notus.Toolbox.Text.RawCipherData2String(
                NewBlock.cipher.data
            );

            string RemoveKeyStr = string.Empty;
            if (NewBlock.info.type == 40)
            {
                Notus.Variable.Struct.LockWalletStruct? tmpTransferResult =
                    JsonSerializer.Deserialize<Notus.Variable.Struct.LockWalletStruct>(rawDataStr);
                if (tmpTransferResult != null)
                {
                    RemoveKeyStr = Notus.Toolbox.Text.ToHex("lock-" + tmpTransferResult.WalletKey);
                }
            }
            else
            {
                //Console.WriteLine("Silinecek Block Anahtari Bilinmiyor");
                //RemoveKeyStr = GiveBlockKey(rawDataStr);
            }
            //Console.WriteLine("Control-Point-a055");
            //Console.WriteLine("Remove Key From Pool : " + RemoveKeyStr);
            MP_BlockPoolList.Remove(RemoveKeyStr,true);
        }

        public void Reset()
        {
            Notus.Archive.ClearBlocks(NVG.Settings);
            MP_BlockPoolList.Clear();
            Queue_PoolTransaction.Clear();
            Obj_PoolTransactionList.Clear();
        }
        public bool Add(Notus.Variable.Struct.PoolBlockRecordStruct? PreBlockData,bool addedToPoolDb=true)
        {
            if (PreBlockData == null)
            {
                Console.WriteLine("Block.Queue.Cs -> Line 668 -> PreBlockData = NULL");
                return false;
            }
            if (NVG.Settings == null)
            {
                Console.WriteLine("Block.Queue.Cs -> Line 673 -> NVG.Settings = NULL");
                return false;
            }
            if (NVG.Settings.NodeWallet == null)
            {
                Console.WriteLine("Block.Queue.Cs -> Line 678 -> NVG.Settings.NodeWallet = NULL");
                return false;
            }

            string PreBlockDataStr = JsonSerializer.Serialize(PreBlockData);
            if (PreBlockData.uid == null)
            {
                PreBlockData.uid = NGF.GenerateTxUid();
            }

            Add2Queue(PreBlockData, PreBlockData.uid);
            string keyStr = PreBlockData.uid;
            if (PreBlockData.type == 40)
            {
                Notus.Variable.Struct.LockWalletBeforeStruct? tmpLockWalletData = JsonSerializer.Deserialize<Notus.Variable.Struct.LockWalletBeforeStruct>(PreBlockData.data);
                if (tmpLockWalletData != null)
                {
                    keyStr = Notus.Toolbox.Text.ToHex("lock-" + tmpLockWalletData.WalletKey);
                }
                else
                {
                    keyStr = "";
                }
            }
            //Console.WriteLine("keyStr : " + keyStr);
            if (keyStr.Length > 0)
            {
                if (addedToPoolDb == true)
                {
                    MP_BlockPoolList.Set(keyStr, PreBlockDataStr, true);
                }
            }
            return true;
        }

        public void AddEmptyBlock()
        {
            Add(new Notus.Variable.Struct.PoolBlockRecordStruct()
            {
                uid = NGF.GenerateTxUid(),
                type = Notus.Variable.Enum.BlockTypeList.EmptyBlock,
                data = JsonSerializer.Serialize(NVG.Settings.LastBlock.info.rowNo)
            },false);
        }

        private void Add2Queue(Notus.Variable.Struct.PoolBlockRecordStruct PreBlockData, string BlockKeyStr)
        {
            //Console.WriteLine(PreBlockData.type.ToString() + " - " +BlockKeyStr.Substring(0, 20));
            if (PoolIdList.ContainsKey(BlockKeyStr) == false)
            {
                bool added = PoolIdList.TryAdd(BlockKeyStr, 1);
                if (Obj_PoolTransactionList.ContainsKey(PreBlockData.type) == false)
                {
                    Obj_PoolTransactionList.TryAdd(
                        PreBlockData.type,
                        new List<Variable.Struct.List_PoolBlockRecordStruct>() { }
                    );
                }
                Obj_PoolTransactionList[PreBlockData.type].Add(
                    new Notus.Variable.Struct.List_PoolBlockRecordStruct()
                    {
                        key = BlockKeyStr,
                        type = PreBlockData.type,
                        data = PreBlockData.data
                    }
                );
                //Console.WriteLine("BlockKeyStr : " + BlockKeyStr);
                Queue_PoolTransaction.Enqueue(new Notus.Variable.Struct.List_PoolBlockRecordStruct()
                {
                    key = BlockKeyStr,
                    type = PreBlockData.type,
                    data = PreBlockData.data
                });
            }
        }
        public void LoadFromPoolDb()
        {
            MP_BlockPoolList.Each((string blockTransactionKey, string TextBlockDataString) =>
            {
                Notus.Variable.Struct.PoolBlockRecordStruct? PreBlockData =
                JsonSerializer.Deserialize<Notus.Variable.Struct.PoolBlockRecordStruct>(TextBlockDataString);
                if (PreBlockData != null)
                {
                    Add2Queue(PreBlockData, blockTransactionKey);
                }
                else
                {
                    Console.WriteLine("Queue -> Line 687");
                    Console.WriteLine("Queue -> Line 687");
                    Console.WriteLine(blockTransactionKey);
                    Console.WriteLine(TextBlockDataString);
                }
            }, 3000);
        }
        public void Start()
        {
            BS_Storage = new Notus.Block.Storage(false);
            BS_Storage.Start();

            MP_BlockPoolList = new Notus.Mempool(
                Notus.IO.GetFolderName(NVG.Settings, Notus.Variable.Constant.StorageFolderName.Common) +
                Notus.Variable.Constant.MemoryPoolName["BlockPoolList"]
            );
            MP_BlockPoolList.AsyncActive = false;
            LoadFromPoolDb();
            CheckPoolDb = false;
        }
        public Queue()
        {
            Obj_PoolTransactionList.Clear();

            Queue_PoolTransaction.Clear();
        }
        ~Queue()
        {
            Dispose();
        }
        public void Dispose()
        {
            try
            {
                MP_BlockPoolList.Dispose();
            }
            catch (Exception err)
            {
                NP.Danger(NVG.Settings, "Error -> Notus.Block.Queue");
                NP.Danger(NVG.Settings, err.Message);
                NP.Danger(NVG.Settings, "Error -> Notus.Block.Queue");
            }
        }
    }
}
