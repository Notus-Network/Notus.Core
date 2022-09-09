using System;
using System.Collections.Generic;
using System.Text.Json;
namespace Notus.Validator
{
    public class Query
    {
        public static (bool, Notus.Variable.Class.BlockData?) GetBlock(
            string nodeAdress, 
            Int64 BlockRowNo,
            bool showOnError=true,
            Notus.Variable.Common.ClassSetting? objSettings = null
        )
        {
            try
            {
                string MainResultStr = Notus.Communication.Request.GetSync(
                    nodeAdress + "block/" + BlockRowNo.ToString(), 
                    10, 
                    true,
                    showOnError,
                    objSettings
                );
                Notus.Variable.Class.BlockData? PreBlockData = JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(MainResultStr);
                return (true, PreBlockData);
            }
            catch(Exception err)
            {
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Info,
                    3020101,
                    err.Message,
                    "BlockRowNo",
                    objSettings,
                    err
                );
            }
            return (false, null);
        }
        public static Notus.Variable.Struct.LastBlockInfo? GetLastBlockInfo(string NodeAddress, Notus.Variable.Common.ClassSetting? Obj_Settings=null)
        {
            try
            {
                //string mainAddressStr = Notus.Core.Function.MakeHttpListenerPath(NodeAddress, Notus.Variable.Struct.PortNo_HttpListener);
                string MainResultStr = Notus.Communication.Request.GetSync(
                    NodeAddress + "block/summary", 
                    10, 
                    true,
                    true,
                    Obj_Settings
                );
                Notus.Variable.Struct.LastBlockInfo PreBlockData = JsonSerializer.Deserialize<Notus.Variable.Struct.LastBlockInfo>(MainResultStr);
                return PreBlockData;
            }
            catch(Exception err)
            {
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Info,
                    809050,
                    err.Message,
                    "BlockRowNo",
                    Obj_Settings,
                    err
                );
            }
            return null;
        }
        public static (bool, Dictionary<string, Notus.Variable.Class.BlockData>) LastBlockList(
            Notus.Variable.Enum.NetworkNodeType nodeType, 
            Notus.Variable.Enum.NetworkType currentNetwork, 
            Notus.Variable.Enum.NetworkLayer currentLayer, 
            bool DebugModeActive = false
        )
        {
            Dictionary<string, Notus.Variable.Class.BlockData> ResultList = new Dictionary<string, Notus.Variable.Class.BlockData>();
            if (nodeType == Notus.Variable.Enum.NetworkNodeType.Master)
            {
                int nodeCount = 0;
                int errorCount= 0;
                foreach (string nodeIpAddress in Notus.Variable.Constant.ListMainNodeIp)
                {
                    nodeCount++;
                    string mainAddressStr = Notus.Network.Node.MakeHttpListenerPath(nodeIpAddress, Notus.Network.Node.GetNetworkPort(currentNetwork, currentLayer));
                    string MainResultStr = Notus.Communication.Request.Get(mainAddressStr + "block/last", 10, true).GetAwaiter().GetResult();

                    //boş değer dönüyor,
                    //eğer boş değer dönüyorsa işlemde sorun var demektir, kontrol et
                    try
                    {
                        Notus.Variable.Class.BlockData PreBlockData = JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(MainResultStr);
                        ResultList.Add(mainAddressStr, PreBlockData);
                    }
                    catch (Exception err)
                    {
                        Notus.Print.Log(
                            Notus.Variable.Enum.LogLevel.Info,
                            951230,
                            err.Message,
                            "BlockRowNo",
                            null,
                            err
                        );

                        errorCount++;
                        Notus.Print.Basic(DebugModeActive, "Error Text [a9b467ce] : " + err.Message);
                    }
                }
                if (nodeCount > errorCount)
                {
                    return (true, ResultList);
                }
            }

            return (false, ResultList);
        }
    }
}
