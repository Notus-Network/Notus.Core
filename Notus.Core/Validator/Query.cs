using System;
using System.Collections.Generic;
using System.Text.Json;
namespace Notus.Validator
{
    public class Query
    {
        public static (bool, Notus.Variable.Class.BlockData) GetBlock(string nodeAdress, Int64 BlockRowNo)
        {
            //string mainAddressStr = Notus.Core.Function.MakeHttpListenerPath(nodeAdress, Notus.Variable.Struct.PortNo_HttpListener);
            try
            {
                string MainResultStr = Notus.Communication.Request.Get(nodeAdress + "block/" + BlockRowNo.ToString(), 10, true).GetAwaiter().GetResult();
                Notus.Variable.Class.BlockData PreBlockData = JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(MainResultStr);
                return (true, PreBlockData);
            }
            catch
            {

            }
            return (false, null);
        }
        public static (bool, Notus.Variable.Class.BlockData) GetLastBlock(string NodeAddress)
        {
            try
            {
                string MainResultStr = Notus.Communication.Request.Get(NodeAddress + "block/last", 10, true).GetAwaiter().GetResult();
                Notus.Variable.Class.BlockData PreBlockData = JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(MainResultStr);
                return (true, PreBlockData);
            }
            catch
            {

            }
            return (false, null);
        }
        public static (bool, Notus.Variable.Struct.LastBlockInfo) GetLastBlockInfo(string NodeAddress)
        {
            try
            {
                //string mainAddressStr = Notus.Core.Function.MakeHttpListenerPath(NodeAddress, Notus.Variable.Struct.PortNo_HttpListener);
                string MainResultStr = Notus.Communication.Request.Get(NodeAddress + "block/summary", 10, true).GetAwaiter().GetResult();
                Notus.Variable.Struct.LastBlockInfo PreBlockData = JsonSerializer.Deserialize<Notus.Variable.Struct.LastBlockInfo>(MainResultStr);
                return (true, PreBlockData);
            }
            catch
            {

            }
            return (false, null);
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
                        errorCount++;
                        Notus.Debug.Print.Basic(DebugModeActive, "Error Text [a9b467ce] : " + err.Message);
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
