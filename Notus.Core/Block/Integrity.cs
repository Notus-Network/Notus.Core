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
                string[] ZipFileList = GetZipFiles();
                foreach (string fileName in ZipFileList)
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch (Exception err)
                    {

                        Notus.Print.Basic(Obj_Settings.DebugMode, "Error Text [7abc63]: " + err.Message);
                    }
                }
            }
            return (false, null);
        }
        private string[] GetZipFiles()
        {
            if (Directory.Exists(Notus.Toolbox.IO.GetFolderName(Obj_Settings.Network, Obj_Settings.Layer, Notus.Variable.Constant.StorageFolderName.Block)) == false)
            {
                return new string[] { };
            }
            return Directory.GetFiles(Notus.Toolbox.IO.GetFolderName(Obj_Settings.Network, Obj_Settings.Layer, Notus.Variable.Constant.StorageFolderName.Block), "*.zip");
        }
        private (Notus.Variable.Enum.BlockIntegrityStatus, Notus.Variable.Class.BlockData) ControlBlockIntegrity()
        {
            Notus.Wallet.Fee.StoreFeeData("", "", Obj_Settings.Network, Obj_Settings.Layer, true);

            Notus.Variable.Class.BlockData LastBlock = Notus.Variable.Class.Block.GetEmpty();
            string[] ZipFileList = GetZipFiles();

            if (ZipFileList.Length == 0)
            {
                Notus.Print.Basic(Obj_Settings, "Block Integrity = GenesisNeed");
                return (Notus.Variable.Enum.BlockIntegrityStatus.GenesisNeed, null);
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
                            ZipArchiveEntry zipEntry = archive.GetEntry(entry.FullName);
                            if (zipEntry != null)
                            {
                                System.IO.FileInfo fif = new System.IO.FileInfo(entry.FullName);
                                using (StreamReader zipEntryStream = new StreamReader(zipEntry.Open()))
                                {
                                    try
                                    {
                                        Notus.Variable.Class.BlockData ControlBlock = JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(zipEntryStream.ReadToEnd());
                                        Notus.Block.Generate BlockValidateObj = new Notus.Block.Generate();
                                        bool Val_BlockVerify = BlockValidateObj.Verify(ControlBlock);
                                        if (Val_BlockVerify == false)
                                        {
                                            Notus.Print.Basic(Obj_Settings, "Block Integrity = NonValid");
                                        }
                                        else
                                        {
                                            if (BlockOrderList.ContainsKey(ControlBlock.info.rowNo))
                                            {
                                                Notus.Print.Basic(Obj_Settings, "Block Integrity = MultipleHeight");
                                            }
                                            else
                                            {
                                                if (BlockPreviousList.ContainsKey(ControlBlock.info.uID))
                                                {
                                                    Notus.Print.Basic(Obj_Settings, "Block Integrity = MultipleId");
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
                                    catch (Exception err)
                                    {
                                        Notus.Print.Basic(Obj_Settings.DebugMode, "Error Text [235abc]: " + err.Message);
                                    }
                                }
                            }
                        }
                    }
                }
                //if (tmpReDownloadBlockList.Count > 0)
                //{
                //Console.WriteLine("hatali blogu tekrar indir");
                //}

                if (tmpDeleteFileList.Count > 0)
                {
                    using (ZipArchive archive = ZipFile.Open(fileName, ZipArchiveMode.Update))
                    {
                        for (int i = 0; i < tmpDeleteFileList.Count; i++)
                        {
                            ZipArchiveEntry entry = archive.GetEntry(tmpDeleteFileList[i]);
                            if (entry != null)
                            {
                                entry.Delete();
                            }
                        }
                    }
                    Notus.Print.Basic(Obj_Settings, "Extra Data Was Deleted");
                }
            }

            if (SmallestBlockHeight > 1)
            {
                Notus.Print.Basic(Obj_Settings, "Missing Block Available");
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
                            Notus.Print.Basic(Obj_Settings.DebugMode, "Getting Block Row No : " + SmallestBlockHeight.ToString());
                            StoreBlockWithRowNo(SmallestBlockHeight);
                        }
                        else
                        {
                            Console.WriteLine("Block Error. [45abcfe713] : " + SmallestBlockHeight.ToString());
                            Console.ReadLine();
                        }
                    }
                }
                return (Notus.Variable.Enum.BlockIntegrityStatus.CheckAgain, null);
            }

            long controlNumber = 1;
            bool rowNumberError = false;
            bool prevBlockRownNumberError = false;

            foreach (KeyValuePair<long, string> item in BlockOrderList)
            {
                if (item.Key != controlNumber)
                {
                    if (
                        Obj_Settings.NodeType != Notus.Variable.Enum.NetworkNodeType.Main &&
                        Obj_Settings.NodeType != Notus.Variable.Enum.NetworkNodeType.Master
                    )
                    {
                        Notus.Print.Basic(Obj_Settings.DebugMode, "Getting Block Row No : " + SmallestBlockHeight.ToString());
                        StoreBlockWithRowNo(controlNumber);
                    }
                    else
                    {
                        Console.WriteLine("Block Error. [7745abcfe4] : " + controlNumber.ToString());
                        Console.ReadLine();
                    }

                    controlNumber = item.Key;
                    rowNumberError = true;
                }
                controlNumber++;
            }
            if (rowNumberError == true)
            {
                return (Notus.Variable.Enum.BlockIntegrityStatus.CheckAgain, null);
            }

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

                string PreviousBlockKey = BlockPreviousList[BlockIdStr];
                if (PreviousBlockKey.Length > 0)
                {
                    PreviousBlockKey = PreviousBlockKey.Substring(0, BlockIdStr.Length);
                    string controlBlockPrevStr = BlockOrderList[BiggestBlockHeight - 1];
                    if (string.Equals(PreviousBlockKey, controlBlockPrevStr) == false)
                    {
                        prevBlockRownNumberError = true;
                        whileExit = true;
                    }
                }
                else
                {
                    if (
                        BiggestBlockHeight == 1 &&
                        string.Equals(Notus.Variable.Constant.GenesisBlockUid, BlockIdStr)
                    )
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
                Notus.Print.Basic(Obj_Settings, "Block Integrity = WrongBlockOrder");
                return (Notus.Variable.Enum.BlockIntegrityStatus.WrongBlockOrder, null);
            }
            Notus.Print.Basic(Obj_Settings, "Block Integrity = Valid");

            using (Notus.Mempool ObjMp_BlockOrder =
                new Notus.Mempool(
                    Notus.Toolbox.IO.GetFolderName(Obj_Settings.Network, Obj_Settings.Layer, Notus.Variable.Constant.StorageFolderName.Common) +
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
                        string MainResultStr = Notus.Communication.Request.Get(
                            Notus.Network.Node.MakeHttpListenerPath(
                                nodeIpAddress,
                                Notus.Network.Node.GetNetworkPort(Obj_Settings.Network, Obj_Settings.Layer)
                            ) + "block/hash/" + BlockRowNo.ToString(),
                            10,
                            true
                        ).GetAwaiter().GetResult();
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
                    string nodeIpAddress = Notus.Variable.Constant.ListMainNodeIp[a];
                    string MainResultStr = string.Empty;
                    try
                    {

                        MainResultStr = Notus.Communication.Request.Get(
                            Notus.Network.Node.MakeHttpListenerPath(
                                nodeIpAddress,
                                Notus.Network.Node.GetNetworkPort(Obj_Settings.Network, Obj_Settings.Layer)
                            ) + "block/" + BlockRowNo.ToString(),
                            10,
                            true
                        ).GetAwaiter().GetResult();
                        Notus.Variable.Class.BlockData tmpEmptyBlock = JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(MainResultStr);
                        using (Notus.Block.Storage BS_Storage = new Notus.Block.Storage(false))
                        {
                            BS_Storage.Network = Obj_Settings.Network;
                            BS_Storage.Layer = Obj_Settings.Layer;
                            BS_Storage.AddSync(tmpEmptyBlock, true);
                        }
                        exitInnerLoop = true;
                    }
                    catch (Exception err)
                    {
                        Notus.Print.Basic(Obj_Settings.DebugMode, "Error Text [5a6e84]: " + err.Message);
                        Notus.Print.Basic(Obj_Settings.DebugMode, "Income Text [5a6e84]: " + MainResultStr);
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
                true,
                Notus.Variable.Constant.Seed_ForMainNet_BlockKeyGenerate,
                Const_DefaultPreText
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
                string tmpResult = Notus.Network.Node.FindAvailableSync("block/" + Notus.Variable.Constant.GenesisBlockUid, Obj_Settings.Network, Notus.Variable.Enum.NetworkLayer.Layer1);
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
        public void GetLastBlock()
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
