using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading;

namespace Notus.Block
{
    public class Integrity : IDisposable
    {
        private DateTime LastNtpTime = Notus.Variable.Constant.DefaultTime;
        private TimeSpan NtpTimeDifference;
        private bool NodeTimeAfterNtpTime = false;      // time difference before or after NTP Server
        private Notus.Variable.Common.ClassSetting Obj_Settings;
        public Notus.Variable.Common.ClassSetting Settings
        {
            get { return Obj_Settings; }
            set { Obj_Settings = value; }
        }

        private int Val_EmptyBlockCount = 0;
        public int EmptyBlockCount
        {
            get { return Val_EmptyBlockCount; }
        }
        private const string Const_DefaultPreText = "notus-block-queue";

        public (bool, Notus.Variable.Class.BlockData) GetSatus(bool ResetBlocksIfNonValid = false)
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
                return (true, LastBlock);
            }
            if (ResetBlocksIfNonValid == true)
            {
                string[] ZipFileList = Notus.IO.GetZipFiles(Obj_Settings);
                foreach (string fileName in ZipFileList)
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch (Exception err)
                    {

                        Notus.Print.Danger(Obj_Settings, "Error Text [7abc63]: " + err.Message);
                    }
                }
            }
            return (false, null);
        }
        private (Notus.Variable.Enum.BlockIntegrityStatus, Notus.Variable.Class.BlockData?) ControlBlockIntegrity()
        {
            Notus.Wallet.Fee.ClearFeeData(Obj_Settings.Network, Obj_Settings.Layer);

            Notus.Variable.Class.BlockData LastBlock = Notus.Variable.Class.Block.GetEmpty();
            string[] ZipFileList = Notus.IO.GetZipFiles(Obj_Settings);

            if (ZipFileList.Length == 0)
            {
                Notus.Print.Success(Obj_Settings, "Genesis Block Needs");
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
                    //Console.WriteLine("Delete Zip : " + fileName);
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
                        //Console.WriteLine("Entry Name : " + entry.FullName);
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
            if (multiBlockFound == true)
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
                //Console.WriteLine("Zip File Name : " + fileName);
                List<Int64> tmpUpdateBlockRowList = new List<Int64>();
                List<string> tmpDeleteFileList = new List<string>();
                bool returnForCheckAgain = false;
                using (ZipArchive archive = ZipFile.OpenRead(fileName))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        //Console.WriteLine("Entry Name : " + entry.FullName);
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
                                                Notus.Print.Danger(Obj_Settings, "Block Integrity = NonValid");
                                                tmpDeleteFileList.Add(entry.FullName);
                                            }
                                            else
                                            {
                                                if (BlockOrderList.ContainsKey(ControlBlock.info.rowNo))
                                                {
                                                    Notus.Print.Danger(Obj_Settings, "Block Integrity = MultipleHeight -> " + ControlBlock.info.rowNo.ToString());
                                                    tmpDeleteFileList.Add(entry.FullName);
                                                    returnForCheckAgain = true;
                                                }
                                                else
                                                {
                                                    if (BlockPreviousList.ContainsKey(ControlBlock.info.uID))
                                                    {
                                                        Notus.Print.Danger(Obj_Settings, "Block Integrity = MultipleId -> " + ControlBlock.info.uID);
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
                                                            Obj_Settings.Genesis = JsonSerializer.Deserialize<Notus.Variable.Genesis.GenesisBlockData>(
                                                                System.Convert.FromBase64String(
                                                                    ControlBlock.cipher.data
                                                                )
                                                            );
                                                            Notus.Wallet.Fee.StoreFeeData("genesis_block", JsonSerializer.Serialize(Obj_Settings.Genesis), Obj_Settings.Network, Obj_Settings.Layer, true);
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
                                        Notus.Print.Danger(Obj_Settings, "Error Text [235abc]: " + err.Message);
                                    }
                                }
                            }
                        }
                    }
                }
                if (tmpDeleteFileList.Count > 0)
                {
                    //Console.WriteLine(JsonSerializer.Serialize(BlockOrderList));
                    //Console.WriteLine(JsonSerializer.Serialize(tmpDeleteFileList));
                    Thread.Sleep(1);
                    Notus.Archive.DeleteFromInside(
                        fileName,
                        tmpDeleteFileList,
                        true
                    );
                    Console.WriteLine(returnForCheckAgain);
                    Notus.Print.Danger(Obj_Settings, "Repair Block Integrity = Contains Wrong / Extra Data");
                    Console.ReadLine();
                    if (returnForCheckAgain == true)
                    {
                        return (Notus.Variable.Enum.BlockIntegrityStatus.CheckAgain, null);
                    }
                }
            }

            if (SmallestBlockHeight > 1)
            {
                Notus.Print.Danger(Obj_Settings, "Repair Block Integrity = Missing Block Available");
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
                            Obj_Settings.NodeType != Notus.Variable.Enum.NetworkNodeType.Main &&
                            Obj_Settings.NodeType != Notus.Variable.Enum.NetworkNodeType.Master
                        )
                        {
                            StoreBlockWithRowNo(SmallestBlockHeight);
                        }
                        else
                        {
                            if (BlockOrderList.ContainsKey(BiggestBlockHeight - 1))
                            {
                                Notus.Archive.DeleteFromInside(
                                    BlockOrderList[BiggestBlockHeight - 1],
                                    Obj_Settings,
                                    true
                                );
                                Notus.Print.Danger(Obj_Settings, "Repair Block Integrity = Missing Block [45abcfe713]");
                            }
                        }
                    }
                }
                return (Notus.Variable.Enum.BlockIntegrityStatus.CheckAgain, null);
            }


            //Console.WriteLine(JsonSerializer.Serialize(BlockOrderList, Notus.Variable.Constant.JsonSetting);
            //Console.ReadLine();

            //Console.WriteLine(Obj_Settings.NodeType);
            //Console.WriteLine(Obj_Settings.NodeType);
            long controlNumber = 1;
            bool rowNumberError = false;
            foreach (KeyValuePair<long, string> item in BlockOrderList)
            {
                if (item.Key != controlNumber)
                {
                    StoreBlockWithRowNo(controlNumber);
                    // Console.WriteLine("We Need This Block :" + controlNumber.ToString());
                    // Notus.Print.Info(Obj_Settings, "We Get Block From Other Node > " + controlNumber.ToString());
                    /*
                    if (
                        Obj_Settings.NodeType != Notus.Variable.Enum.NetworkNodeType.Main &&
                        Obj_Settings.NodeType != Notus.Variable.Enum.NetworkNodeType.Master
                    )
                    {
                    }
                    else
                    {
                        //Notus.Print.Danger(Obj_Settings, "Block Order Error > " + controlNumber.ToString() + " / " + item.Key + " > " + item.Value.Substring(0, 10) + ".." + item.Value.Substring(80));
                        //Notus.Archive.DeleteFromInside(item.Value, Obj_Settings, true);
                    }
                    */
                    controlNumber = item.Key;
                    rowNumberError = true;
                }
                controlNumber++;
            }
            if (rowNumberError == true)
            {
                //Console.WriteLine(JsonSerializer.Serialize(BlockOrderList, Notus.Variable.Constant.JsonSetting);
                //Console.ReadLine();

                return (Notus.Variable.Enum.BlockIntegrityStatus.CheckAgain, null);
            }

            bool prevBlockRownNumberError = false;
            bool whileExit = false;
            while (whileExit == false)
            {
                string BlockIdStr = BlockOrderList[BiggestBlockHeight];
                if (BlockTypeList[BlockIdStr] == 300)
                {
                    Val_EmptyBlockCount++;
                }
                else
                {
                    if (BlockTypeList[BlockIdStr] != 360)
                    {
                        Val_EmptyBlockCount = 0;
                    }
                }

                if (BlockPreviousList[BlockIdStr].Length > 0)
                {
                    if (
                        string.Equals(
                            BlockPreviousList[BlockIdStr].Substring(0, BlockIdStr.Length),
                            BlockOrderList[BiggestBlockHeight - 1]
                        ) == false
                    )
                    {
                        Notus.Archive.DeleteFromInside(
                            BlockOrderList[BiggestBlockHeight - 1],
                            Obj_Settings,
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
                Notus.Print.Danger(Obj_Settings, "Repair Block Integrity = Wrong Block Order");
                return (Notus.Variable.Enum.BlockIntegrityStatus.CheckAgain, null);
            }
            Notus.Print.Success(Obj_Settings, "Block Integrity Valid");

            using (Notus.Mempool ObjMp_BlockOrder =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(Obj_Settings.Network, Obj_Settings.Layer, Notus.Variable.Constant.StorageFolderName.Common) +
                    "block_order_list"
                )
            )
            {
                ObjMp_BlockOrder.AsyncActive = false;
                ObjMp_BlockOrder.Clear();
                foreach (KeyValuePair<long, string> item in BlockOrderList)
                {
                    ObjMp_BlockOrder.Add(
                        item.Key.ToString(),
                        JsonSerializer.Serialize(
                            new KeyValuePair<string, string>(
                                item.Value,
                                new Notus.Hash().CommonSign("sha1", item.Value + Obj_Settings.HashSalt)
                            )
                        )
                    );
                }
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
                                Notus.Network.Node.GetNetworkPort(Obj_Settings.Network, Obj_Settings.Layer)
                            ) + "block/hash/" + BlockRowNo.ToString(),
                            10,
                            true,
                            true,
                            Obj_Settings
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
                        Notus.Print.Basic(Obj_Settings.DebugMode, "Error Text [96a3c2]: " + err.Message);
                        Thread.Sleep(5000);
                    }
                }
            }
            return (tmpBlockKeyStr, tmpBlockSignStr);

        }
        private void StoreBlockWithRowNo(Int64 BlockRowNo)
        {
            bool exitInnerLoop = false;
            while (exitInnerLoop == false)
            {
                for (int a = 0; a < Notus.Variable.Constant.ListMainNodeIp.Count && exitInnerLoop == false; a++)
                {
                    string myIpAddress = (Obj_Settings.LocalNode == true ? Obj_Settings.IpInfo.Local : Obj_Settings.IpInfo.Public);
                    string nodeIpAddress = Notus.Variable.Constant.ListMainNodeIp[a];
                    if (string.Equals(myIpAddress, nodeIpAddress) == false)
                    {
                        string MainResultStr = string.Empty;
                        try
                        {
                            string nodeUrl = Notus.Network.Node.MakeHttpListenerPath(
                                    nodeIpAddress,
                                    Notus.Network.Node.GetNetworkPort(Obj_Settings)
                                );
                            MainResultStr = Notus.Communication.Request.GetSync(
                                 nodeUrl + "block/" + BlockRowNo.ToString() + "/raw",
                                10,
                                true,
                                true,
                                Obj_Settings
                            );
                            Notus.Variable.Class.BlockData? tmpEmptyBlock = JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(MainResultStr);
                            if (tmpEmptyBlock != null)
                            {
                                Notus.Print.Info(Obj_Settings, "Getting Block Row No [ " + nodeUrl + " ]: " + BlockRowNo.ToString());
                                using (Notus.Block.Storage BS_Storage = new Notus.Block.Storage(false))
                                {
                                    BS_Storage.Network = Obj_Settings.Network;
                                    BS_Storage.Layer = Obj_Settings.Layer;
                                    BS_Storage.AddSync(tmpEmptyBlock, true);
                                }
                                exitInnerLoop = true;
                            }
                        }
                        catch (Exception err)
                        {
                            Notus.Print.Basic(Obj_Settings.DebugMode, "Error Text [5a6e84]: " + err.Message);
                            Notus.Print.Basic(Obj_Settings.DebugMode, "Income Text [5a6e84]: " + MainResultStr);
                        }
                        if (exitInnerLoop == true)
                        {
                            Thread.Sleep(2500);
                        }
                    }
                }
            }
        }
        private Notus.Variable.Class.BlockData GiveMeEmptyBlock(Notus.Variable.Class.BlockData FreeBlockStruct, string PrevStr)
        {
            FreeBlockStruct.info.type = 300;
            FreeBlockStruct.info.rowNo = 2;
            FreeBlockStruct.info.multi = false;
            FreeBlockStruct.info.uID = Notus.Block.Key.Generate(GetNtpTime(), Obj_Settings.NodeWallet.WalletKey);
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
            return new Notus.Block.Generate(Obj_Settings.NodeWallet.WalletKey).Make(FreeBlockStruct, 1000);
        }

        private Notus.Variable.Class.BlockData GiveMeGenesisBlock(Notus.Variable.Class.BlockData GenBlockStruct)
        {
            if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
            {
                Obj_Settings.Genesis = Notus.Block.Genesis.Generate(Obj_Settings.NodeWallet.WalletKey, Obj_Settings.Network, Obj_Settings.Layer);
            }
            else
            {
                string tmpResult = Notus.Network.Node.FindAvailableSync(
                    "block/" + Notus.Variable.Constant.GenesisBlockUid,
                    Obj_Settings.Network,
                    Notus.Variable.Enum.NetworkLayer.Layer1,
                    Obj_Settings.DebugMode,
                    Obj_Settings
                );
                Notus.Variable.Class.BlockData ControlBlock = JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(tmpResult);
                Obj_Settings.Genesis = JsonSerializer.Deserialize<Notus.Variable.Genesis.GenesisBlockData>(
                    System.Convert.FromBase64String(
                        ControlBlock.cipher.data
                    )
                );
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
                    JsonSerializer.Serialize(Obj_Settings.Genesis)
                )
            );
            return new Notus.Block.Generate(Obj_Settings.NodeWallet.WalletKey).Make(GenBlockStruct, 1000);
        }
        public void Synchronous()
        {

        }
        public bool ControlGenesisBlock()
        {
            string[] ZipFileList = Notus.IO.GetZipFiles(Obj_Settings);
            string myGenesisSign = string.Empty;
            DateTime myGenesisTime = DateTime.Now.AddDays(1);

            // we have genesis
            if (ZipFileList.Length > 0)
            {
                Notus.Print.Basic(Obj_Settings, "We Have Block - Lets Check Genesis Time And Hash");
                using (Notus.Block.Storage BS_Storage = new Notus.Block.Storage(false))
                {
                    BS_Storage.Network = Obj_Settings.Network;
                    BS_Storage.Layer = Obj_Settings.Layer;
                    Notus.Variable.Class.BlockData? blockData = BS_Storage.ReadBlock(Notus.Variable.Constant.GenesisBlockUid);
                    if (blockData != null)
                    {
                        if (blockData.info.type == 360)
                        {
                            myGenesisSign = blockData.sign;
                            myGenesisTime = Notus.Date.GetGenesisCreationTimeFromString(blockData);
                        }
                    }
                }
            }
            else
            {
                //Notus.Print.Basic(Obj_Settings, "We Do Not Have Any Block");
            }

            //there is no layer on constant
            if (Notus.Validator.List.Main.ContainsKey(Obj_Settings.Layer) == false)
            {
                return false;
            }

            //there is no Network on constant
            if (Notus.Validator.List.Main[Obj_Settings.Layer].ContainsKey(Obj_Settings.Network) == false)
            {
                return false;
            }

            Dictionary<string, Notus.Variable.Class.BlockData> signBlock = new Dictionary<string, Notus.Variable.Class.BlockData>();
            signBlock.Clear();

            Dictionary<string, int> signCount = new Dictionary<string, int>();
            signCount.Clear();

            Dictionary<string, List<Notus.Variable.Struct.IpInfo>> signNode = new Dictionary<string, List<Notus.Variable.Struct.IpInfo>>();
            signNode.Clear();

            foreach (Variable.Struct.IpInfo item in Notus.Validator.List.Main[Obj_Settings.Layer][Obj_Settings.Network])
            {
                if (string.Equals(Obj_Settings.IpInfo.Public, item.IpAddress) == false)
                {
                    Notus.Variable.Class.BlockData? tmpInnerBlockData =
                    Notus.Toolbox.Network.GetBlockFromNode(item.IpAddress, item.Port, 1, Obj_Settings);
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
                        Notus.Print.Danger(Obj_Settings, "Error Happened While Trying To Get Genesis From Other Node");
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
                        BS_Storage.Network = Obj_Settings.Network;
                        BS_Storage.Layer = Obj_Settings.Layer;
                        Notus.Print.Warning(Obj_Settings, "Current Block Were Deleted");
                        Notus.Archive.ClearBlocks(Obj_Settings);
                        BS_Storage.AddSync(signBlock[tmpBiggestSign], true);
                        Notus.Print.Basic(Obj_Settings, "Added Block : " + signBlock[tmpBiggestSign].info.uID);
                        //Console.WriteLine(JsonSerializer.Serialize(signNode));
                        bool secondBlockAdded = false;
                        foreach (Variable.Struct.IpInfo? entry in signNode[tmpBiggestSign])
                        {
                            if (secondBlockAdded == false)
                            {
                                Notus.Variable.Class.BlockData? tmpInnerBlockData =
                                Notus.Toolbox.Network.GetBlockFromNode(entry.IpAddress, entry.Port, 2, Obj_Settings);
                                if (tmpInnerBlockData != null)
                                {
                                    Notus.Print.Basic(Obj_Settings, "Added Block : " + tmpInnerBlockData.info.uID);
                                    BS_Storage.AddSync(tmpInnerBlockData, true);
                                    secondBlockAdded = true;
                                }
                            }
                        }
                    }
                    //Console.WriteLine("enter for continue");
                    //Console.ReadLine();
                    Notus.Date.SleepWithoutBlocking(150);
                }
                else
                {
                    Notus.Print.Basic(Obj_Settings, "Hold Your Genesis Block - We Are Older");
                }
            }
            //Console.WriteLine("Press Enter To Continue");
            //Console.ReadLine();
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
                    Notus.Variable.Class.BlockData tmpLastBlock
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
                    BS_Storage.Network = Obj_Settings.Network;
                    BS_Storage.Layer = Obj_Settings.Layer;
                    BS_Storage.AddSync(tmpGenesisBlock);
                    if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
                    {
                        BS_Storage.AddSync(tmpEmptyBlock);
                    }
                }
                Obj_Settings.GenesisCreated = true;
                if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
                {
                    Obj_Settings.LastBlock = tmpEmptyBlock;
                }
                else
                {
                    Obj_Settings.LastBlock = tmpGenesisBlock;
                }
            }
            else
            {
                Obj_Settings.GenesisCreated = false;
                Obj_Settings.LastBlock = LastBlock;
            }
        }
        private DateTime GetNtpTime()
        {
            if (
                string.Equals(
                    LastNtpTime.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText),
                    Notus.Variable.Constant.DefaultTime.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText)
                )
            )
            {
                LastNtpTime = Notus.Time.GetFromNtpServer();
                DateTime tmpNtpCheckTime = DateTime.Now;
                NodeTimeAfterNtpTime = (tmpNtpCheckTime > LastNtpTime);
                NtpTimeDifference = (NodeTimeAfterNtpTime == true ? (tmpNtpCheckTime - LastNtpTime) : (LastNtpTime - tmpNtpCheckTime));
                return LastNtpTime;
            }

            if (NodeTimeAfterNtpTime == true)
            {
                LastNtpTime = DateTime.Now.Subtract(NtpTimeDifference);
                return LastNtpTime;
            }
            LastNtpTime = DateTime.Now.Add(NtpTimeDifference);
            return LastNtpTime;
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
