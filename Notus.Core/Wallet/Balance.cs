using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
namespace Notus.Wallet
{
    public class Balance : IDisposable
    {
        private Notus.Variable.Common.ClassSetting Obj_Settings;
        public Notus.Variable.Common.ClassSetting Settings
        {
            get { return Obj_Settings; }
            set { Obj_Settings = value; }
        }
        private Notus.Mempool ObjMp_Balance;

        private void StoreToDb(Notus.Variable.Struct.WalletBalanceStruct BalanceObj)
        {
            ObjMp_Balance.Set(BalanceObj.Wallet, JsonSerializer.Serialize(BalanceObj), true);
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
                                Notus.Network.Node.GetNetworkPort(Obj_Settings.Network, Notus.Variable.Enum.NetworkLayer.Layer1)
                            ) + "balance/" + WalletKey + "/";

                        string MainResultStr = Notus.Communication.Request.Get(fullUrlAddress, 10, true).GetAwaiter().GetResult();
                        Notus.Variable.Struct.WalletBalanceResponseStruct tmpTransferResult = JsonSerializer.Deserialize<Notus.Variable.Struct.WalletBalanceResponseStruct>(MainResultStr);
                        return tmpTransferResult;
                    }
                    catch (Exception err)
                    {
                        Notus.Print.Basic(true, "Error Text [8ae5cf]: " + err.Message);
                    }
                }
            }
            return null;
        }

        public Notus.Variable.Struct.WalletBalanceStruct Get(string WalletKey, ulong timeYouCanUse)
        {
            string BalanceValStr = ObjMp_Balance.Get(WalletKey, string.Empty);
            if (BalanceValStr == string.Empty)
            {
                if (timeYouCanUse == 0)
                {
                    timeYouCanUse = Notus.Time.NowToUlong();
                }
                return new Notus.Variable.Struct.WalletBalanceStruct()
                {
                    Balance = new Dictionary<string, Dictionary<ulong, string>>()
                    {
                        {
                            Settings.Genesis.CoinInfo.Tag,
                            new Dictionary<ulong, string>(){
                                { Notus.Time.NowToUlong(),"0" }
                            }
                        },
                    },
                    RowNo = 0,
                    UID = "",
                    Wallet = WalletKey
                };
            }
            Notus.Variable.Struct.WalletBalanceStruct tmpBalanceVal = JsonSerializer.Deserialize<Notus.Variable.Struct.WalletBalanceStruct>(BalanceValStr);
            return tmpBalanceVal;
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
        public (bool, Notus.Variable.Struct.WalletBalanceStruct) SubtractVolumeWithUnlockTime(Notus.Variable.Struct.WalletBalanceStruct balanceObj, string volume, string coinTagName, ulong unlockTime)
        {
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
                return (false, balanceObj);
            }
            return (true, balanceObj);
        }
        public Notus.Variable.Struct.WalletBalanceStruct AddVolumeWithUnlockTime(Notus.Variable.Struct.WalletBalanceStruct balanceObj, string volume, string coinTagName, ulong unlockTime)
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
            return balanceObj;
        }
        public BigInteger GetCoinBalance(Notus.Variable.Struct.WalletBalanceStruct tmpBalanceObj, string CoinTagName)
        {
            if (tmpBalanceObj.Balance.ContainsKey(CoinTagName) == false)
            {
                return 0;
            }

            BigInteger resultVal = 0;
            ulong exactTimeLong = Notus.Time.DateTimeToUlong(DateTime.Now);
            foreach (KeyValuePair<ulong, string> entry in tmpBalanceObj.Balance[CoinTagName])
            {
                if (exactTimeLong > entry.Key)
                {
                    resultVal += BigInteger.Parse(entry.Value);
                }
            }
            return resultVal;
        }
        public void Control(Notus.Variable.Class.BlockData tmpBlockForBalance)
        {
            // genesis block
            if (tmpBlockForBalance.info.type == 360)
            {
                ulong coinStartingTime = Notus.Time.BlockIdToUlong(tmpBlockForBalance.info.uID);


                Notus.Wallet.Currency.ClearList(Obj_Settings.Network, Obj_Settings.Layer);

                Notus.Wallet.Currency.Add2List(Obj_Settings.Network, Obj_Settings.Layer, new Notus.Variable.Struct.CurrencyListStorageStruct()
                {
                    Detail = new Notus.Variable.Struct.CurrencyList()
                    {
                        Logo = new Notus.Variable.Struct.FileStorageStruct()
                        {
                            Base64 = Obj_Settings.Genesis.CoinInfo.Logo.Base64,
                            Source = Obj_Settings.Genesis.CoinInfo.Logo.Source,
                            Url = Obj_Settings.Genesis.CoinInfo.Logo.Url,
                            Used = Obj_Settings.Genesis.CoinInfo.Logo.Used
                        },
                        Name = Obj_Settings.Genesis.CoinInfo.Name,
                        ReserveCurrency = true,
                        Tag = Obj_Settings.Genesis.CoinInfo.Tag,
                    },
                    Uid = tmpBlockForBalance.info.uID
                });
                //Notus.Wallet.Currency.Add2List(Obj_Settings.Network, Obj_Settings.Genesis.CoinInfo.Tag, tmpBlockForBalance.info.uID);

                ObjMp_Balance.Clear();
                string tmpBalanceStr = Obj_Settings.Genesis.Premining.PreSeed.Volume.ToString();
                if (Obj_Settings.Genesis.Premining.PreSeed.DecimalContains == false)
                {
                    tmpBalanceStr = tmpBalanceStr + Notus.Toolbox.Text.RepeatString(Obj_Settings.Genesis.Reserve.Decimal, "0");
                }
                StoreToDb(new Notus.Variable.Struct.WalletBalanceStruct()
                {
                    UID = tmpBlockForBalance.info.uID,
                    RowNo = tmpBlockForBalance.info.rowNo,
                    Wallet = Obj_Settings.Genesis.Premining.PreSeed.Wallet,
                    Balance = new Dictionary<string, Dictionary<ulong, string>>()
                    {
                        {
                            Obj_Settings.Genesis.CoinInfo.Tag,
                            new Dictionary<ulong, string>()
                            {
                                {
                                    coinStartingTime, tmpBalanceStr
                                }
                            }
                        }
                    }
                });


                tmpBalanceStr = Obj_Settings.Genesis.Premining.Private.Volume.ToString();
                if (Obj_Settings.Genesis.Premining.Private.DecimalContains == false)
                {
                    tmpBalanceStr = tmpBalanceStr + Notus.Toolbox.Text.RepeatString(Obj_Settings.Genesis.Reserve.Decimal, "0");
                }
                StoreToDb(new Notus.Variable.Struct.WalletBalanceStruct()
                {
                    UID = tmpBlockForBalance.info.uID,
                    RowNo = tmpBlockForBalance.info.rowNo,
                    Wallet = Obj_Settings.Genesis.Premining.Private.Wallet,
                    Balance = new Dictionary<string, Dictionary<ulong, string>>()
                    {
                        {
                            Obj_Settings.Genesis.CoinInfo.Tag,
                            new Dictionary<ulong, string>()
                            {
                                {
                                    coinStartingTime, tmpBalanceStr
                                }
                            }
                        }
                    }
                });


                tmpBalanceStr = Obj_Settings.Genesis.Premining.Public.Volume.ToString();
                if (Obj_Settings.Genesis.Premining.Public.DecimalContains == false)
                {
                    tmpBalanceStr = tmpBalanceStr + Notus.Toolbox.Text.RepeatString(Obj_Settings.Genesis.Reserve.Decimal, "0");
                }
                StoreToDb(new Notus.Variable.Struct.WalletBalanceStruct()
                {
                    UID = tmpBlockForBalance.info.uID,
                    RowNo = tmpBlockForBalance.info.rowNo,
                    Wallet = Obj_Settings.Genesis.Premining.Public.Wallet,
                    Balance = new Dictionary<string, Dictionary<ulong, string>>()
                    {
                        {
                            Obj_Settings.Genesis.CoinInfo.Tag,
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

            Notus.Print.Basic(Obj_Settings, "Balance.Cs -> Control function -> Line 178 -> Block type -> " + tmpBlockForBalance.info.type.ToString());
            Notus.Print.Basic(Obj_Settings, "Balance.Cs -> " + tmpBlockForBalance.info.rowNo.ToString());

            if (tmpBlockForBalance.info.type == 120)
            {
                string tmpRawDataStr = System.Text.Encoding.UTF8.GetString(
                    System.Convert.FromBase64String(
                        tmpBlockForBalance.cipher.data
                    )
                );
                Notus.Variable.Class.BlockStruct_120 tmpBalanceVal = JsonSerializer.Deserialize<Notus.Variable.Class.BlockStruct_120>(tmpRawDataStr);

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

            if (tmpBlockForBalance.info.type == 160)
            {
                Notus.Variable.Struct.BlockStruct_160 tmpBalanceVal = JsonSerializer.Deserialize<Notus.Variable.Struct.BlockStruct_160>(
                    System.Text.Encoding.UTF8.GetString(
                        System.Convert.FromBase64String(
                            tmpBlockForBalance.cipher.data
                        )
                    )
                );
                Int64 BlockFee = Notus.Wallet.Fee.Calculate(tmpBalanceVal, Obj_Settings.Network);
                string WalletKeyStr = Notus.Wallet.ID.GetAddressWithPublicKey(tmpBalanceVal.Creation.PublicKey, Obj_Settings.Network);
                Notus.Variable.Struct.WalletBalanceStruct CurrentBalance = Get(WalletKeyStr, 0);
                string TokenBalanceStr = tmpBalanceVal.Reserve.Supply.ToString();


                ulong tmpBlockTime = ulong.Parse(tmpBlockForBalance.info.time.Substring(0,17));
                CurrentBalance.Balance.Add(tmpBalanceVal.Info.Tag, new Dictionary<ulong, string>()
                    {
                        { tmpBlockTime, TokenBalanceStr }
                    }
                );

                (bool tmpErrorStatus, var newBalanceVal) =
                    SubtractVolumeWithUnlockTime(CurrentBalance, BlockFee.ToString(), Obj_Settings.Genesis.CoinInfo.Tag, tmpBlockTime);
                /*
                CurrentBalance.Balance[Obj_Settings.Genesis.CoinInfo.Tag] =
                    (BigInteger.Parse(CurrentBalance.Balance[Obj_Settings.Genesis.CoinInfo.Tag]) -
                    BlockFee).ToString();
                */

                StoreToDb(new Notus.Variable.Struct.WalletBalanceStruct()
                {
                    Balance = newBalanceVal.Balance,
                    RowNo = tmpBlockForBalance.info.rowNo,
                    UID = tmpBlockForBalance.info.uID,
                    Wallet = Notus.Wallet.ID.GetAddressWithPublicKey(tmpBalanceVal.Creation.PublicKey, Obj_Settings.Network)
                }
                );

                Notus.Wallet.Currency.Add2List(Obj_Settings.Network, Obj_Settings.Layer, new Notus.Variable.Struct.CurrencyListStorageStruct()
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
        public void Start()
        {
            ObjMp_Balance = new Notus.Mempool(
                Notus.IO.GetFolderName(Obj_Settings.Network, Obj_Settings.Layer, Notus.Variable.Constant.StorageFolderName.Balance) +
                "account_balance"
            );
            ObjMp_Balance.AsyncActive = false;
            ObjMp_Balance.Clear();
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
            try
            {
                if (ObjMp_Balance != null)
                {
                    ObjMp_Balance.Dispose();
                }
            }
            catch { }
        }
    }
}
