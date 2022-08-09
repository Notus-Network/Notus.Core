using System.Collections.Generic;
using System.Text.Json;
namespace Notus
{
    public class Sync
    {
        public static bool Block(
            Notus.Variable.Common.ClassSetting objSettings, 
            List<Notus.Variable.Struct.IpInfo> nodeList,
            System.Action<Notus.Variable.Class.BlockData>? Func_NewBlockIncome = null
        )
        {
            long smallestBlockRow = long.MaxValue;
            bool weFindOtherNode = false;
            foreach (Variable.Struct.IpInfo? tmpEntry in nodeList)
            {
                if (tmpEntry != null)
                {
                    Notus.Variable.Class.BlockData? nodeLastBlock = Notus.Toolbox.Network.GetLastBlock(tmpEntry);
                    if (nodeLastBlock != null)
                    {
                        weFindOtherNode = true;
                        if (smallestBlockRow > nodeLastBlock.info.rowNo)
                        {
                            smallestBlockRow = nodeLastBlock.info.rowNo;
                        }
                    }
                }
            }
            if (weFindOtherNode==true)
            {
                //Console.WriteLine("weFindOtherNode");
            }

            //Console.WriteLine("smallestBlockRow : " + smallestBlockRow.ToString());
            //Console.WriteLine(objSettings.LastBlock.info.uID);
            //Console.WriteLine(objSettings.LastBlock.info.type);
            //Console.WriteLine(objSettings.LastBlock.info.rowNo);
            if (objSettings.LastBlock.info.rowNo> smallestBlockRow)
            {
                //Console.WriteLine("My Node Higher Than Other");
                //Console.ReadLine();
            }
            else
            {
                //Console.WriteLine("My Node Smaller Than Other");
            }
            bool exitForLoop = false;
            int nCount = 0;
            List<bool> nodeControlList = new List<bool>();
            for(int i = 0; i < nodeList.Count; i++)
            {
                nodeControlList.Add(false);
            }
            for (long blockNo = objSettings.LastBlock.info.rowNo; blockNo < (smallestBlockRow +1) && exitForLoop == false; blockNo++)
            {
                /*
                kontrol edilmemiş olanlar false olarak işaretlenecek
                */
                for (int i = 0; i < nodeList.Count; i++)
                {
                    nodeControlList[i]=false;
                }
                // burada belirtilen sayıda node'u kontrol ederek blok bulacak
                // aşağıdaki verilen 8 sayısı en fazla kontrol edilecek node sayısı
                for (int iCount=0; iCount<8; iCount++)
                {
                    if (nodeControlList[nCount] == false)
                    {
                        nodeControlList[nCount] = true;
                        Notus.Variable.Struct.IpInfo? currentNode = nodeList[nCount];
                        if (currentNode != null)
                        {
                            Notus.Variable.Class.BlockData? nodeLastBlock =
                                Notus.Toolbox.Network.GetBlockFromNode(
                                    currentNode,
                                    blockNo,
                                    objSettings
                                );
                            if (nodeLastBlock != null)
                            {
                                if (Func_NewBlockIncome != null)
                                {
                                    Func_NewBlockIncome(nodeLastBlock);
                                }
                            }
                        }
                    }
                    nCount++;
                    if (nodeList.Count == nCount)
                    {
                        nCount = 0;
                    }
                }
            }
            return true;
            //Console.ReadLine();
            //önce son blokları çek
            //önce son blokları çek
            /*
            Dictionary<string, Notus.Variable.Class.BlockData> signBlock = new Dictionary<string, Notus.Variable.Class.BlockData>();
            signBlock.Clear();

            Dictionary<string, int> signCount = new Dictionary<string, int>();
            signCount.Clear();

            foreach (Variable.Struct.IpInfo item in Notus.Validator.List.Main[objSettings.Layer][objSettings.Network])
            {
                if (string.Equals(objSettings.IpInfo.Public, item.IpAddress) == false)
                {
                    Notus.Variable.Class.BlockData? tmpInnerBlockData =
                    Notus.Toolbox.Network.GetBlockFromNode(item.IpAddress, item.Port, 1, objSettings);
                    if (tmpInnerBlockData != null)
                    {
                        if (signCount.ContainsKey(tmpInnerBlockData.sign) == false)
                        {
                            signCount.Add(tmpInnerBlockData.sign, 0);
                            signBlock.Add(tmpInnerBlockData.sign, tmpInnerBlockData);
                        }
                        signCount[tmpInnerBlockData.sign] = signCount[tmpInnerBlockData.sign] + 1;
                    }
                    else
                    {
                        Notus.Print.Danger(objSettings, "Error Happened While Trying To Get Genesis From Other Node");
                        Notus.Date.SleepWithoutBlocking(100);
                    }
                }
            }

            if (signCount.Count == 0)
            {
                return false;
            }
            int tmpBiggestCount = 0;
            string tmpBiggestSign = string.Empty;
            foreach (KeyValuePair<string, int> entry in signCount)
            {
                if (entry.Value > tmpBiggestCount)
                {
                    tmpBiggestCount = entry.Value;
                    tmpBiggestSign = entry.Key;
                }
            }
            if (string.Equals(tmpBiggestSign, myGenesisSign) == false)
            {
                DateTime otherNodeGenesisTime = Notus.Date.GetGenesisCreationTimeFromString(signBlock[tmpBiggestSign]);
                Int64 otherNodeGenesisTimeVal = Int64.Parse(
                    otherNodeGenesisTime.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText)
                );
                Int64 myGenesisTimeVal = Int64.Parse(
                    myGenesisTime.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText)
                );
                if (myGenesisTimeVal > otherNodeGenesisTimeVal)
                {
                    using (Notus.Block.Storage BS_Storage = new Notus.Block.Storage(false))
                    {
                        BS_Storage.Network = objSettings.Network;
                        BS_Storage.Layer = objSettings.Layer;
                        Notus.Print.Basic(objSettings, "Current Block Were Deleted");
                        Notus.Archive.ClearBlocks(objSettings);
                        BS_Storage.AddSync(signBlock[tmpBiggestSign], true);
                        Notus.Print.Basic(objSettings, "Added Block : " + signBlock[tmpBiggestSign].info.uID);
                    }
                    Notus.Date.SleepWithoutBlocking(150);
                }
                else
                {
                    Notus.Print.Basic(objSettings, "Hold Your Genesis Block - We Are Older");
                }
            }
            //Console.WriteLine("Press Enter To Continue");
            //Console.ReadLine();
            return true;
            */
        }


        //burada verilen blok numarasını tüm nodelardan sorgula
        //alınan blok özetlerini kontrol et ve en çok olan özeti kabul et
        private static Notus.Variable.Class.BlockData GetValidBlock
        (
            Notus.Variable.Common.ClassSetting objSettings
        )
        {
            return null;
        }
    }
}
