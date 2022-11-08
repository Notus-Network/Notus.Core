using System;
using System.Collections.Generic;
using System.Text.Json;
using NP = Notus.Print;
namespace Notus.Wallet
{
    /*
     nuget push C:\wamp64\www\server\nuget\Notus.Wallet\Notus.Wallet\bin\Debug\Notus.Wallet.2.0.13.nupkg oy2poyafosoxjx7q6neakhxqgs34sqhenfegmyx3zc5koy -Source https://api.nuget.org/v3/index.json
    
    */
    public static class Fee
    {
        public static Int64 Calculate(Notus.Variable.Struct.BlockStruct_160 RawObj, Notus.Variable.Enum.NetworkType networkType = Notus.Variable.Enum.NetworkType.MainNet,Notus.Variable.Enum.NetworkLayer networkLayer = Notus.Variable.Enum.NetworkLayer.Layer1)
        {
            return
                ReadFeeData(Notus.Variable.Enum.Fee.TokenGeneration, networkType, networkLayer) +
                (
                    ReadFeeData(Notus.Variable.Enum.Fee.DataStorage, networkType, networkLayer)
                        *
                    (RawObj.Info.Logo.Base64.Length==0 ? 1 : RawObj.Info.Logo.Base64.Length)
                );
        }
        public static Int64 Calculate(
            Notus.Variable.Enum.Fee FeeType, 
            Notus.Variable.Enum.NetworkType networkType, 
            Notus.Variable.Enum.NetworkLayer networkLayer
        )
        {
            return ReadFeeData(FeeType, networkType, networkLayer);
        }

        public static Notus.Variable.Struct.FeeCalculationStruct CalculateFromNode(
            Notus.Variable.Struct.BlockStruct_160 RawObj, Notus.Variable.Enum.NetworkType currentNetwork)
        {
            bool exitInnerLoop = false;
            while (exitInnerLoop == false)
            {
                for (int a = 0; a < Notus.Variable.Constant.ListMainNodeIp.Count && exitInnerLoop == false; a++)
                {
                    string nodeIpAddress = Notus.Variable.Constant.ListMainNodeIp[a];
                    try
                    {
                        string fullUrlAddress =
                            Notus.Network.Node.MakeHttpListenerPath(
                                nodeIpAddress,
                                Notus.Network.Node.GetNetworkPort(currentNetwork, Notus.Variable.Enum.NetworkLayer.Layer1)
                            ) + "calculator/160/";

                        string CalculateResultDataStr = Notus.Communication.Request.Post(
                            fullUrlAddress,
                            new Dictionary<string, string>
                            {
                                { "data" , JsonSerializer.Serialize(RawObj) }
                            }
                        ).GetAwaiter().GetResult();

                        Notus.Variable.Struct.FeeCalculationStruct tmpTransferResult = JsonSerializer.Deserialize<Notus.Variable.Struct.FeeCalculationStruct>(CalculateResultDataStr);
                        return tmpTransferResult;
                    }
                    catch (Exception err)
                    {
                        NP.Basic(true, "Error Text [a32d5gjhnr87]: " + err.Message);
                        return new Notus.Variable.Struct.FeeCalculationStruct()
                        {
                            Fee = 0,
                            Error = true
                        };
                    }
                }
            }
            return new Notus.Variable.Struct.FeeCalculationStruct()
            {
                Fee = 0,
                Error = true
            };
        }

        private static string FeeDataStorageDbName(Notus.Variable.Enum.NetworkType networkType,Notus.Variable.Enum.NetworkLayer networkLayer)
        {
            string tmpFolderName = Notus.IO.GetFolderName(
                networkType,
                networkLayer,
                Notus.Variable.Constant.StorageFolderName.Common
            );
            //Console.WriteLine(tmpFolderName);
            //Console.WriteLine(tmpFolderName);
            return tmpFolderName + "price_data";
        }
        public static Int64 ReadFeeData(
            Notus.Variable.Enum.Fee FeeConstant, 
            Notus.Variable.Enum.NetworkType networkType,
            Notus.Variable.Enum.NetworkLayer networkLayer
        )
        {
            Notus.Variable.Genesis.GenesisBlockData? Obj_Genesis = null;

            using (
                Notus.Mempool ObjMp_BlockOrder = new Notus.Mempool(
                    FeeDataStorageDbName(
                        networkType, networkLayer
                    )
                )
            )
            {
                string tmpReturnVal = ObjMp_BlockOrder.Get("genesis_block", "");
                if (tmpReturnVal.Length > 0)
                {
                    Obj_Genesis = JsonSerializer.Deserialize<Notus.Variable.Genesis.GenesisBlockData>(tmpReturnVal);
                }
            }
            if (Obj_Genesis == null)
            {
                return 1000000;
            }

            if (FeeConstant == Notus.Variable.Enum.Fee.CryptoTransfer_MultiSign)
            {
                return Obj_Genesis.Fee.Transfer.Common * 2;
            }

            if (FeeConstant == Notus.Variable.Enum.Fee.CryptoTransfer)
            {
                return Obj_Genesis.Fee.Transfer.Common;
            }
            if (FeeConstant == Notus.Variable.Enum.Fee.CryptoTransfer_Fast)
            {
                return Obj_Genesis.Fee.Transfer.Fast;
            }
            if (FeeConstant == Notus.Variable.Enum.Fee.CryptoTransfer_NoName)
            {
                return Obj_Genesis.Fee.Transfer.NoName;
            }
            if (FeeConstant == Notus.Variable.Enum.Fee.CryptoTransfer_ByPieces)
            {
                return Obj_Genesis.Fee.Transfer.ByPieces;
            }
            if (FeeConstant == Notus.Variable.Enum.Fee.TokenGeneration)
            {
                return Obj_Genesis.Fee.Token.Generate;
            }
            if (FeeConstant == Notus.Variable.Enum.Fee.TokenUpdate)
            {
                return Obj_Genesis.Fee.Token.Update;
            }
            if (FeeConstant == Notus.Variable.Enum.Fee.DataStorage)
            {
                return Obj_Genesis.Fee.Data;
            }
            return 0;
        }
        public static void ClearFeeData(Notus.Variable.Enum.NetworkType networkType , Notus.Variable.Enum.NetworkLayer networkLayer)
        {
            try
            {
                using (Notus.Mempool ObjMp_FeeData = new Notus.Mempool(FeeDataStorageDbName(networkType, networkLayer)))
                {
                    ObjMp_FeeData.AsyncActive = false;
                    ObjMp_FeeData.Clear();
                }
            }
            catch(Exception err)
            {
                Console.WriteLine("Fee.Cs -> Line 172");
                Console.WriteLine(err.Message);
                Console.WriteLine(err.Message);
            }
        }
        public static void StoreFeeData(string KeyName, string RawData, Notus.Variable.Enum.NetworkType networkType , Notus.Variable.Enum.NetworkLayer networkLayer , bool ClearTable = false)
        {
            if (ClearTable == true)
            {
                ClearFeeData(networkType, networkLayer);
            }
            using (Notus.Mempool ObjMp_BlockOrder = new Notus.Mempool(FeeDataStorageDbName(networkType, networkLayer)))
            {
                ObjMp_BlockOrder.AsyncActive = false;
                if (KeyName.Length > 0)
                {
                    ObjMp_BlockOrder.Add(KeyName, RawData);
                }
            }
        }
    }
}
