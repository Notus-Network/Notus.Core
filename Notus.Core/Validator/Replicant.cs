using System;
using System.Collections.Generic;
using NVG = Notus.Variable.Globals;
namespace Notus.Validator
{
    public class Replicant : IDisposable
    {
        private bool LightNodeActive = true;
        public bool LightNode
        {
            get { return LightNodeActive; }
            set { LightNodeActive = value; }
        }
        private Notus.Block.Integrity Obj_Integrity;

        private Notus.Block.Storage Obj_Storage;

        private Dictionary<string, Notus.Variable.Class.BlockData> AllMainList = new Dictionary<string, Notus.Variable.Class.BlockData>();
        private Dictionary<string, Notus.Variable.Class.BlockData> AllMasterList = new Dictionary<string, Notus.Variable.Class.BlockData>();
        private List<string> AllReplicantList = new List<string>();

        private void GetBlockFromNode(string NodeAddress, Int64 BlockRowNo, bool AssingToLastBlockVar)
        {
            Notus.Print.Info(NVG.Settings, "Getting Block Row No : " + BlockRowNo.ToString());
            bool exitWhileLoop = false;
            while (exitWhileLoop == false)
            {
                (bool NoError, Notus.Variable.Class.BlockData tmpBlockData) = 
                Notus.Validator.Query.GetBlock(
                    NodeAddress, BlockRowNo, 
                    NVG.Settings.DebugMode,
                    NVG.Settings
                );
                if (NoError == true)
                {
                    Obj_Storage.AddSync(tmpBlockData);
                    exitWhileLoop = true;
                    if (AssingToLastBlockVar == true)
                    {
                        NVG.Settings.LastBlock = tmpBlockData;
                    }
                }
                else
                {
                    Notus.Date.SleepWithoutBlocking(5, false);
                    Console.Write(".");
                }
            }
        }
        public void Start()
        {
            Notus.IO.NodeFolderControl();
            Notus.Print.Basic(NVG.Settings, "Replicant Started");
            AllMasterList.Clear();
            bool stayInTheLoop = true;
            // burada main node'lardaki en küçük row numarası alınıyor...
            string NodeAddress = "";
            Int64 smallestBlockRownNo = Int64.MaxValue;
            bool notEmpty = false;
            Dictionary<string, Notus.Variable.Class.BlockData> tmpMasterList = new Dictionary<string, Notus.Variable.Class.BlockData>();
            while (stayInTheLoop == true)
            {
                (notEmpty, tmpMasterList) = Notus.Validator.Query.LastBlockList(
                    Notus.Variable.Enum.NetworkNodeType.Master,
                    NVG.Settings.Network,
                    NVG.Settings.Layer
                );
                bool listChecked = false;
                foreach (KeyValuePair<string, Notus.Variable.Class.BlockData> tmpEntry in tmpMasterList)
                {
                    if (smallestBlockRownNo > tmpEntry.Value.info.rowNo)
                    {
                        smallestBlockRownNo = tmpEntry.Value.info.rowNo;
                        NodeAddress = tmpEntry.Key;
                        listChecked = true;
                    }
                }
                if (listChecked == true)
                {
                    stayInTheLoop = false;
                }
            }

            Obj_Storage = new Notus.Block.Storage(false);
            Obj_Integrity = new Notus.Block.Integrity();

            Notus.Variable.Class.BlockData tmpLastBlock = Obj_Integrity.GetSatus(true);
            if (tmpLastBlock != null)
            {
                NVG.Settings.LastBlock = tmpLastBlock;
                Notus.Print.Basic(NVG.Settings, "All Blocks Valid");
            }
            else
            {
                Notus.Print.Basic(NVG.Settings, "Non-Valid Blocks");
                if (notEmpty == true && tmpMasterList.Count > 0)
                {
                    Notus.Print.Basic(NVG.Settings, "Smallest block height : " + smallestBlockRownNo.ToString());
                    bool tmpDefinedLastBlock = false;
                    for (Int64 i = smallestBlockRownNo; i > 0; i--)
                    {
                        if (tmpDefinedLastBlock == false)
                        {
                            GetBlockFromNode(NodeAddress, i, true);
                            tmpDefinedLastBlock = true;
                        }
                        else
                        {
                            GetBlockFromNode(NodeAddress, i, false);
                        }
                    }
                }
                else
                {
                    Notus.Print.Basic(NVG.Settings, "Last Block reading issues");
                    //Console.ReadLine();
                }
            }
            Int64 MN_LastBlockRowNo = smallestBlockRownNo;
            DateTime LastPrintTime = NVG.NOW.Obj;
            if (NVG.Settings.LastBlock.info.rowNo != MN_LastBlockRowNo)
            {
                LastPrintTime.Subtract(new TimeSpan(0, 0, 11));
            }
            while (true)
            {
                if ((NVG.NOW.Obj - LastPrintTime).TotalSeconds > 10)
                {
                    LastPrintTime = NVG.NOW.Obj;
                    if (NVG.Settings.LastBlock.info.rowNo == MN_LastBlockRowNo)
                    {
                        Notus.Print.Basic(NVG.Settings, "Checking Block Height");
                        Notus.Variable.Struct.LastBlockInfo? tmpLastBlockInfo = Notus.Validator.Query.GetLastBlockInfo(NodeAddress,NVG.Settings);
                        if (tmpLastBlockInfo!=null)
                        {
                            if (MN_LastBlockRowNo == tmpLastBlockInfo.RowNo)
                            {
                                Notus.Print.Basic(NVG.Settings, "Chain Could Not Change");
                                Notus.Date.SleepWithoutBlocking(5, false);
                            }
                            else
                            {
                                MN_LastBlockRowNo = tmpLastBlockInfo.RowNo;
                                Notus.Print.Basic(NVG.Settings, "There Are New Blocks");
                            }
                        }
                        else
                        {
                            Notus.Print.Basic(NVG.Settings, "Last Block Row Number Could Not Get");
                            Notus.Date.SleepWithoutBlocking(2, false);
                        }
                    }
                    else
                    {
                        Notus.Print.Basic(NVG.Settings, "Block Sync Starting");
                        bool SyncCompleted = false;
                        while (SyncCompleted == false)
                        {
                            if (NVG.Settings.LastBlock.info.rowNo == MN_LastBlockRowNo)
                            {
                                Notus.Print.Basic(NVG.Settings, "Block Sync Finished");
                                SyncCompleted = true;
                            }
                            else
                            {
                                Int64 tmpNextBlockRowNo = NVG.Settings.LastBlock.info.rowNo + 1;
                                GetBlockFromNode(NodeAddress, tmpNextBlockRowNo, true);
                            }
                        }
                        Notus.Date.SleepWithoutBlocking(5, false);
                    }
                }
            }
        }

        public Replicant()
        {
        }
        ~Replicant()
        {
            Dispose();
        }
        public void Dispose()
        {
            //HttpObj.Dispose();
        }
    }
}
