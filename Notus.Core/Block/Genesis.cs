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
