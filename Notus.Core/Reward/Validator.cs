using System.Collections.Generic;
using System.Text.Json;

namespace Notus.Reward
{
    public static class Validator
    {
        private static Notus.Mempool GiveCurrencyListDb(Notus.Variable.Enum.NetworkType networkType,Notus.Variable.Enum.NetworkLayer networkLayer)
        {
            Notus.Mempool ObjMp_Balance = new Notus.Mempool(
                            Notus.IO.GetFolderName(
                                networkType,
                                networkLayer,
                                Notus.Variable.Constant.StorageFolderName.Balance
                            ) + "currency_list"
            );
            ObjMp_Balance.AsyncActive = false;
            return ObjMp_Balance;
        }
        public static List<Notus.Variable.Struct.CurrencyList> GetList(Notus.Variable.Enum.NetworkType networkType,Notus.Variable.Enum.NetworkLayer networkLayer)
        {
            List<Notus.Variable.Struct.CurrencyList> tmpcurrencyList = new List<Notus.Variable.Struct.CurrencyList>();
            Notus.Mempool ObjMp_Balance = GiveCurrencyListDb(networkType, networkLayer);
            ObjMp_Balance.Each((string CurrencyHexName, string currencyDataStr) =>
            {
                //Console.WriteLine("Notus.Wallet.Currency.GetList");
                //Console.WriteLine(currencyDataStr);
                Notus.Variable.Struct.CurrencyListStorageStruct tmpAirDrop = JsonSerializer.Deserialize<Notus.Variable.Struct.CurrencyListStorageStruct>(currencyDataStr);
                tmpcurrencyList.Add(new Notus.Variable.Struct.CurrencyList()
                {
                    ReserveCurrency = tmpAirDrop.Detail.ReserveCurrency,
                    Name = tmpAirDrop.Detail.Name,
                    Tag = tmpAirDrop.Detail.Tag,
                    Logo = tmpAirDrop.Detail.Logo
                });
            }, 0);
            return tmpcurrencyList;
        }
        public static void ClearList(Notus.Variable.Enum.NetworkType networkType,Notus.Variable.Enum.NetworkLayer networkLayer)
        {
            Notus.Mempool mp_CurrencyList = GiveCurrencyListDb(networkType, networkLayer);
            mp_CurrencyList.AsyncActive = false;
            mp_CurrencyList.Clear();
            mp_CurrencyList.Dispose();
        }
        public static string Add2List(Notus.Variable.Enum.NetworkType networkType, Notus.Variable.Enum.NetworkLayer networkLayer, Notus.Variable.Struct.CurrencyListStorageStruct CurrencyData)
        {
            Notus.Mempool ObjMp_Balance = GiveCurrencyListDb(networkType, networkLayer);
            bool exitWhileLoop = false;
            string CurrencyName = CurrencyData.Detail.Tag;
            while (exitWhileLoop == false)
            {

                string tmpCurrencyData = ObjMp_Balance.Get(Notus.Toolbox.Text.CurrencyName2Hex(CurrencyName), "");
                if (tmpCurrencyData.Length == 0)
                {
                    CurrencyData.Detail.Tag = CurrencyName;
                    exitWhileLoop = true;
                    if (
                        ObjMp_Balance.Add(
                            Notus.Toolbox.Text.CurrencyName2Hex(CurrencyName),
                            JsonSerializer.Serialize(CurrencyData)
                        ) == false
                    )
                    {
                        CurrencyName = "";
                    }
                }
                else
                {
                    CurrencyName = CurrencyName + "#2";
                }
            }
            return CurrencyName;
        }

        // this function control currency name have or not
        public static bool Exist(Notus.Variable.Enum.NetworkType networkType, Notus.Variable.Enum.NetworkLayer networkLayer, string CurrencyName)
        {
            Notus.Mempool ObjMp_Balance = GiveCurrencyListDb(networkType, networkLayer);
            string tmpCurrencyData = ObjMp_Balance.Get(Notus.Toolbox.Text.CurrencyName2Hex(CurrencyName), "");
            return (tmpCurrencyData.Length == 0 ? false : true);
        }
    }
}
