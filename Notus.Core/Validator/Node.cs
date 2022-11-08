using System;
using System.Text.Json;
using System.Threading;
using NVG = Notus.Variable.Globals;
using NGF = Notus.Variable.Globals.Functions;
using NVC = Notus.Variable.Constant;
using NVE = Notus.Variable.Enum;
using NVS = Notus.Variable.Struct;
using NP = Notus.Print;
namespace Notus.Validator
{
    public static class Node
    {
        public static void Start(string[] argsFromCLI)
        {
            bool LightNodeActive = false;
            NGF.PreStart();

            using (Notus.Validator.Menu menuObj = new Notus.Validator.Menu())
            {
                menuObj.PreStart(argsFromCLI);
                menuObj.Start();
                menuObj.DefineMySetting();
            }

            if (NVG.Settings.NodeType != NVE.NetworkNodeType.Replicant)
            {
                LightNodeActive = false;
            }

            if (NVG.Settings.DevelopmentNode == true)
            {
                NVG.Settings.Network = NVE.NetworkType.DevNet;
                Notus.Validator.Node.Start(LightNodeActive);
            }
            else
            {
                NVG.Settings.Network = NVE.NetworkType.MainNet;
                Notus.Validator.Node.Start(LightNodeActive);
            }
        }
        public static void Start(bool LightNodeActive)
        {
            if (NVG.Settings.LocalNode == true)
            {
                NP.Info(NVG.Settings, "LocalNode Activated");
            }
            Notus.IO.NodeFolderControl();


            /*
            Notus.Block.Storage storageObj = new Notus.Block.Storage(false);

            //tgz-exception
            string LastBlockUid = "1348c02274960011734a5d9a654b68e8355d6a80586560b60a9cd4f6314f6234dd43851e7d88da27b4f879f02d";
            Notus.Variable.Class.BlockData? tmpBlockData = storageObj.ReadBlock(LastBlockUid);
            Console.WriteLine(JsonSerializer.Serialize(tmpBlockData, NVC.JsonSetting));
            Console.ReadLine();
            Console.ReadLine();
            */

            NP.Info(NVG.Settings, "Activated DevNET for " + NVC.LayerText[NVG.Settings.Layer]);
            Notus.Toolbox.Network.IdentifyNodeType(5);
            NGF.Start();

            switch (NVG.Settings.NodeType)
            {
                // if IP and port node written in the code
                case NVE.NetworkNodeType.Main:
                    StartAsMain();
                    break;

                // if node join the network
                case NVE.NetworkNodeType.Master:
                    StartAsMain();
                    break;

                // if node only store the data
                case NVE.NetworkNodeType.Replicant:
                    StartAsReplicant( LightNodeActive);
                    break;

                default:
                    break;
            }
            if(NVG.Settings.NodeClosing == false)
            {
                NP.Warning(NVG.Settings, "Task Ended");
            }
        }
        private static void StartAsMaster()
        {
            bool exitOuterLoop = false;
            while (exitOuterLoop == false)
            {
                using (Notus.Validator.Main MainObj = new Notus.Validator.Main())
                {
                    //MainObj.Settings = NodeSettings;
                    MainObj.Start();
                }

                if (NVG.Settings.NodeClosing == false)
                {
                    NP.Basic(NVG.Settings, "Sleep For 2.5 Seconds");
                    Thread.Sleep(2500);
                }
            }
        }
        private static void StartAsMain()
        {
            bool exitOuterLoop = false;
            while (exitOuterLoop == false && NVG.Settings.NodeClosing==false)
            {
                using (Notus.Validator.Main MainObj = new Notus.Validator.Main())
                {
                    MainObj.Start();
                }

                if (NVG.Settings.GenesisCreated == true)
                {
                    Environment.Exit(0);
                }

                if (NVG.Settings.NodeClosing == false)
                {
                    NP.Basic(NVG.Settings, "Sleep For 2.5 Seconds");
                    Thread.Sleep(2500);
                }
            }
        }
        private static void StartAsReplicant(bool LightNodeActive)
        {
            bool exitOuterLoop = false;
            while (exitOuterLoop == false)
            {
                try
                {
                    using (Notus.Validator.Replicant ReplicantObj = new Notus.Validator.Replicant())
                    {
                        ReplicantObj.LightNode = LightNodeActive;
                        ReplicantObj.Start();
                    }
                }
                catch (Exception err)
                {
                    NP.Danger(NVG.Settings, "Replicant Outer Error Text : " + err.Message);
                }
            }
        }
    }
}