using System;

namespace Notus.Variable.Genesis
{
    public class GenesisBlockData
    {
        public int Version { get; set; }
        public EmptyBlockType Empty { get; set; }
        public CoinReserveType Reserve { get; set; }
        public CoinInformationType CoinInfo { get; set; }
        public CoinSupplyType Supply { get; set; }
        public FeeType Fee { get; set; }
        //public ContractFeeType Data { get; set; }
        public GenesisInfoType Info { get; set; }
        public PreminingType Premining { get; set; }
    }

    public class EmptyBlockType
    {
        public bool Active { get; set; }
        public ulong LuckyReward { get; set; }           // daily lucky node reward
        public ulong TotalSupply { get; set; }           // total reward supply
        public ulong Reward { get; set; }           // total reward supply
        public IntervalType Interval { get; set; }
        public SlowBlockType SlowBlock { get; set; }
        public EmptyBlockNonceType Nonce { get; set; }
    }

    public class EmptyBlockNonceType
    {
        public int Method { get; set; }
        public int Type { get; set; }
        public int Difficulty { get; set; }
    }
    public class IntervalType
    {
        public int Time { get; set; }       /* saniye cinsinden */
        public int Block { get; set; }      /* block sayısı bu miktara ulaşınca da empty block oluşturulsun */
    }
    public class SlowBlockType
    {
        public bool Active { get; set; }       /* slow blok işlemi geçerli mi */
        public int Count { get; set; }       /* peşpeşe kaç empty blok sonrası işlem yavaşlatılacak */
        public int Multiply { get; set; }    /* peşpeşe empty block sonra interval time kaç ile çarpılacak */
    }
    public class CoinInformationType
    {
        public string Tag { get; set; }       /* coin tag name */
        public string Name { get; set; }      /* coin full name */
        public Notus.Variable.Struct.FileStorageStruct Logo { get; set; }     /* coin logo */
    }
    public class CoinReserveType
    {
        public UInt64 Value { get; set; }       /* eğer kesin tutar belirtilecekse burada yazılacak string olarak, ondalık basamağı nokta ile ayrılacak */
        public UInt64 Total { get; set; }       /* "4" x "12" adet sıfır ve hassasiyet için virgülden sonra "6" basamak */
        public int Digit { get; set; }       /* virgül sonrası sıfır sayısı */
        public int Decimal { get; set; }    /* virgül öncesi basamak sayısı */
    }
    public class CoinSupplyType
    {
        public int Decrease { get; set; }       /* ödülden yapılacak düşüş oranı */
        public int Type { get; set; }           /* 1-empty blok sayısı ile 2-toplam blok sayısı sayılacak  */
        public int Modular { get; set; }        /* kaç blokta bir blok arzı yüzde olarak azalacak */
    }
    public class TokenPriceStructType
    {
        public int Generate { get; set; }       /* token oluşturma işlem ücreti */
        public int Update { get; set; }       /* token oluşturma sonrası güncelleme işlemi ücreti */
    }

    public class CoinTransferFeeType
    {
        public int Fast { get; set; }       /* yüksek öncelik */
        public int Common { get; set; }       /* standart işlem ücreti */
        public int NoName { get; set; }       /* isimsiz gönderim ile */
        public int ByPieces { get; set; }    /* parçalar halinde */
    }
    public class ContractFeeType
    {
        public int Data { get; set; }           /* kaydedilen data için byte fiyatı */
        // public int Comparison { get; set; }         /* standart işlem ücreti */
        // public int NoName { get; set; }         /* isimsiz gönderim ile */
        // public int ByPieces { get; set; }       /* parçalar halinde */
    }
    public class FeeType
    {
        public CoinTransferFeeType Transfer { get; set; }
        public TokenPriceStructType Token { get; set; }
        //public ContractFeeType Contract { get; set; }
        public int Data { get; set; }
    }

    public class GenesisInfoType
    {
        public DateTime Creation { get; set; }       /* oluşturma zaman */
        public string Creator { get; set; }          /* oluşturan kullanıcı cüzdan adresi */
        public string CurveName { get; set; }          /* varsayılan olarak kullanılacak curve adı */
        public bool EncryptKeyPair { get; set; }          /* varsayılan olarak kullanılacak curve adı */
    }

    public class PreminingType
    {
        public SaleOptionGroupType PreSeed { get; set; }       /* oluşturma zaman */
        public SaleOptionGroupType Private { get; set; }       /* oluşturma zaman */
        public SaleOptionGroupType Public { get; set; }       /* oluşturma zaman */
    }


    //UInt64 EnUzunSayi_64_Bit_Icin = 18446744073709551615;
    //                                   100 000 000 000 000 000
    //UInt64 EnUzunSayi_64_Bit_Icin = 18 446 744 073 709 551 615;
    public class SaleOptionGroupType
    {
        public UInt64 Volume { get; set; }
        public bool DecimalContains { get; set; }
        public int HowManyMonthsLater { get; set; }
        public int PercentPerMonth { get; set; }
        public string Wallet { get; set; }
        public string PublicKey { get; set; }
    }
}
