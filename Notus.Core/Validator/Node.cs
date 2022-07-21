using System;
using System.Text.Json;
using System.Threading;

namespace Notus.Validator
{
    public static class Node
    {
        public static void Start(string[] argsFromCLI)
        {
            bool LightNodeActive = false;
            bool EmptyTimerActive = false;
            bool CryptoTimerActive = true;
            Notus.Variable.Common.ClassSetting NodeSettings = new Notus.Variable.Common.ClassSetting();

            using (Notus.Validator.Menu menuObj = new Notus.Validator.Menu())
            {
                NodeSettings = menuObj.PreStart(argsFromCLI);
                menuObj.Start();
                NodeSettings = menuObj.DefineMySetting(NodeSettings);
            }
            if (NodeSettings.NodeType != Notus.Variable.Enum.NetworkNodeType.Replicant)
            {
                LightNodeActive = false;
            }

            //Console.WriteLine(JsonSerializer.Serialize(NodeSettings, new JsonSerializerOptions() { WriteIndented = true }));
            //Console.ReadLine();

            if (NodeSettings.DevelopmentNode == true)
            {
                NodeSettings.Network = Notus.Variable.Enum.NetworkType.DevNet;
                Notus.Validator.Node.Start(NodeSettings, EmptyTimerActive, CryptoTimerActive, LightNodeActive);
            }
            else
            {
                NodeSettings.Network = Notus.Variable.Enum.NetworkType.MainNet;
                Notus.Validator.Node.Start(NodeSettings, EmptyTimerActive, CryptoTimerActive, LightNodeActive);
            }
        }
        public static void Start(Notus.Variable.Common.ClassSetting NodeSettings, bool EmptyTimerActive, bool CryptoTimerActive, bool LightNodeActive)
        {
            if (NodeSettings.LocalNode == true)
            {
                Notus.Print.Info(NodeSettings.InfoMode, "LocalNode Activated");
            }
            Notus.IO.NodeFolderControl(NodeSettings.Network, NodeSettings.Layer);

            Notus.Print.Info(NodeSettings.InfoMode, "Activated DevNET for " + Notus.Variable.Constant.LayerText[NodeSettings.Layer]);
            NodeSettings = Notus.Toolbox.Network.IdentifyNodeType(NodeSettings, 5);
            switch (NodeSettings.NodeType)
            {
                // if IP and port node written in the code
                case Notus.Variable.Enum.NetworkNodeType.Main:
                    StartAsMain(NodeSettings, EmptyTimerActive, CryptoTimerActive);
                    break;

                // if node join the network
                case Notus.Variable.Enum.NetworkNodeType.Master:
                    StartAsMaster(NodeSettings);
                    break;

                // if node only store the data
                case Notus.Variable.Enum.NetworkNodeType.Replicant:
                    StartAsReplicant(NodeSettings, LightNodeActive);
                    break;

                default:
                    break;
            }
            Notus.Print.Warning(NodeSettings, "Task Ended");
        }
        private static void StartAsMaster(Notus.Variable.Common.ClassSetting NodeSettings)
        {

        }
        private static void StartAsMain(Notus.Variable.Common.ClassSetting NodeSettings, bool EmptyTimerActive, bool CryptoTimerActive)
        {
            bool exitOuterLoop = false;
            while (exitOuterLoop == false)
            {
                using (Notus.Validator.Main MainObj = new Notus.Validator.Main())
                {
                    MainObj.Settings = NodeSettings;
                    MainObj.EmptyTimerActive = EmptyTimerActive;
                    MainObj.CryptoTimerActive = CryptoTimerActive;
                    MainObj.Start();
                }

                Notus.Print.Basic(NodeSettings, "Sleep For 2.5 Seconds");
                Thread.Sleep(2500);
            }
        }
        private static void StartAsReplicant(Notus.Variable.Common.ClassSetting NodeSettings, bool LightNodeActive)
        {
            bool exitOuterLoop = false;
            while (exitOuterLoop == false)
            {
                try
                {
                    using (Notus.Validator.Replicant ReplicantObj = new Notus.Validator.Replicant())
                    {
                        ReplicantObj.Settings = NodeSettings;
                        ReplicantObj.LightNode = LightNodeActive;
                        ReplicantObj.Start();
                    }
                }
                catch (Exception err)
                {
                    Notus.Print.Danger(NodeSettings, "Replicant Outer Error Text : " + err.Message);
                }
            }
        }
    }
}