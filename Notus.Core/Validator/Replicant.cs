using System;
using System.Collections.Generic;
namespace Notus.Validator
{
    public class Replicant : IDisposable
    {
        private Notus.Variable.Common.ClassSetting Obj_Settings;
        public Notus.Variable.Common.ClassSetting Settings
        {
            get { return Obj_Settings; }
            set { Obj_Settings = value; }
        }

        private bool LightNodeActive = true;
        public bool LightNode
        {
            get { return LightNodeActive; }
            set { LightNodeActive = value; }
        }
        private Notus.Block.Integrity Obj_Integrity;
        //private Notus.Wallet.Balance Obj_Balance;

        private Notus.Block.Storage Obj_Storage;

        private Dictionary<string, Notus.Variable.Class.BlockData> AllMainList = new Dictionary<string, Notus.Variable.Class.BlockData>();
        private Dictionary<string, Notus.Variable.Class.BlockData> AllMasterList = new Dictionary<string, Notus.Variable.Class.BlockData>();
        private List<string> AllReplicantList = new List<string>();

        private void GetBlockFromNode(string NodeAddress, Int64 BlockRowNo, bool AssingToLastBlockVar)
        {
            Notus.Debug.Print.Basic(Obj_Settings.DebugMode, "Getting Block Row No : " + BlockRowNo.ToString());
            bool exitWhileLoop = false;
            while (exitWhileLoop == false)
            {
                (bool NoError, Notus.Variable.Class.BlockData tmpBlockData) = Notus.Validator.Query.GetBlock(NodeAddress, BlockRowNo);
                if (NoError == true)
                {
                    Obj_Storage.AddSync(tmpBlockData);
                    exitWhileLoop = true;
                    if (AssingToLastBlockVar == true)
                    {
                        Obj_Settings.LastBlock = tmpBlockData;
                    }
                }
                else
                {
                    SleepWithoutBlocking(5, false);
                    Console.Write(".");
                }
            }
        }
        public void Start()
        {
            Notus.IO.NodeFolderControl(Obj_Settings.Network, Obj_Settings.Layer);
            Notus.Debug.Print.Basic(Obj_Settings, "Replicant Started");
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
                    Obj_Settings.Network,
                    Obj_Settings.Layer
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
            Obj_Storage.Network = Obj_Settings.Network;
            Obj_Storage.Layer = Obj_Settings.Layer;

            Obj_Integrity = new Notus.Block.Integrity();
            Obj_Integrity.Settings = Obj_Settings;

            (bool validBlock, Notus.Variable.Class.BlockData tmpLastBlock) = Obj_Integrity.GetSatus(true);
            if (validBlock == true)
            {
                Obj_Settings.LastBlock = tmpLastBlock;
                Notus.Debug.Print.Basic(Obj_Settings, "All Blocks Valid");
            }
            else
            {
                Notus.Debug.Print.Basic(Obj_Settings, "Non-Valid Blocks");
                if (notEmpty == true && tmpMasterList.Count > 0)
                {
                    Notus.Debug.Print.Basic(Obj_Settings, "Smallest block height : " + smallestBlockRownNo.ToString());
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
                    Notus.Debug.Print.Basic(Obj_Settings, "Last Block reading issues");
                    //Console.ReadLine();
                }
            }
            Int64 MN_LastBlockRowNo = smallestBlockRownNo;

            Obj_Settings.Genesis = Obj_Integrity.Settings.Genesis;

            DateTime LastPrintTime = DateTime.Now;
            if (Obj_Settings.LastBlock.info.rowNo != MN_LastBlockRowNo)
            {
                LastPrintTime.Subtract(new TimeSpan(0, 0, 11));
            }
            while (true)
            {
                if ((DateTime.Now - LastPrintTime).TotalSeconds > 10)
                {
                    LastPrintTime = DateTime.Now;
                    if (Obj_Settings.LastBlock.info.rowNo == MN_LastBlockRowNo)
                    {
                        Notus.Debug.Print.Basic(Obj_Settings, "Checking Block Height");
                        (bool tmpNoError, Notus.Variable.Struct.LastBlockInfo tmpLastBlockInfo) = Notus.Validator.Query.GetLastBlockInfo(NodeAddress);
                        if (tmpNoError == true)
                        {
                            if (MN_LastBlockRowNo == tmpLastBlockInfo.RowNo)
                            {
                                Notus.Debug.Print.Basic(Obj_Settings, "Chain Could Not Change");
                                SleepWithoutBlocking(5, false);
                            }
                            else
                            {
                                MN_LastBlockRowNo = tmpLastBlockInfo.RowNo;
                                Notus.Debug.Print.Basic(Obj_Settings, "There Are New Blocks");
                            }
                        }
                        else
                        {
                            Notus.Debug.Print.Basic(Obj_Settings, "Last Block Row Number Could Not Get");
                            SleepWithoutBlocking(2, false);
                        }
                    }
                    else
                    {
                        Notus.Debug.Print.Basic(Obj_Settings, "Block Sync Starting");
                        bool SyncCompleted = false;
                        while (SyncCompleted == false)
                        {
                            if (Obj_Settings.LastBlock.info.rowNo == MN_LastBlockRowNo)
                            {
                                Notus.Debug.Print.Basic(Obj_Settings, "Block Sync Finished");
                                SyncCompleted = true;
                            }
                            else
                            {
                                Int64 tmpNextBlockRowNo = Obj_Settings.LastBlock.info.rowNo + 1;
                                GetBlockFromNode(NodeAddress, tmpNextBlockRowNo, true);
                            }
                        }
                        SleepWithoutBlocking(5, false);
                    }
                }
            }
        }

        private void SleepWithoutBlocking(int SleepTime, bool Milisecond)
        {
            DateTime Sonraki;
            if (Milisecond == true)
            {
                Sonraki = DateTime.Now.AddMilliseconds(SleepTime);
            }
            else
            {
                Sonraki = DateTime.Now.AddSeconds(SleepTime);
            }
            while (Sonraki > DateTime.Now)
            {

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
