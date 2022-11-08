using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using NVG = Notus.Variable.Globals;
using NGF = Notus.Variable.Globals.Functions;
using System.Collections.Concurrent;

namespace Notus.Wallet
{
    public class Balance : IDisposable
    {
        //this store balance to Dictionary list
        private ConcurrentDictionary<string, Notus.Variable.Struct.WalletBalanceStruct> SummaryList =
            new ConcurrentDictionary<string, Notus.Variable.Struct.WalletBalanceStruct>();
        //private Notus.Mempool ObjMp_Balance;

        //private Notus.Mempool ObjMp_WalletUsage;
        //private Notus.Mempool ObjMp_LockWallet;
        private Notus.Mempool ObjMp_MultiWalletParticipant;
        private Notus.Mempool ObjMp_WalletsICanApprove;
        private ConcurrentDictionary<string, Notus.Variable.Enum.MultiWalletType> MultiWalletTypeList=new ConcurrentDictionary<string, Variable.Enum.MultiWalletType>();
        
        public List<string> WalletsICanApprove(string WalletId)
        {
            string multiParticipantStr = ObjMp_WalletsICanApprove.Get(WalletId, "");
            if (multiParticipantStr == "")
            {
                return new List<string>();
            }
            List<string>? participantList = new List<string>();
            try
            {
                participantList = JsonSerializer.Deserialize<List<string>>(multiParticipantStr);
            }
            catch
            {
            }
            if (participantList == null)
            {
                return new List<string>();
            }
            return participantList;
        }
        public Notus.Variable.Enum.MultiWalletType GetMultiWalletType(string MultiSignatureWalletId)
        {
            if (MultiWalletTypeList.ContainsKey(MultiSignatureWalletId))
            {
                return MultiWalletTypeList[MultiSignatureWalletId];
            }
            return Variable.Enum.MultiWalletType.Unknown;
        }
        public List<string> GetParticipant(string MultiSignatureWalletId)
        {
            string multiParticipantStr = ObjMp_MultiWalletParticipant.Get(MultiSignatureWalletId, "");
            if (multiParticipantStr == "")
            {
                return new List<string>();
            }
            List<string>? participantList = new List<string>();
            try
            {
                participantList = JsonSerializer.Deserialize<List<string>>(multiParticipantStr);
            }
            catch
            {
            }
            if (participantList == null)
            {
                return new List<string>();
            }
            return participantList;
        }
        // bu fonksiyonlar ile cüzdanın kilitlenmesi durumuna bakalım
        public bool WalletUsageAvailable(string walletKey)
        {
            return (NGF.WalletUsageList.ContainsKey(walletKey) == false ? true : false);
        }
        public bool StartWalletUsage(string walletKey)
        {
            if (WalletUsageAvailable(walletKey) == false)
            {
                return false;
            }
            if(NGF.WalletUsageList.ContainsKey(walletKey) == false)
            {
                NGF.WalletUsageList.TryAdd(walletKey, 1);
            }
            return true;
        }
        public void StopWalletUsage(string walletKey)
        {
            NGF.WalletUsageList.TryRemove(walletKey, out _);
        }

        // omergoksoy
        private void StoreToDb(Notus.Variable.Struct.WalletBalanceStruct BalanceObj)
        {
            if (SummaryList.ContainsKey(BalanceObj.Wallet) == false)
            {
                SummaryList.TryAdd(BalanceObj.Wallet, BalanceObj);
            }
            else
            {
                SummaryList[BalanceObj.Wallet] = BalanceObj;
            }
            //ObjMp_Balance.Set(BalanceObj.Wallet, JsonSerializer.Serialize(BalanceObj), true);
            //burada cüzdan kilidi açılacak...
            StopWalletUsage(BalanceObj.Wallet);
        }
        public Notus.Variable.Struct.WalletBalanceResponseStruct ReadFromNode(string WalletKey)
        {
            bool exitInnerLoop = false;
            while (exitInnerLoop == false)
            {
                for (int a = 0; a < Notus.Variable.Constant.ListMainNodeIp.Count && exitInnerLoop == false; a++)
                {
                    string nodeIpAddress = Notus.Variable.Constant.ListMainNodeIp[a];
                    try
                    {
                        //bool RealNetwork = PreTransfer.Network == Notus.Variable.Enum.NetworkType.Const_MainNetwork;
                        string fullUrlAddress =
                            Notus.Network.Node.MakeHttpListenerPath(
                                nodeIpAddress,
                                Notus.Network.Node.GetNetworkPort(NVG.Settings.Network, Notus.Variable.Enum.NetworkLayer.Layer1)
                            ) + "balance/" + WalletKey + "/";

                        string MainResultStr = Notus.Communication.Request.Get(fullUrlAddress, 10, true).GetAwaiter().GetResult();
                        Notus.Variable.Struct.WalletBalanceResponseStruct tmpTransferResult = JsonSerializer.Deserialize<Notus.Variable.Struct.WalletBalanceResponseStruct>(MainResultStr);
                        return tmpTransferResult;
                    }
                    catch (Exception err)
                    {
                        Notus.Print.Log(
                            Notus.Variable.Enum.LogLevel.Info,
                            2354874,
                            err.Message,
                            "BlockRowNo",
                            null,
                            err
                        );
                        Notus.Print.Basic(true, "Error Text [8ahgd6s4d]: " + err.Message);
                    }
                }
            }
            return null;
        }
        public Notus.Variable.Struct.WalletBalanceStruct Get(string WalletKey, ulong timeYouCanUse)
        {
            if (SummaryList.ContainsKey(WalletKey) == true)
            {
                return SummaryList[WalletKey];
            }

            string defaultCoinTag = Notus.Variable.Constant.MainCoinTagName;
            if (NVG.Settings != null)
            {
                if (NVG.Settings.Genesis != null)
                {
                    if (NVG.Settings.Genesis.CoinInfo != null)
                    {
                        if (NVG.Settings.Genesis.CoinInfo.Tag.Length > 0)
                        {
                            defaultCoinTag = NVG.Settings.Genesis.CoinInfo.Tag;
                        }
                    }

                }
            }
            timeYouCanUse=(timeYouCanUse == 0 ? NVG.NOW.Int : timeYouCanUse) ;
            return new Notus.Variable.Struct.WalletBalanceStruct()
            {
                Balance = new Dictionary<string, Dictionary<ulong, string>>()
                {
                    {
                        defaultCoinTag,
                        new Dictionary<ulong, string>(){
                            { timeYouCanUse ,"0" }
                        }
                    },
                },
                RowNo = 0,
                UID = "",
                Wallet = WalletKey
            };
        }
        /*
        public BigInteger GetCoinBalance(string WalletKey)
        {
            return GetCoinBalance(Get(WalletKey));
        }
        */

        //bu fonksiyon hesaptan çıkartma işlemi yapıyor
        public Dictionary<string, Dictionary<ulong, string>> ReAssign(Dictionary<string, Dictionary<ulong, string>> balanceObj)
        {
            Dictionary<string, Dictionary<ulong, string>> tmpResult = new Dictionary<string, Dictionary<ulong, string>>();
            foreach (KeyValuePair<string, Dictionary<ulong, string>> currencyEntry in balanceObj)
            {
                tmpResult.Add(currencyEntry.Key, new Dictionary<ulong, string>());
                foreach (KeyValuePair<ulong, string> balanceEntry in currencyEntry.Value)
                {
                    tmpResult[currencyEntry.Key].Add(balanceEntry.Key, balanceEntry.Value);
                }
            }
            return tmpResult;
        }
        /*

        controlpoint
        controlpoint
        controlpoint
        controlpoint


        BURAYA CÜZDAN KİLİTLEME İŞLEMİ EKLE
        KİLİTLEME OLAYI ŞU : AYNI ANDA BİRDEN FAZLA BAKİYE GİRİŞ VE ÇIKIŞI İŞLEMİNE İZİN VERMEMESİ
        İÇİN GEÇİCİ OLARAK İŞLEMİN KİLİTLENMESİ DURUMU

        AYRICA ÇÖZMEK İÇİNDE BİR FONKSİYON VEYA İŞLEM EKLE

        */
        public Dictionary<ulong, string> RemoveZeroUnlockTime(Dictionary<ulong, string> currentBalance)
        {
            ulong removeKey = 0;
            foreach(var entry in currentBalance)
            {
                if (entry.Value == "0")
                {
                    removeKey = entry.Key;
                }
            }
            if (removeKey > 0)
            {
                currentBalance.Remove(removeKey);
                currentBalance = RemoveZeroUnlockTime(currentBalance);
            }
            return currentBalance;
        }
        public (bool, Notus.Variable.Struct.WalletBalanceStruct) SubtractVolumeWithUnlockTime(
            Notus.Variable.Struct.WalletBalanceStruct balanceObj,
            string volume,
            string coinTagName,
            ulong unlockTime = 0
        )
        {
            if (unlockTime == 0)
            {
                unlockTime = NVG.NOW.Int;
            }
            bool volumeError = true;
            // first parametre hata oluşması durumunda
            if (balanceObj.Balance.ContainsKey(coinTagName) == false)
            {
                return (volumeError, balanceObj);
            }

            BigInteger volumeNeeded = BigInteger.Parse(volume);
            foreach (KeyValuePair<ulong, string> entry in balanceObj.Balance[coinTagName])
            {
                if (unlockTime > entry.Key)
                {
                    if (volumeNeeded > 0)
                    {
                        BigInteger currentTimeVolume = BigInteger.Parse(entry.Value);
                        if (currentTimeVolume > volumeNeeded)
                        {
                            BigInteger resultVolume = currentTimeVolume - volumeNeeded;
                            balanceObj.Balance[coinTagName][entry.Key] = resultVolume.ToString();
                            volumeNeeded = 0;
                        }
                        else
                        {
                            volumeNeeded = volumeNeeded - currentTimeVolume;
                            balanceObj.Balance[coinTagName][entry.Key] = "0";
                        }
                    }
                }
            }

            if (volumeNeeded == 0)
            {
                //balanceObj.Balance[coinTagName] = RemoveZeroUnlockTime(balanceObj.Balance[coinTagName]);
                return (false, balanceObj);
            }
            return (true, balanceObj);
        }
        public Notus.Variable.Struct.WalletBalanceStruct AddVolumeWithUnlockTime(
            Notus.Variable.Struct.WalletBalanceStruct balanceObj, 
            string volume, 
            string coinTagName, 
            ulong unlockTime
        )
        {
            if (balanceObj.Balance.ContainsKey(coinTagName) == false)
            {
                balanceObj.Balance.Add(coinTagName, new Dictionary<ulong, string>()
                {
                    { unlockTime,volume }
                }
                );
                return balanceObj;
            }
            if (balanceObj.Balance[coinTagName].ContainsKey(unlockTime) == false)
            {
                balanceObj.Balance[coinTagName].Add(unlockTime, volume);
                return balanceObj;
            }
            BigInteger totalVolume = BigInteger.Parse(balanceObj.Balance[coinTagName][unlockTime]) + BigInteger.Parse(volume);
            balanceObj.Balance[coinTagName][unlockTime] = totalVolume.ToString();
            //balanceObj.Balance[coinTagName] = RemoveZeroUnlockTime(balanceObj.Balance[coinTagName]);
            return balanceObj;
        }
        public bool HasEnoughCoin(string walletKey, BigInteger howMuchCoinNeed, string CoinTagName = "")
        {
            if (NVG.Settings == null)
            {
                return false;
            }
            if (NVG.Settings.Genesis == null)
            {
                return false;
            }
            if (CoinTagName.Length == 0)
            {
                CoinTagName = NVG.Settings.Genesis.CoinInfo.Tag;
            }
            Notus.Variable.Struct.WalletBalanceStruct tmpGeneratorBalanceObj = Get(walletKey, 0);
            BigInteger currentVolume = GetCoinBalance(tmpGeneratorBalanceObj, CoinTagName);
            if (howMuchCoinNeed > currentVolume)
            {
                return false;
            }
            return true;
        }
        public BigInteger GetCoinBalance(Notus.Variable.Struct.WalletBalanceStruct tmpBalanceObj, string CoinTagName)
        {
            if (tmpBalanceObj.Balance.ContainsKey(CoinTagName) == false)
            {
                return 0;
            }

            BigInteger resultVal = 0;
            ulong exactTimeLong = NVG.NOW.Int;
            foreach (KeyValuePair<ulong, string> entry in tmpBalanceObj.Balance[CoinTagName])
            {
                if (exactTimeLong > entry.Key)
                {
                    resultVal += BigInteger.Parse(entry.Value);
                }
            }
            return resultVal;
        }
        public bool AccountIsLock(string WalletKey)
        {
            string unlockTimeStr = "";
            if (NGF.LockWalletList.ContainsKey(WalletKey) == true)
            {
                unlockTimeStr=NGF.LockWalletList[WalletKey];
            }

            /*
            string unlockTimeStr = ObjMp_LockWallet.Get(
                Notus.Toolbox.Text.ToHex(WalletKey),
                ""
            );
            */

            if (unlockTimeStr.Length > 0)
            {
                if (ulong.TryParse(unlockTimeStr, out ulong unlockTimeLong))
                {
                    DateTime unlockTime = Notus.Date.ToDateTime(unlockTimeStr);
                    if (NVG.NOW.Obj > unlockTime)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /*
        //control-local-block
        private void StoreToTemp(Notus.Variable.Class.BlockData? tmpBlockData)
        {
            if (tmpBlockData != null)
            {
                string fileName = tmpBlockData.info.uID + ".tmp";
                string folderName = Notus.IO.GetFolderName(NVG.Settings, Notus.Variable.Constant.StorageFolderName.TempBlock);
                string fullPath = folderName + fileName;
                string blockStr = JsonSerializer.Serialize(tmpBlockData);
                using (StreamWriter writer = new StreamWriter(fullPath))
                {
                    writer.WriteLine(blockStr);
                }
            }
            else
            {
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Error,
                    1111199999,
                    "Block Is NULL",
                    "BlockRowNo",
                    NVG.Settings,
                    null
                );
            }
        }
        */
        public void Control(Notus.Variable.Class.BlockData tmpBlockForBalance)
        {
            //bloklar geçici dosyaya kaydediliyor...
            //control-local-block
            //StoreToTemp(tmpBlockForBalance);

            // genesis block
            if (tmpBlockForBalance.info.type == Notus.Variable.Enum.BlockTypeList.GenesisBlock)
            {
                ulong coinStartingTime = Notus.Block.Key.BlockIdToUlong(tmpBlockForBalance.info.uID);


                Notus.Wallet.Block.ClearList(NVG.Settings.Network, NVG.Settings.Layer);

                Notus.Wallet.Block.Add2List(NVG.Settings.Network, NVG.Settings.Layer, new Notus.Variable.Struct.CurrencyListStorageStruct()
                {
                    Detail = new Notus.Variable.Struct.CurrencyList()
                    {
                        Logo = new Notus.Variable.Struct.FileStorageStruct()
                        {
                            Base64 = NVG.Settings.Genesis.CoinInfo.Logo.Base64,
                            Source = NVG.Settings.Genesis.CoinInfo.Logo.Source,
                            Url = NVG.Settings.Genesis.CoinInfo.Logo.Url,
                            Used = NVG.Settings.Genesis.CoinInfo.Logo.Used
                        },
                        Name = NVG.Settings.Genesis.CoinInfo.Name,
                        ReserveCurrency = true,
                        Tag = NVG.Settings.Genesis.CoinInfo.Tag,
                    },
                    Uid = tmpBlockForBalance.info.uID
                });
                //Notus.Wallet.Currency.Add2List(NVG.Settings.Network, NVG.Settings.Genesis.CoinInfo.Tag, tmpBlockForBalance.info.uID);

                //ObjMp_Balance.Clear();
                //ObjMp_LockWallet.Clear();
                NGF.LockWalletList.Clear();
                NGF.WalletUsageList.Clear();
                //ObjMp_WalletUsage.Clear();
                //Console.WriteLine("kontrol-2");
                ObjMp_MultiWalletParticipant.Clear();
                ObjMp_WalletsICanApprove.Clear();
                MultiWalletTypeList.Clear();
                string tmpBalanceStr = NVG.Settings.Genesis.Premining.PreSeed.Volume.ToString();
                if (NVG.Settings.Genesis.Premining.PreSeed.DecimalContains == false)
                {
                    tmpBalanceStr = tmpBalanceStr + Notus.Toolbox.Text.RepeatString(NVG.Settings.Genesis.Reserve.Decimal, "0");
                }
                StoreToDb(new Notus.Variable.Struct.WalletBalanceStruct()
                {
                    UID = tmpBlockForBalance.info.uID,
                    RowNo = tmpBlockForBalance.info.rowNo,
                    Wallet = NVG.Settings.Genesis.Premining.PreSeed.Wallet,
                    Balance = new Dictionary<string, Dictionary<ulong, string>>()
                    {
                        {
                            NVG.Settings.Genesis.CoinInfo.Tag,
                            new Dictionary<ulong, string>()
                            {
                                {
                                    coinStartingTime, tmpBalanceStr
                                }
                            }
                        }
                    }
                });


                tmpBalanceStr = NVG.Settings.Genesis.Premining.Private.Volume.ToString();
                if (NVG.Settings.Genesis.Premining.Private.DecimalContains == false)
                {
                    tmpBalanceStr = tmpBalanceStr + Notus.Toolbox.Text.RepeatString(NVG.Settings.Genesis.Reserve.Decimal, "0");
                }
                StoreToDb(new Notus.Variable.Struct.WalletBalanceStruct()
                {
                    UID = tmpBlockForBalance.info.uID,
                    RowNo = tmpBlockForBalance.info.rowNo,
                    Wallet = NVG.Settings.Genesis.Premining.Private.Wallet,
                    Balance = new Dictionary<string, Dictionary<ulong, string>>()
                    {
                        {
                            NVG.Settings.Genesis.CoinInfo.Tag,
                            new Dictionary<ulong, string>()
                            {
                                {
                                    coinStartingTime, tmpBalanceStr
                                }
                            }
                        }
                    }
                });


                tmpBalanceStr = NVG.Settings.Genesis.Premining.Public.Volume.ToString();
                if (NVG.Settings.Genesis.Premining.Public.DecimalContains == false)
                {
                    tmpBalanceStr = tmpBalanceStr + Notus.Toolbox.Text.RepeatString(NVG.Settings.Genesis.Reserve.Decimal, "0");
                }
                StoreToDb(new Notus.Variable.Struct.WalletBalanceStruct()
                {
                    UID = tmpBlockForBalance.info.uID,
                    RowNo = tmpBlockForBalance.info.rowNo,
                    Wallet = NVG.Settings.Genesis.Premining.Public.Wallet,
                    Balance = new Dictionary<string, Dictionary<ulong, string>>()
                    {
                        {
                            NVG.Settings.Genesis.CoinInfo.Tag,
                            new Dictionary<ulong, string>()
                            {
                                {
                                    coinStartingTime, tmpBalanceStr
                                }
                            }
                        }
                    }
                });
            }
            else
            {
                //Console.WriteLine("Balance -> Line 205");
                //Console.WriteLine("Buradaki kontroller hata düzeltmelerinden sonra tekrar aktive edilece.");
                //Console.WriteLine("Buradaki kontroller hata düzeltmelerinden sonra tekrar aktive edilece.");
                //Console.ReadLine();
                //Console.ReadLine();
            }

            /*
            if (
                tmpBlockForBalance.info.type != Notus.Variable.Enum.BlockTypeList.EmptyBlock
                &&
                tmpBlockForBalance.info.type != Notus.Variable.Enum.BlockTypeList.GenesisBlock
            )
            {
                //Notus.Print.Basic(NVG.Settings, tmpBlockForBalance.info.uID);
                Notus.Print.Basic(NVG.Settings, "Balance.Cs -> Control function -> Line 178 -> Block type -> " + tmpBlockForBalance.info.type.ToString() + " -> " + tmpBlockForBalance.info.rowNo.ToString());
            }
            */
            // MultiWalletCryptoTransfer
            
            if (tmpBlockForBalance.info.type == Notus.Variable.Enum.BlockTypeList.MultiWalletCryptoTransfer)
            {
                string tmpRawDataStr = System.Text.Encoding.UTF8.GetString(
                    System.Convert.FromBase64String(
                        tmpBlockForBalance.cipher.data
                    )
                );
                //Console.WriteLine(tmpRawDataStr);
                Dictionary<string, Notus.Variable.Struct.MultiWalletTransactionStruct>? tmpBalanceVal =
                    JsonSerializer.Deserialize<Dictionary<string, Notus.Variable.Struct.MultiWalletTransactionStruct>>(
                        tmpRawDataStr
                    );
                if (tmpBalanceVal != null)
                {
                    foreach (KeyValuePair<string, Variable.Struct.MultiWalletTransactionStruct> outerEntry in tmpBalanceVal)
                    {
                        foreach (KeyValuePair<string, Dictionary<string, Dictionary<ulong, string>>> innerEntry in outerEntry.Value.After)
                        {
                            StoreToDb(new Notus.Variable.Struct.WalletBalanceStruct()
                            {
                                UID = tmpBlockForBalance.info.uID,
                                RowNo = tmpBlockForBalance.info.rowNo,
                                Wallet = innerEntry.Key,
                                Balance = innerEntry.Value
                            });
                        }
                    }
                }
            }

            //LockAccount
            if (tmpBlockForBalance.info.type == Notus.Variable.Enum.BlockTypeList.LockAccount)
            {
                string tmpRawDataStr = System.Text.Encoding.UTF8.GetString(
                    System.Convert.FromBase64String(
                        tmpBlockForBalance.cipher.data
                    )
                );
                Notus.Variable.Struct.LockWalletStruct? tmpLockBalance =
                    JsonSerializer.Deserialize<Notus.Variable.Struct.LockWalletStruct>(
                        tmpRawDataStr
                    );
                if (tmpLockBalance != null)
                {
                    if (tmpLockBalance.Out != null)
                    {
                        StoreToDb(new Notus.Variable.Struct.WalletBalanceStruct()
                        {
                            UID = tmpBlockForBalance.info.uID,
                            RowNo = tmpBlockForBalance.info.rowNo,
                            Wallet = tmpLockBalance.WalletKey,
                            Balance = tmpLockBalance.Out
                        });
                    }
                    string pureStr = System.Text.Encoding.UTF8.GetString(
                        System.Convert.FromBase64String(
                            tmpBlockForBalance.cipher.data
                        )
                    );

                    if (NGF.LockWalletList.ContainsKey(tmpLockBalance.WalletKey) == false)
                    {
                        NGF.LockWalletList.TryAdd(
                            tmpLockBalance.WalletKey,
                            tmpLockBalance.UnlockTime.ToString()
                        );
                    }
                    /*
                    ObjMp_LockWallet.Set(
                        Notus.Toolbox.Text.ToHex(tmpLockBalance.WalletKey),
                        tmpLockBalance.UnlockTime.ToString(),
                        true
                    );
                    */
                }
            }

            //Airdrop
            if (tmpBlockForBalance.info.type == Notus.Variable.Enum.BlockTypeList.AirDrop)
            {
                string tmpRawDataStr = System.Text.Encoding.UTF8.GetString(
                    System.Convert.FromBase64String(
                        tmpBlockForBalance.cipher.data
                    )
                );
                Notus.Variable.Class.BlockStruct_125? tmpLockBalance =
                    JsonSerializer.Deserialize<Notus.Variable.Class.BlockStruct_125>(
                        tmpRawDataStr
                    );
                if (tmpLockBalance != null)
                {
                    foreach(var entry in tmpLockBalance.Out)
                    {
                        StoreToDb(new Notus.Variable.Struct.WalletBalanceStruct()
                        {
                            UID = tmpBlockForBalance.info.uID,
                            RowNo = tmpBlockForBalance.info.rowNo,
                            Wallet = entry.Key,
                            Balance = entry.Value
                        });
                        NGF.Balance.StopWalletUsage(entry.Key);
                    }
                }
            }
            //CryptoTransfer
            if (tmpBlockForBalance.info.type == Notus.Variable.Enum.BlockTypeList.CryptoTransfer)
            {
                string tmpRawDataStr = System.Text.Encoding.UTF8.GetString(
                    System.Convert.FromBase64String(
                        tmpBlockForBalance.cipher.data
                    )
                );
                Notus.Variable.Class.BlockStruct_120? tmpBalanceVal =
                    JsonSerializer.Deserialize<Notus.Variable.Class.BlockStruct_120>(
                        tmpRawDataStr
                    );
                foreach (KeyValuePair<string, Dictionary<string, Dictionary<ulong, string>>> entry in tmpBalanceVal.Out)
                {
                    StoreToDb(new Notus.Variable.Struct.WalletBalanceStruct()
                    {
                        UID = tmpBlockForBalance.info.uID,
                        RowNo = tmpBlockForBalance.info.rowNo,
                        Wallet = entry.Key,
                        Balance = entry.Value
                    });
                }
            }

            //MultiWalletContract 
            if (tmpBlockForBalance.info.type == Notus.Variable.Enum.BlockTypeList.MultiWalletContract)
            {
                string tmpRawDataStr = System.Text.Encoding.UTF8.GetString(
                    System.Convert.FromBase64String(
                        tmpBlockForBalance.cipher.data
                    )
                );
                Notus.Variable.Struct.MultiWalletStoreStruct? tmpBalanceVal = JsonSerializer.Deserialize<Notus.Variable.Struct.MultiWalletStoreStruct>(tmpRawDataStr);
                if (tmpBalanceVal == null)
                {
                    Console.WriteLine("tmpRawDataStr -> Balance.Cs -> 493. Line");
                    Console.WriteLine(tmpRawDataStr);
                }
                else
                {

                    //string multiParticipantStr = ObjMp_MultiWalletParticipant.Get(tmpBalanceVal.MultiWalletKey, "");

                    //multi wallet cüzdanın katılımcılarını tutan mempool listesi
                    List<string> participantList = GetParticipant(tmpBalanceVal.MultiWalletKey);
                    for(int i=0;i< tmpBalanceVal.WalletList.Count; i++)
                    {
                        if (participantList.IndexOf(tmpBalanceVal.WalletList[i]) == -1)
                        {
                            participantList.Add(tmpBalanceVal.WalletList[i]);
                        }

                        List<string> walletIcanApprove = WalletsICanApprove(tmpBalanceVal.WalletList[i]);
                        if (walletIcanApprove.IndexOf(tmpBalanceVal.MultiWalletKey) == -1)
                        {
                            walletIcanApprove.Add(tmpBalanceVal.MultiWalletKey);
                            ObjMp_WalletsICanApprove.Set(
                                tmpBalanceVal.WalletList[i],
                                JsonSerializer.Serialize(walletIcanApprove),
                                true
                            );
                        }

                        //WalletsICanApprove()
                        //
                    }

                    if (MultiWalletTypeList.ContainsKey(tmpBalanceVal.MultiWalletKey) == false)
                    {
                        MultiWalletTypeList.TryAdd(tmpBalanceVal.MultiWalletKey, tmpBalanceVal.VoteType);
                    }
                    else
                    {
                        MultiWalletTypeList[tmpBalanceVal.MultiWalletKey]=tmpBalanceVal.VoteType;
                    }
                    ObjMp_MultiWalletParticipant.Set(
                        tmpBalanceVal.MultiWalletKey,
                        JsonSerializer.Serialize(participantList),
                        true
                    );


                    //Console.WriteLine(JsonSerializer.Serialize(participantList, Notus.Variable.Constant.JsonSetting));
                    //multi wallet cüzdanın katılımcılarını tutan mempool listesi
                    //List<string> participantList = GetParticipant(tmpBalanceVal.MultiWalletKey);

                    StoreToDb(new Notus.Variable.Struct.WalletBalanceStruct()
                    {
                        UID = tmpBalanceVal.Balance.UID,
                        RowNo = tmpBalanceVal.Balance.RowNo,
                        Wallet = tmpBalanceVal.Founder.WalletKey,
                        Balance = tmpBalanceVal.Balance.Balance
                    });
                    
                    StoreToDb(new Notus.Variable.Struct.WalletBalanceStruct()
                    {
                        UID = tmpBlockForBalance.info.uID,
                        RowNo = tmpBlockForBalance.info.rowNo,
                        Wallet = tmpBalanceVal.MultiWalletKey,
                        Balance = new Dictionary<string, Dictionary<ulong, string>>(){
                        {
                            NVG.Settings.Genesis.CoinInfo.Tag,
                            new Dictionary<ulong, string>(){
                                {
                                    Notus.Block.Key.BlockIdToUlong(tmpBalanceVal.Balance.UID) , "0"
                                }
                            }
                        }
                    }
                    });

                    //Console.WriteLine("Multi Signature Wallet -> Balance.Cs -> 498. Line");
                    //Console.WriteLine(JsonSerializer.Serialize(tmpBalanceVal, Notus.Variable.Constant.JsonSetting));
                }
            }
            /*
            if (tmpBlockForBalance.info.type == 240)
            {
                string tmpRawDataStr = System.Text.Encoding.UTF8.GetString(
                    System.Convert.FromBase64String(
                        tmpBlockForBalance.cipher.data
                    )
                );
                Notus.Variable.Struct.StorageOnChainStruct tmpBalanceVal = JsonSerializer.Deserialize<Notus.Variable.Struct.StorageOnChainStruct>(tmpRawDataStr);
                StoreToDb(new Notus.Variable.Struct.WalletBalanceStruct()
                {
                    UID = tmpBalanceVal.Balance.UID,
                    RowNo = tmpBalanceVal.Balance.RowNo,
                    Wallet = tmpBalanceVal.Balance.Wallet,
                    Balance = tmpBalanceVal.Balance.Balance
                });
            }
            */

            // TokenGeneration
            if (tmpBlockForBalance.info.type == 160)
            {
                Notus.Variable.Struct.BlockStruct_160? tmpBalanceVal = JsonSerializer.Deserialize<Notus.Variable.Struct.BlockStruct_160>(
                    System.Text.Encoding.UTF8.GetString(
                        System.Convert.FromBase64String(
                            tmpBlockForBalance.cipher.data
                        )
                    )
                );
                if (tmpBalanceVal != null)
                {
                    Int64 BlockFee = Notus.Wallet.Fee.Calculate(tmpBalanceVal, NVG.Settings.Network);
                    string WalletKeyStr = Notus.Wallet.ID.GetAddressWithPublicKey(tmpBalanceVal.Creation.PublicKey, NVG.Settings.Network);
                    Notus.Variable.Struct.WalletBalanceStruct CurrentBalance = Get(WalletKeyStr, 0);
                    string TokenBalanceStr = tmpBalanceVal.Reserve.Supply.ToString();


                    ulong tmpBlockTime = ulong.Parse(tmpBlockForBalance.info.time.Substring(0, 17));
                    CurrentBalance.Balance.Add(tmpBalanceVal.Info.Tag, new Dictionary<ulong, string>()
                    {
                        { tmpBlockTime, TokenBalanceStr }
                    }
                    );

                    (bool tmpErrorStatus, var newBalanceVal) =
                        SubtractVolumeWithUnlockTime(
                            CurrentBalance,
                            BlockFee.ToString(),
                            NVG.Settings.Genesis.CoinInfo.Tag,
                            tmpBlockTime
                        );
                    /*
                    CurrentBalance.Balance[NVG.Settings.Genesis.CoinInfo.Tag] =
                        (BigInteger.Parse(CurrentBalance.Balance[NVG.Settings.Genesis.CoinInfo.Tag]) -
                        BlockFee).ToString();
                    */

                    StoreToDb(new Notus.Variable.Struct.WalletBalanceStruct()
                    {
                        Balance = newBalanceVal.Balance,
                        RowNo = tmpBlockForBalance.info.rowNo,
                        UID = tmpBlockForBalance.info.uID,
                        Wallet = Notus.Wallet.ID.GetAddressWithPublicKey(tmpBalanceVal.Creation.PublicKey, NVG.Settings.Network)
                    }
                    );

                    Notus.Wallet.Block.Add2List(NVG.Settings.Network, NVG.Settings.Layer, new Notus.Variable.Struct.CurrencyListStorageStruct()
                    {
                        Uid = tmpBlockForBalance.info.uID,
                        Detail = new Notus.Variable.Struct.CurrencyList()
                        {
                            ReserveCurrency = false,
                            Name = tmpBalanceVal.Info.Name,
                            Tag = tmpBalanceVal.Info.Tag,
                            Logo = new Notus.Variable.Struct.FileStorageStruct()
                            {
                                Base64 = tmpBalanceVal.Info.Logo.Base64,
                                Source = tmpBalanceVal.Info.Logo.Source,
                                Url = tmpBalanceVal.Info.Logo.Url,
                                Used = tmpBalanceVal.Info.Logo.Used
                            }
                        }
                    }
                    );
                }
            }

        }
        public void Start()
        {
            /*
            ObjMp_Balance = new Notus.Mempool(
                Notus.IO.GetFolderName(NVG.Settings.Network, NVG.Settings.Layer, Notus.Variable.Constant.StorageFolderName.Balance) +
                "account_balance"
            );
            ObjMp_Balance.AsyncActive = true;
            ObjMp_Balance.Clear();
            */
            /*
            ObjMp_LockWallet = new Notus.Mempool(
                Notus.IO.GetFolderName(NVG.Settings.Network, NVG.Settings.Layer, Notus.Variable.Constant.StorageFolderName.Balance) +
                "account_lock"
            );
            ObjMp_LockWallet.AsyncActive = false;
            ObjMp_LockWallet.Clear();
            */
            /*
            ObjMp_WalletUsage = new Notus.Mempool(
                Notus.IO.GetFolderName(NVG.Settings.Network, NVG.Settings.Layer, Notus.Variable.Constant.StorageFolderName.Balance) +
                "wallet_usage"
            );
            ObjMp_WalletUsage.AsyncActive = false;
            ObjMp_WalletUsage.Clear();
            */
            //Console.WriteLine("kontrol-1");
            ObjMp_MultiWalletParticipant = new Notus.Mempool(
                Notus.IO.GetFolderName(NVG.Settings.Network, NVG.Settings.Layer, Notus.Variable.Constant.StorageFolderName.Balance) +
                "multi_wallet_participant"
            );
            
            ObjMp_MultiWalletParticipant.AsyncActive = false;
            //Console.WriteLine("kontrol-4");
            ObjMp_MultiWalletParticipant.Clear();
            //Console.WriteLine("kontrol-3");

            ObjMp_WalletsICanApprove = new Notus.Mempool(
                Notus.IO.GetFolderName(NVG.Settings.Network, NVG.Settings.Layer, Notus.Variable.Constant.StorageFolderName.Balance) +
                "wallet_i_can_approve"
            );

            ObjMp_WalletsICanApprove.AsyncActive = false;
            ObjMp_WalletsICanApprove.Clear();
            MultiWalletTypeList.Clear();
        }
        public Balance()
        {
        }
        ~Balance()
        {
            Dispose();
        }
        public void Dispose()
        {
            /*
            try
            {
                if (ObjMp_Balance != null)
                {
                    ObjMp_Balance.Dispose();
                }
            }
            catch (Exception err)
            {
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Info,
                    897989784,
                    err.Message,
                    "BlockRowNo",
                    null,
                    err
                );
            }
            try
            {
                if (ObjMp_LockWallet != null)
                {
                    ObjMp_LockWallet.Dispose();
                }
            }
            catch (Exception err)
            {
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Info,
                    8754213,
                    err.Message,
                    "BlockRowNo",
                    null,
                    err
                );
            }
            */
            /*
            try
            {
                if (ObjMp_WalletUsage != null)
                {
                    ObjMp_WalletUsage.Dispose();
                }
            }
            catch (Exception err)
            {
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Info,
                    8754293,
                    err.Message,
                    "BlockRowNo",
                    null,
                    err
                );
            }
            */

            try
            {
                if (ObjMp_MultiWalletParticipant != null)
                {
                    ObjMp_MultiWalletParticipant.Dispose();
                }
            }
            catch (Exception err)
            {
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Info,
                    8754279,
                    err.Message,
                    "BlockRowNo",
                    null,
                    err
                );
            }
            MultiWalletTypeList.Clear();

            try
            {
                if (ObjMp_WalletsICanApprove != null)
                {
                    ObjMp_WalletsICanApprove.Dispose();
                }
            }
            catch (Exception err)
            {
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Info,
                    8754290,
                    err.Message,
                    "BlockRowNo",
                    null,
                    err
                );
            }
            
        }
    }
}
