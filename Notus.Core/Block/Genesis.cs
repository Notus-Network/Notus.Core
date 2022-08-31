using Notus.Variable.Genesis;
using System;
using System.Text.Json;

namespace Notus.Block
{
    public class Genesis
    {
        private const string SelectedCurveName = Notus.Variable.Constant.Default_EccCurveName;
        private const bool Val_DefaultEncryptKeyPair = false;
        private const Notus.Variable.Enum.NetworkType Val_DefaultNetworkType = Notus.Variable.Enum.NetworkType.MainNet;
        private const Notus.Variable.Enum.NetworkLayer Val_DefaultNetworkLayer = Notus.Variable.Enum.NetworkLayer.Layer1;

        public static GenesisBlockData Generate(string CreatorWalletKey, Notus.Variable.Enum.NetworkType NetworkType, Notus.Variable.Enum.NetworkLayer NetworkLayer)
        {
            return GetGenesis_SubFunction(CreatorWalletKey, Val_DefaultEncryptKeyPair, NetworkType, NetworkLayer);
        }
        public static GenesisBlockData GetGenesis(string CreatorWalletKey, bool EncryptKeyPair)
        {
            return GetGenesis_SubFunction(CreatorWalletKey, EncryptKeyPair, Val_DefaultNetworkType, Val_DefaultNetworkLayer);
        }
        public static GenesisBlockData GetGenesis(string CreatorWalletKey, Notus.Variable.Enum.NetworkType NetworkType, Notus.Variable.Enum.NetworkLayer NetworkLayer)
        {
            return GetGenesis_SubFunction(CreatorWalletKey, Val_DefaultEncryptKeyPair, NetworkType, NetworkLayer);
        }

        public static GenesisBlockData GetGenesis_SubFunction(string CreatorWalletKey, bool EncryptKeyPair, Notus.Variable.Enum.NetworkType NetworkType, Notus.Variable.Enum.NetworkLayer NetworkLayer)
        {
            DateTime generationTime = Notus.Time.GetFromNtpServer();
            string EncKey = generationTime.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText);
            Notus.Variable.Struct.EccKeyPair KeyPair_PreSeed = Notus.Wallet.ID.GenerateKeyPair(SelectedCurveName, NetworkType);
            Notus.Variable.Struct.EccKeyPair KeyPair_Private = Notus.Wallet.ID.GenerateKeyPair(SelectedCurveName, NetworkType);
            Notus.Variable.Struct.EccKeyPair KeyPair_Public = Notus.Wallet.ID.GenerateKeyPair(SelectedCurveName, NetworkType);



            using (Notus.Mempool ObjMp_Genesis =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(NetworkType, NetworkLayer, Notus.Variable.Constant.StorageFolderName.Common) + "genesis_accounts"
                )
            )
            {
                ObjMp_Genesis.AsyncActive = false;
                ObjMp_Genesis.Clear();
                if (EncryptKeyPair == true)
                {
                    using (Notus.Encryption.Cipher Obj_Cipher = new Notus.Encryption.Cipher())
                    {
                        ObjMp_Genesis.Set("seed_key",
                            Obj_Cipher.Encrypt(
                                JsonSerializer.Serialize(KeyPair_PreSeed), "", EncKey, EncKey
                            ),
                            true
                        );
                    }

                    using (Notus.Encryption.Cipher Obj_Cipher = new Notus.Encryption.Cipher())
                    {
                        ObjMp_Genesis.Set("private_key",
                            Obj_Cipher.Encrypt(
                                JsonSerializer.Serialize(KeyPair_Private), "", EncKey, EncKey
                            ),
                            true
                        );
                    }

                    using (Notus.Encryption.Cipher Obj_Cipher = new Notus.Encryption.Cipher())
                    {
                        ObjMp_Genesis.Set("public_key",
                            Obj_Cipher.Encrypt(
                                JsonSerializer.Serialize(KeyPair_Public), "", EncKey, EncKey
                            ),
                            true
                        );
                    }
                }
                else
                {
                    ObjMp_Genesis.Set("seed_key", JsonSerializer.Serialize(KeyPair_PreSeed), true);
                    ObjMp_Genesis.Set("private_key", JsonSerializer.Serialize(KeyPair_Private), true);
                    ObjMp_Genesis.Set("public_key", JsonSerializer.Serialize(KeyPair_Public), true);
                }
            }

            return new GenesisBlockData()
            {
                Version = 10000,
                Empty = new EmptyBlockType()
                {
                    TotalSupply = 550000000,
                    LuckyReward = 50,
                    Reward =2,
                    Active = true,
                    Interval = new IntervalType()
                    {
                        Time = 90,
                        Block = 10
                    },
                    SlowBlock = new SlowBlockType() /* peşpeşe empty block sonrası yavaşlatma süresi */
                    {
                        Active = true,
                        Count = 10,             // kaç adet empty blok sayılacak
                        Multiply = 10           // eğer sayı "Count" sayısı kadar empty blok mevcut ise, "Time" değişkeni kaç ile çarpılacak
                    },
                    Nonce = new EmptyBlockNonceType()
                    {
                        Type = 1,           // 1- kayar hesaplama, 2- atlamalı hesaplama
                        Method = 10,        // kullanılacak hash methodu
                        Difficulty = 1      // zorluk değeri
                    }
                },
                Reserve = new CoinReserveType()
                {
                    // Total + Digit * "0" , Decimal * "0"
                    // 100.000.000 , 000 000 000
                    // 100000000000000000

                    // 100 milyon
                    Value = 100000000000000000,     // Exact coin reserve
                    Total = 0,                      // Coin Reserve starts with this number
                    Digit = 0,                      // Add zero end of the "Total" number
                    Decimal = 9                     // Decimal zero count
                    /*
                    Value = 0,      // Exact coin reserve
                    Total = 1,      // Coin Reserve starts with this number
                    Digit = 8,      // Add zero end of the "Total" number
                    Decimal = 9     // Decimal zero count
                    */
                },
                CoinInfo = new CoinInformationType()
                {
                    Tag = "NOTUS",
                    Name = "Notus Coin",
                    Logo = new Notus.Variable.Struct.FileStorageStruct()
                    {
                        Used = true,
                        Base64 = "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",
                        Source = "",
                        Url = ""
                    },
                },
                Supply = new CoinSupplyType()
                {
                    Decrease = 3,
                    Type = 1,
                    Modular = 4000
                },
                Fee = new FeeType()
                {
                    Data = 1500,
                    Token = new TokenPriceStructType()
                    {
                        Generate = 500000,
                        Update = 900000
                    },
                    Transfer = new CoinTransferFeeType()
                    {
                        Fast = 400,         // öncelik verilen işlem
                        Common = 150,       // standart transfer işlemi
                        NoName = 1000,      // önce merkezi bir hesaba ardından kişiye gönderilen 
                        ByPieces = 4000     // önce merkezi bir hesaba ardından paralı halde kişiye gönderilen 
                    },
                    BlockAccount = 1500     // standart işlem ücretinin 10 katı ile hesap bloke edilebilir.
                },
                Info = new GenesisInfoType()
                {
                    Creation = generationTime,
                    Creator = CreatorWalletKey,
                    CurveName = SelectedCurveName,
                    EncryptKeyPair = Val_DefaultEncryptKeyPair
                },
                Premining = new PreminingType()
                {
                    PreSeed = new SaleOptionGroupType()
                    {
                        Volume = 1000000,        //1 milyon
                        DecimalContains = false,
                        HowManyMonthsLater = 24,
                        PercentPerMonth = 5,
                        Wallet = KeyPair_PreSeed.WalletKey,
                        PublicKey = KeyPair_PreSeed.PublicKey
                    },
                    Private = new SaleOptionGroupType()
                    {
                        Volume = 2000000,        //2 milyon
                        DecimalContains = false,
                        HowManyMonthsLater = 18,
                        PercentPerMonth = 5,
                        Wallet = KeyPair_Private.WalletKey,
                        PublicKey = KeyPair_Private.PublicKey
                    },
                    Public = new SaleOptionGroupType()
                    {
                        Volume = 10000000,        //10 milyon
                        DecimalContains = false,
                        HowManyMonthsLater = 12,
                        PercentPerMonth = 5,
                        Wallet = KeyPair_Public.WalletKey,
                        PublicKey = KeyPair_Public.PublicKey
                    }
                }
            };
        }
    }
}
