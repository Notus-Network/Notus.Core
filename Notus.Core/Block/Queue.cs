using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;

namespace Notus.Block
{
    public class Queue : IDisposable
    {
        private DateTime LastNtpTime = Notus.Variable.Constant.DefaultTime;
        private TimeSpan NtpTimeDifference;
        private bool NodeTimeAfterNtpTime = false;      // time difference before or after NTP Server

        private Notus.Variable.Common.ClassSetting Obj_Settings;
        public Notus.Variable.Common.ClassSetting Settings
        {
            get { return Obj_Settings; }
            set { Obj_Settings = value; }
        }

        private Notus.Mempool MP_BlockPoolList;
        private Notus.Block.Storage BS_Storage;
        private Dictionary<int, List<Notus.Variable.Struct.List_PoolBlockRecordStruct>> Obj_PoolTransactionList =
            new Dictionary<int, List<Notus.Variable.Struct.List_PoolBlockRecordStruct>>();
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

        public Notus.Variable.Class.BlockData OrganizeBlockOrder(Notus.Variable.Class.BlockData CurrentBlock)
        {
            CurrentBlock.info.rowNo = Obj_Settings.LastBlock.info.rowNo + 1;

            CurrentBlock.prev = Obj_Settings.LastBlock.info.uID + Obj_Settings.LastBlock.sign;
            CurrentBlock.info.prevList.Clear();
            foreach (KeyValuePair<int, string> entry in Obj_Settings.LastBlock.info.prevList)
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
        public Notus.Variable.Struct.PoolBlockRecordStruct? Get(DateTime currentUtcTime, Notus.Wallet.Balance BalanceObj)
        {
            DateTime startingTime = DateTime.Now;
            if (Queue_PoolTransaction.Count == 0)
            {
                return null;
            }

            int CurrentBlockType = -1;
            List<string> TempWalletList = new List<string>() { Obj_Settings.NodeWallet.WalletKey };

            List<string> TempBlockList = new List<string>();
            List<Notus.Variable.Struct.List_PoolBlockRecordStruct> TempPoolTransactionList = new List<Notus.Variable.Struct.List_PoolBlockRecordStruct>();
            bool exitLoop = false;
            while (exitLoop == false)
            {
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
                            if (CurrentBlockType == 120)
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
                                        exitLoop = true;
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
                                TempPoolTransactionList.Count == 1000 ||
                                CurrentBlockType == 240 || // layer1 - > dosya ekleme isteği
                                CurrentBlockType == 250 || // layer3 - > dosya içeriği
                                CurrentBlockType == 300
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
            BlockStruct.info.uID = Notus.Block.Key.Generate(GetNtpTime(), Obj_Settings.NodeWallet.WalletKey);

            if (CurrentBlockType == 360)
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

                if (CurrentBlockType == 40)
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
                        string lockAccountFee = Obj_Settings.Genesis.Fee.BlockAccount.ToString();
                        Notus.Variable.Struct.WalletBalanceStruct currentBalance =
                            BalanceObj.Get(tmpLockWalletStruct.WalletKey, 0);
                        (bool tmpBalanceResult, Notus.Variable.Struct.WalletBalanceStruct tmpNewGeneratorBalance) =
                            BalanceObj.SubtractVolumeWithUnlockTime(
                                BalanceObj.Get(tmpLockWalletStruct.WalletKey, 0),
                                lockAccountFee,
                                Obj_Settings.Genesis.CoinInfo.Tag,
                                Notus.Time.NowToUlong()
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
                                            Balance = BalanceObj.ReAssign(currentBalance.Balance),
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

                if (CurrentBlockType == 120)
                {
                    if (TempBlockList.Count > 1)
                    {
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
                        }
                        TempBlockList.Clear();
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

        public Notus.Variable.Class.BlockData? ReadFromChain(string BlockId)
        {
            return BS_Storage.ReadBlock(BlockId);
        }
        //yeni blok hesaplanması tamamlandığı zaman buraya gelecek ve geçerli blok ise eklenecek.
        public void AddToChain(Notus.Variable.Class.BlockData NewBlock)
        {
            Notus.Print.Log(
                Notus.Variable.Enum.LogLevel.Info,
                70,
                JsonSerializer.Serialize(NewBlock),
                "AddToChain",
                null,
                null
            );

            BS_Storage.Add(NewBlock);

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
                RemoveKeyStr = GiveBlockKey(rawDataStr);
            }
            MP_BlockPoolList.Remove(RemoveKeyStr);
        }

        public void Reset()
        {
            Notus.Archive.ClearBlocks(Obj_Settings);
            MP_BlockPoolList.Clear();
            Queue_PoolTransaction.Clear();
            Obj_PoolTransactionList.Clear();
        }
        public void Add(Notus.Variable.Struct.PoolBlockRecordStruct? PreBlockData)
        {
            if (PreBlockData != null)
            {
                if (Obj_Settings != null)
                {
                    if (Obj_Settings.NodeWallet != null)
                    {
                        string blockKeyStr = Notus.Block.Key.Generate(GetNtpTime(), Obj_Settings.NodeWallet.WalletKey);
                        Add2Queue(PreBlockData, blockKeyStr);
                        string PreBlockDataStr = JsonSerializer.Serialize(PreBlockData);
                        string keyStr = GiveBlockKey(PreBlockData.data);
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

                        if (keyStr.Length > 0)
                        {
                            MP_BlockPoolList.Set(keyStr, PreBlockDataStr, true);
                        }
                    }
                }
            }
        }

        public void AddEmptyBlock()
        {
            Add(new Notus.Variable.Struct.PoolBlockRecordStruct()
            {
                type = 300,
                data = JsonSerializer.Serialize(Obj_Settings.LastBlock.info.rowNo)
            });
        }
        public string GiveBlockKey(string BlockDataStr)
        {
            return
                new Notus.Hash().CommonHash("md5", BlockDataStr) +
                new Notus.Hash().CommonHash("sha1", BlockDataStr);
        }
        private void Add2Queue(Notus.Variable.Struct.PoolBlockRecordStruct? PreBlockData, string BlockKeyStr)
        {
            if (PreBlockData != null)
            {
                if (Obj_PoolTransactionList.ContainsKey(PreBlockData.type) == false)
                {
                    Obj_PoolTransactionList.Add(
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
                Queue_PoolTransaction.Enqueue(new Notus.Variable.Struct.List_PoolBlockRecordStruct()
                {
                    key = BlockKeyStr,
                    type = PreBlockData.type,
                    data = PreBlockData.data
                });
            }
        }
        public void Start()
        {
            BS_Storage = new Notus.Block.Storage(false);
            BS_Storage.Network = Obj_Settings.Network;
            BS_Storage.Layer = Obj_Settings.Layer;
            BS_Storage.Start();

            MP_BlockPoolList = new Notus.Mempool(
                Notus.IO.GetFolderName(Obj_Settings.Network, Obj_Settings.Layer, Notus.Variable.Constant.StorageFolderName.Common) +
                Notus.Variable.Constant.MemoryPoolName["BlockPoolList"]
            );
            MP_BlockPoolList.Each((string blockTransactionKey, string TextBlockDataString) =>
            {
                Notus.Variable.Struct.PoolBlockRecordStruct? PreBlockData = JsonSerializer.Deserialize<Notus.Variable.Struct.PoolBlockRecordStruct>(TextBlockDataString);
                if (PreBlockData != null)
                {
                    Add2Queue(PreBlockData, blockTransactionKey);
                }
            });
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
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Info,
                    864578,
                    err.Message,
                    "BlockRowNo",
                    Obj_Settings,
                    err
                );

                Notus.Print.Danger(Obj_Settings, "Error -> Notus.Block.Queue");
                Notus.Print.Danger(Obj_Settings, err.Message);
                Notus.Print.Danger(Obj_Settings, "Error -> Notus.Block.Queue");
            }
        }
    }
}
