using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading;
using NVG = Notus.Variable.Globals;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using ND = Notus.Date;
namespace Notus.Block
{
    public class Integrity : IDisposable
    {
        public Notus.Variable.Class.BlockData? GetSatus(bool ResetBlocksIfNonValid = false)
        {
            Notus.Variable.Enum.BlockIntegrityStatus Val_Status = Notus.Variable.Enum.BlockIntegrityStatus.CheckAgain;
            Notus.Variable.Class.BlockData LastBlock = new Notus.Variable.Class.BlockData();
            bool exitInnerLoop = false;
            while (exitInnerLoop == false)
            {
                (Notus.Variable.Enum.BlockIntegrityStatus tmpStatus, Notus.Variable.Class.BlockData tmpLastBlock) = ControlBlockIntegrity();
                if (tmpStatus != Notus.Variable.Enum.BlockIntegrityStatus.CheckAgain)
                {
                    Val_Status = tmpStatus;
                    LastBlock = tmpLastBlock;
                    exitInnerLoop = true;
                }
            }

            if (Val_Status == Notus.Variable.Enum.BlockIntegrityStatus.Valid)
            {
                return LastBlock;
            }
            if (ResetBlocksIfNonValid == true)
            {
                string[] ZipFileList = Notus.IO.GetZipFiles(NVG.Settings);
                foreach (string fileName in ZipFileList)
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch (Exception err)
                    {
                        NP.Danger(NVG.Settings, "Error Text [7abc63]: " + err.Message);
                    }
                }
            }
            return null;
        }
        private (Notus.Variable.Enum.BlockIntegrityStatus, Notus.Variable.Class.BlockData?) ControlBlockIntegrity()
        {
            try
            {
                Notus.Wallet.Fee.ClearFeeData(NVG.Settings.Network, NVG.Settings.Layer);

            }catch(Exception err)
            {
                Console.WriteLine("Integrity.Cs -> Line 68");
                Console.WriteLine(err.Message);
                Console.WriteLine(err.Message);
            }

            Notus.Variable.Class.BlockData LastBlock = Notus.Variable.Class.Block.GetEmpty();
            string[] ZipFileList = Notus.IO.GetZipFiles(NVG.Settings);

            if (ZipFileList.Length == 0)
            {
                NP.Success(NVG.Settings, "Genesis Block Needs");
                //NGF.BlockOrder.Clear();
                return (Notus.Variable.Enum.BlockIntegrityStatus.GenesisNeed, null);
            }
            bool tmpGetListAgain = false;
            foreach (string fileName in ZipFileList)
            {
                int fileCountInZip = 0;
                using (ZipArchive archive = ZipFile.OpenRead(fileName))
                {
                    fileCountInZip = archive.Entries.Count;
                }
                if (fileCountInZip == 0)
                {
                    tmpGetListAgain = true;
                    Thread.Sleep(1);
                    File.Delete(fileName);
                }
            }
            if (tmpGetListAgain == true)
            {
                return (Notus.Variable.Enum.BlockIntegrityStatus.CheckAgain, null);
            }

            bool multiBlockFound = false;
            foreach (string fileName in ZipFileList)
            {
                List<string> deleteInnerFileList = new List<string>();
                using (ZipArchive archive = ZipFile.OpenRead(fileName))
                {
                    List<string> fileNameList = new List<string>();
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (fileNameList.IndexOf(entry.FullName) == -1)
                        {
                            fileNameList.Add(entry.FullName);
                        }
                        else
                        {
                            deleteInnerFileList.Add(entry.FullName);
                        }
                    }
                }
                if (deleteInnerFileList.Count > 0)
                {
                    Notus.Archive.DeleteFromInside(fileName, deleteInnerFileList, true);
                    multiBlockFound = true;
                }
            }
            if(multiBlockFound == true)
            {
                return (Notus.Variable.Enum.BlockIntegrityStatus.CheckAgain, null);
            }

            SortedDictionary<long, string> BlockOrderList = new SortedDictionary<long, string>();
            Dictionary<string, int> BlockTypeList = new Dictionary<string, int>();
            Dictionary<string, string> BlockPreviousList = new Dictionary<string, string>();
            Dictionary<string, bool> ZipArchiveList = new Dictionary<string, bool>();
            Dictionary<string, Notus.Variable.Class.BlockData> Control_RealBlockList = new Dictionary<string, Notus.Variable.Class.BlockData>();
            long BiggestBlockHeight = 0;
            long SmallestBlockHeight = long.MaxValue;

            foreach (string fileName in ZipFileList)
            {
                List<Int64> tmpUpdateBlockRowList = new List<Int64>();
                List<string> tmpDeleteFileList = new List<string>();
                bool returnForCheckAgain = false;
                using (ZipArchive archive = ZipFile.OpenRead(fileName))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) == false)
                        {
                            tmpDeleteFileList.Add(entry.FullName);
                        }
                        else
                        {
                            ZipArchiveEntry? zipEntry = archive.GetEntry(entry.FullName);
                            if (zipEntry != null)
                            {
                                System.IO.FileInfo fif = new System.IO.FileInfo(entry.FullName);
                                using (StreamReader zipEntryStream = new StreamReader(zipEntry.Open()))
                                {
                                    try
                                    {
                                        Notus.Variable.Class.BlockData? ControlBlock = JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(zipEntryStream.ReadToEnd());
                                        if (ControlBlock != null)
                                        {
                                            Notus.Block.Generate BlockValidateObj = new Notus.Block.Generate();
                                            bool Val_BlockVerify = BlockValidateObj.Verify(ControlBlock);
                                            if (Val_BlockVerify == false)
                                            {
                                                NP.Danger(NVG.Settings, "Block Integrity = NonValid");
                                                tmpDeleteFileList.Add(entry.FullName);
                                            }
                                            else
                                            {
                                                if (BlockOrderList.ContainsKey(ControlBlock.info.rowNo))
                                                {
                                                    NP.Danger(NVG.Settings, "Block Integrity = MultipleHeight -> " + ControlBlock.info.rowNo.ToString());
                                                    tmpDeleteFileList.Add(entry.FullName);
                                                    returnForCheckAgain = true;
                                                }
                                                else
                                                {
                                                    if (BlockPreviousList.ContainsKey(ControlBlock.info.uID))
                                                    {
                                                        NP.Danger(NVG.Settings, "Block Integrity = MultipleId -> " + ControlBlock.info.uID);
                                                        tmpDeleteFileList.Add(entry.FullName);
                                                        returnForCheckAgain = true;
                                                    }
                                                    else
                                                    {
                                                        if (SmallestBlockHeight > ControlBlock.info.rowNo)
                                                        {
                                                            SmallestBlockHeight = ControlBlock.info.rowNo;
                                                        }
                                                        if (ControlBlock.info.rowNo > BiggestBlockHeight)
                                                        {
                                                            BiggestBlockHeight = ControlBlock.info.rowNo;
                                                            LastBlock = ControlBlock;
                                                        }
                                                        if (ControlBlock.info.rowNo == 1)
                                                        {
                                                            NVG.Settings.Genesis = JsonSerializer.Deserialize<Notus.Variable.Genesis.GenesisBlockData>(
                                                                System.Convert.FromBase64String(
                                                                    ControlBlock.cipher.data
                                                                )
                                                            );
                                                            Notus.Wallet.Fee.StoreFeeData("genesis_block", JsonSerializer.Serialize(NVG.Settings.Genesis), NVG.Settings.Network, NVG.Settings.Layer, true);
                                                        }

                                                        ZipArchiveList.Add(fif.Name, Val_BlockVerify);
                                                        Control_RealBlockList.Add(ControlBlock.info.uID, ControlBlock);
                                                        BlockOrderList.Add(ControlBlock.info.rowNo, ControlBlock.info.uID);
                                                        BlockPreviousList.Add(ControlBlock.info.uID, ControlBlock.prev);
                                                        BlockTypeList.Add(ControlBlock.info.uID, ControlBlock.info.type);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception err)
                                    {
                                        NP.Danger(NVG.Settings, "Error Text [235abc]: " + err.Message);
                                    }
                                }
                            }
                        }
                    }
                }
                if (tmpDeleteFileList.Count > 0)
                {
                    Thread.Sleep(1);
                    Notus.Archive.DeleteFromInside(
                        fileName,
                        tmpDeleteFileList,
                        true
                    );
                    NP.Danger(NVG.Settings, "Repair Block Integrity = Contains Wrong / Extra Data");
                    if (returnForCheckAgain == true)
                    {
                        return (Notus.Variable.Enum.BlockIntegrityStatus.CheckAgain, null);
                    }
                }
            }

            if (SmallestBlockHeight > 1)
            {
                NP.Danger(NVG.Settings, "Repair Block Integrity = Missing Block Available");
                bool exitInnerLoop = false;
                while (exitInnerLoop == false)
                {
                    SmallestBlockHeight--;
                    if (SmallestBlockHeight == 0)
                    {
                        exitInnerLoop = true;
                    }
                    else
                    {
                        if (
                            NVG.Settings.NodeType != Notus.Variable.Enum.NetworkNodeType.Main &&
                            NVG.Settings.NodeType != Notus.Variable.Enum.NetworkNodeType.Master
                        )
                        {
                            StoreBlockWithRowNo(SmallestBlockHeight);
                        }
                        else
                        {
                            if(BlockOrderList.ContainsKey(BiggestBlockHeight - 1))
                            {
                                Notus.Archive.DeleteFromInside(
                                    BlockOrderList[BiggestBlockHeight - 1],
                                    NVG.Settings,
                                    true
                                );
                                NP.Danger(NVG.Settings, "Repair Block Integrity = Missing Block [45abcfe713]");
                            }
                        }
                    }
                }
                return (Notus.Variable.Enum.BlockIntegrityStatus.CheckAgain, null);
            }

            long controlNumber = 1;
            bool rowNumberError = false;
            foreach (KeyValuePair<long, string> item in BlockOrderList)
            {
                if (item.Key != controlNumber)
                {
                    StoreBlockWithRowNo(controlNumber);
                    controlNumber = item.Key;
                    rowNumberError = true;
                }
                controlNumber++;
            }
            if (rowNumberError == true)
            {
                return (Notus.Variable.Enum.BlockIntegrityStatus.CheckAgain, null);
            }

            bool prevBlockRownNumberError = false;
            bool whileExit = false;
            while (whileExit == false)
            {
                string BlockIdStr = BlockOrderList[BiggestBlockHeight];
                if (BlockPreviousList[BlockIdStr].Length > 0)
                {
                    if (
                        string.Equals(
                            BlockPreviousList[BlockIdStr].Substring(0, BlockIdStr.Length),
                            BlockOrderList[BiggestBlockHeight - 1]
                        ) == false
                    )
                    {

                        /*
                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine(JsonSerializer.Serialize(BlockPreviousList));
                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine(JsonSerializer.Serialize(BlockOrderList));
                        Console.WriteLine("");
                        Console.WriteLine("");
                        */
                        Notus.Archive.DeleteFromInside(
                            BlockOrderList[BiggestBlockHeight - 1],
                            NVG.Settings,
                            true
                        );
                        prevBlockRownNumberError = true;
                        whileExit = true;
                    }
                }
                else
                {
                    if (BiggestBlockHeight == 1 && string.Equals(Notus.Variable.Constant.GenesisBlockUid, BlockIdStr))
                    {
                        whileExit = true;
                    }
                }
                if (BiggestBlockHeight > 0)
                {
                    BiggestBlockHeight--;
                }
                else
                {
                    whileExit = true;
                }
            }
            if (prevBlockRownNumberError == true)
            {
                NP.Danger(NVG.Settings, "Repair Block Integrity = Wrong Block Order");
                return (Notus.Variable.Enum.BlockIntegrityStatus.CheckAgain, null);
            }
            NP.Success(NVG.Settings, "Block Integrity Valid");

            foreach (KeyValuePair<long, string> item in BlockOrderList)
            {
                if (NGF.BlockOrder.ContainsKey(item.Key) == false)
                {
                    NGF.BlockOrder.TryAdd(item.Key, item.Value);
                }
                //NGF.BlockOrder.Add(item.Key.ToString(), item.Value);
            }
            return (Notus.Variable.Enum.BlockIntegrityStatus.Valid, LastBlock);
        }

        private (string, string) GetBlockSign(Int64 BlockRowNo)
        {
            string tmpBlockKeyStr = string.Empty;
            string tmpBlockSignStr = string.Empty;
            bool exitInnerLoop = false;
            while (exitInnerLoop == false)
            {
                for (int a = 0; a < Notus.Variable.Constant.ListMainNodeIp.Count && exitInnerLoop == false; a++)
                {
                    string nodeIpAddress = Notus.Variable.Constant.ListMainNodeIp[a];
                    try
                    {
                        string MainResultStr = Notus.Communication.Request.GetSync(
                            Notus.Network.Node.MakeHttpListenerPath(
                                nodeIpAddress,
                                Notus.Network.Node.GetNetworkPort(NVG.Settings.Network, NVG.Settings.Layer)
                            ) + "block/hash/" + BlockRowNo.ToString(),
                            10,
                            true,
                            true,
                            NVG.Settings
                        );
                        if (MainResultStr.Length > 90)
                        {
                            exitInnerLoop = true;
                            tmpBlockKeyStr = MainResultStr.Substring(0, 90);
                            tmpBlockSignStr = MainResultStr.Substring(90);
                        }
                        else
                        {
                            Thread.Sleep(5000);
                        }
                    }
                    catch (Exception err)
                    {
                        NP.Basic(NVG.Settings.DebugMode, "Error Text [96a3c2]: " + err.Message);
                        Thread.Sleep(5000);
                    }
                }
            }
            return (tmpBlockKeyStr, tmpBlockSignStr);

        }
        
        //control-local-block
        private bool AddFromLocalTemp(Int64 BlockRowNo)
        {
            string[] ZipFileList = Notus.IO.GetFileList(NVG.Settings, Notus.Variable.Constant.StorageFolderName.TempBlock, "tmp");
            for(int i=0; i< ZipFileList.Length; i++)
            {
                string textBlockData = File.ReadAllText(ZipFileList[i]);
                Notus.Variable.Class.BlockData? tmpBlockData = JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(textBlockData);
                if (tmpBlockData != null)
                {
                    if (tmpBlockData.info.rowNo == BlockRowNo)
                    {
                        using (Notus.Block.Storage BS_Storage = new Notus.Block.Storage(false))
                        {
                            BS_Storage.AddSync(tmpBlockData, true);
                        }
                        return true;
                    }
                }
                else
                {
                }
            }
            return false;
        }
        private void StoreBlockWithRowNo(Int64 BlockRowNo)
        {
            /*
            Console.WriteLine("BlockRowNo Does Not Exist : " + BlockRowNo.ToString());
            */
            //control-local-block
            //bool localFound=AddFromLocalTemp(BlockRowNo);

            bool localFound = false;
            if (localFound == false)
            {
                bool debugPrinted= false;
                bool exitInnerLoop = false;
                while (exitInnerLoop == false)
                {
                    for (int a = 0; a < Notus.Variable.Constant.ListMainNodeIp.Count && exitInnerLoop == false; a++)
                    {
                        string myIpAddress = (NVG.Settings.LocalNode == true ? NVG.Settings.IpInfo.Local : NVG.Settings.IpInfo.Public);
                        string nodeIpAddress = Notus.Variable.Constant.ListMainNodeIp[a];
                        if (string.Equals(myIpAddress, nodeIpAddress) == false)
                        {
                            string MainResultStr = string.Empty;
                            try
                            {
                                string nodeUrl = Notus.Network.Node.MakeHttpListenerPath(
                                        nodeIpAddress,
                                        Notus.Network.Node.GetNetworkPort(NVG.Settings)
                                    );
                                MainResultStr = Notus.Communication.Request.GetSync(
                                     nodeUrl + "block/" + BlockRowNo.ToString() + "/raw",
                                    10,
                                    true,
                                    true,
                                    NVG.Settings
                                );
                                Notus.Variable.Class.BlockData? tmpEmptyBlock = JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(MainResultStr);
                                if (tmpEmptyBlock != null)
                                {
                                    NP.Info(NVG.Settings, "Getting Block Row No [ " + nodeUrl + " ]: " + BlockRowNo.ToString());
                                    using (Notus.Block.Storage BS_Storage = new Notus.Block.Storage(false))
                                    {
                                        BS_Storage.AddSync(tmpEmptyBlock, true);
                                    }
                                    exitInnerLoop = true;
                                }
                            }
                            catch (Exception err)
                            {
                                if (debugPrinted == false)
                                {
                                    NP.Basic(NVG.Settings.DebugMode, "Error Text [5a6e84]: " + err.Message);
                                    NP.Basic(NVG.Settings.DebugMode, "Income Text [5a6e84]: " + MainResultStr);
                                    debugPrinted = true;
                                }
                                else
                                {
                                    Console.Write(".");
                                }
                            }
                            if (exitInnerLoop == true)
                            {
                                Thread.Sleep(2500);
                            }
                        }
                    }
                    if (exitInnerLoop == false)
                    {
                        Thread.Sleep(5000);
                    }
                }
            }
        }
        private Notus.Variable.Class.BlockData GiveMeEmptyBlock(Notus.Variable.Class.BlockData FreeBlockStruct, string PrevStr)
        {   
            FreeBlockStruct.info.type = 300;
            FreeBlockStruct.info.rowNo = 2;
            FreeBlockStruct.info.multi = false;
            FreeBlockStruct.info.uID = Notus.Block.Key.Generate(
                ND.ToDateTime(NVG.NOW.Int), 
                NVG.Settings.NodeWallet.WalletKey
            );

            FreeBlockStruct.info.time = Notus.Block.Key.GetTimeFromKey(FreeBlockStruct.info.uID, true);
            FreeBlockStruct.cipher.ver = "NE";
            FreeBlockStruct.cipher.data = System.Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes(
                    JsonSerializer.Serialize(1)
                )
            );

            FreeBlockStruct.prev = PrevStr;
            FreeBlockStruct.info.prevList.Clear();
            FreeBlockStruct.info.prevList.Add(360, PrevStr);
            return new Notus.Block.Generate(NVG.Settings.NodeWallet.WalletKey).Make(FreeBlockStruct, 1000);
        }
        private Notus.Variable.Class.BlockData GiveMeGenesisBlock(Notus.Variable.Class.BlockData GenBlockStruct)
        {
            if (NVG.Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
            {
                NVG.Settings.Genesis = Notus.Block.Genesis.Generate(NVG.Settings.NodeWallet.WalletKey, NVG.Settings.Network, NVG.Settings.Layer);
            }
            else
            {
                string tmpResult = Notus.Network.Node.FindAvailableSync(
                    "block/" + Notus.Variable.Constant.GenesisBlockUid,
                    NVG.Settings.Network,
                    Notus.Variable.Enum.NetworkLayer.Layer1,
                    NVG.Settings.DebugMode,
                    NVG.Settings
                );
                Notus.Variable.Class.BlockData? ControlBlock = JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(tmpResult);
                if (ControlBlock != null)
                {
                    NVG.Settings.Genesis = JsonSerializer.Deserialize<Notus.Variable.Genesis.GenesisBlockData>(
                        System.Convert.FromBase64String(
                            ControlBlock.cipher.data
                        )
                    );
                }
            }

            GenBlockStruct.info.type = 360;
            GenBlockStruct.info.rowNo = 1;
            GenBlockStruct.info.multi = false;
            GenBlockStruct.info.uID = Notus.Variable.Constant.GenesisBlockUid;
            GenBlockStruct.prev = "";
            GenBlockStruct.info.prevList.Clear();
            GenBlockStruct.info.time = Notus.Block.Key.GetTimeFromKey(GenBlockStruct.info.uID, true);
            GenBlockStruct.cipher.ver = "NE";
            GenBlockStruct.cipher.data = System.Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes(
                    JsonSerializer.Serialize(NVG.Settings.Genesis)
                )
            );
            return new Notus.Block.Generate(NVG.Settings.NodeWallet.WalletKey).Make(GenBlockStruct, 1000);
        }
        public void Synchronous()
        {

        }
        public bool ControlGenesisBlock()
        {
            //string[] ZipFileList = Notus.IO.GetZipFiles(NVG.Settings);
            string ZipFileName = Notus.IO.GetFolderName(
                NVG.Settings.Network, 
                NVG.Settings.Layer, 
                Notus.Variable.Constant.StorageFolderName.Block
            ) + 
            Notus.Block.Key.GetBlockStorageFileName(
                Notus.Variable.Constant.GenesisBlockUid, 
                true
            ) + ".zip";
            string myGenesisSign = string.Empty;
            
            DateTime myGenesisTime = NVG.NOW.Obj.AddDays(1);
            //if (ZipFileList.Length > 0)
            if (File.Exists(ZipFileName) ==true)
            {
                using (Notus.Block.Storage BS_Storage = new Notus.Block.Storage(false))
                {
                    //tgz-exception
                    Notus.Variable.Class.BlockData? blockData = BS_Storage.ReadBlock(Notus.Variable.Constant.GenesisBlockUid);
                    if (blockData != null)
                    {
                        if (blockData.info.type == 360)
                        {
                            myGenesisSign = blockData.sign;
                            myGenesisTime = ND.GetGenesisCreationTimeFromString(blockData);
                        }
                    }
                }
            }
            if (NVG.Settings.LocalNode == false)
            {
                if (myGenesisSign.Length == 0)
                {
                    return false;
                }
            }
            //there is no layer on constant
            if (Notus.Validator.List.Main.ContainsKey(NVG.Settings.Layer) == false)
            {
                return false;
            }

            //there is no Network on constant
            if (Notus.Validator.List.Main[NVG.Settings.Layer].ContainsKey(NVG.Settings.Network) == false)
            {
                return false;
            }
            if (NVG.Settings.LocalNode == false)
            {
                Dictionary<string, Notus.Variable.Class.BlockData> signBlock = new Dictionary<string, Notus.Variable.Class.BlockData>();
                signBlock.Clear();

                Dictionary<string, int> signCount = new Dictionary<string, int>();
                signCount.Clear();

                Dictionary<string, List<Notus.Variable.Struct.IpInfo>> signNode = new Dictionary<string, List<Notus.Variable.Struct.IpInfo>>();
                signNode.Clear();

                foreach (Variable.Struct.IpInfo item in Notus.Validator.List.Main[NVG.Settings.Layer][NVG.Settings.Network])
                {
                    if (string.Equals(NVG.Settings.IpInfo.Public, item.IpAddress) == false)
                    {
                        NP.Info("Checking From -> " + item.IpAddress);
                        Notus.Variable.Class.BlockData? tmpInnerBlockData =
                        Notus.Toolbox.Network.GetBlockFromNode(item.IpAddress, item.Port, 1, NVG.Settings);
                        if (tmpInnerBlockData != null)
                        {
                            if (signCount.ContainsKey(tmpInnerBlockData.sign) == false)
                            {
                                signNode.Add(tmpInnerBlockData.sign, new List<Notus.Variable.Struct.IpInfo>() { });
                                signCount.Add(tmpInnerBlockData.sign, 0);
                                signBlock.Add(tmpInnerBlockData.sign, tmpInnerBlockData);
                            }
                            signNode[tmpInnerBlockData.sign].Add(
                                new Notus.Variable.Struct.IpInfo()
                                {
                                    IpAddress = item.IpAddress,
                                    Port = item.Port
                                }
                            );
                            signCount[tmpInnerBlockData.sign] = signCount[tmpInnerBlockData.sign] + 1;
                        }
                        else
                        {
                            NP.Danger(NVG.Settings, "Error Happened While Trying To Get Genesis From Other Node");
                            ND.SleepWithoutBlocking(100);
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
                    DateTime otherNodeGenesisTime = ND.GetGenesisCreationTimeFromString(signBlock[tmpBiggestSign]);
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
                            NP.Warning(NVG.Settings, "Current Block Were Deleted");

                            Notus.TGZArchiver.ClearBlocks();
                            Notus.Archive.ClearBlocks(NVG.Settings);
                            BS_Storage.AddSync(signBlock[tmpBiggestSign], true);
                            NP.Basic(NVG.Settings, "Added Block : " + signBlock[tmpBiggestSign].info.uID);
                            bool secondBlockAdded = false;
                            foreach (Variable.Struct.IpInfo? entry in signNode[tmpBiggestSign])
                            {
                                if (secondBlockAdded == false)
                                {
                                    Notus.Variable.Class.BlockData? tmpInnerBlockData =
                                    Notus.Toolbox.Network.GetBlockFromNode(entry.IpAddress, entry.Port, 2, NVG.Settings);
                                    if (tmpInnerBlockData != null)
                                    {
                                        NP.Basic(NVG.Settings, "Added Block : " + tmpInnerBlockData.info.uID);
                                        BS_Storage.AddSync(tmpInnerBlockData, true);
                                        secondBlockAdded = true;
                                    }
                                }
                            }
                        }
                        ND.SleepWithoutBlocking(150);
                    }
                    else
                    {
                        NP.Basic(NVG.Settings, "Hold Your Genesis Block - We Are Older");
                    }
                }
            }
            return true;
        }
        public void GetLastBlock()
        {
            Notus.Variable.Enum.BlockIntegrityStatus Val_Status = Notus.Variable.Enum.BlockIntegrityStatus.CheckAgain;
            Notus.Variable.Class.BlockData LastBlock = new Notus.Variable.Class.BlockData();
            bool exitInnerLoop = false;
            while (exitInnerLoop == false)
            {
                (
                    Notus.Variable.Enum.BlockIntegrityStatus tmpStatus,
                    Notus.Variable.Class.BlockData? tmpLastBlock
                ) = ControlBlockIntegrity();

                if (tmpStatus != Notus.Variable.Enum.BlockIntegrityStatus.CheckAgain)
                {
                    Val_Status = tmpStatus;
                    LastBlock = tmpLastBlock;
                    exitInnerLoop = true;
                }
            }

            if (Val_Status == Notus.Variable.Enum.BlockIntegrityStatus.GenesisNeed)
            {
                Notus.Variable.Class.BlockData tmpGenesisBlock = GiveMeGenesisBlock(
                    Notus.Variable.Class.Block.GetEmpty()
                );
                string tmpPrevStr = tmpGenesisBlock.info.uID + tmpGenesisBlock.sign;
                Notus.Variable.Class.BlockData tmpEmptyBlock = GiveMeEmptyBlock(
                        Notus.Variable.Class.Block.GetEmpty(),
                        tmpPrevStr
                    );

                using (Notus.Block.Storage BS_Storage = new Notus.Block.Storage(false))
                {
                    BS_Storage.AddSync(tmpGenesisBlock);
                    if (NVG.Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
                    {
                        BS_Storage.AddSync(tmpEmptyBlock);
                    }
                }
                NVG.Settings.GenesisCreated = true;
                if (NVG.Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
                {
                    NVG.Settings.LastBlock = tmpEmptyBlock;
                }
                else
                {
                    NVG.Settings.LastBlock = tmpGenesisBlock;
                }
            }
            else
            {
                NVG.Settings.GenesisCreated = false;
                NVG.Settings.LastBlock = LastBlock;
            }
        }
        public Integrity()
        {
        }
        ~Integrity()
        {
            Dispose();
        }
        public void Dispose()
        {
        }
    }
}
