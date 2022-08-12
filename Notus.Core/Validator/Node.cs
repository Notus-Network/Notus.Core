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
            Notus.Variable.Common.ClassSetting NodeSettings = new Notus.Variable.Common.ClassSetting();

            /*
            
            burada ntp zaman bilgisi çekilecek
            burada ntp zaman bilgisi çekilecek

            Console.WriteLine(
                JsonSerializer.Serialize(
                    Notus.Time.GetNtpTime()
                )
            );
            Console.WriteLine("Control-Point-Notus.Validator.Node.Start() -> Line 19");
            Console.ReadLine();
            */

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

            if (NodeSettings.DevelopmentNode == true)
            {
                NodeSettings.Network = Notus.Variable.Enum.NetworkType.DevNet;
                Notus.Validator.Node.Start(NodeSettings, LightNodeActive);
            }
            else
            {
                NodeSettings.Network = Notus.Variable.Enum.NetworkType.MainNet;
                Notus.Validator.Node.Start(NodeSettings, LightNodeActive);
            }
        }
        public static void Start(Notus.Variable.Common.ClassSetting NodeSettings, bool LightNodeActive)
        {
            if (NodeSettings.LocalNode == true)
            {
                Notus.Print.Info(NodeSettings, "LocalNode Activated");
            }
            Notus.IO.NodeFolderControl(NodeSettings.Network, NodeSettings.Layer);

            Notus.Print.Info(NodeSettings, "Activated DevNET for " + Notus.Variable.Constant.LayerText[NodeSettings.Layer]);
            NodeSettings = Notus.Toolbox.Network.IdentifyNodeType(NodeSettings, 5);
            switch (NodeSettings.NodeType)
            {
                // if IP and port node written in the code
                case Notus.Variable.Enum.NetworkNodeType.Main:
                    StartAsMain(NodeSettings);
                    break;

                // if node join the network
                case Notus.Variable.Enum.NetworkNodeType.Master:
                    //StartAsMaster(NodeSettings);
                    StartAsMain(NodeSettings);
                    //StartAsMaster(NodeSettings);
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
            bool exitOuterLoop = false;
            while (exitOuterLoop == false)
            {
                using (Notus.Validator.Main MainObj = new Notus.Validator.Main())
                {
                    MainObj.Settings = NodeSettings;
                    MainObj.Start();
                }

                Notus.Print.Basic(NodeSettings, "Sleep For 2.5 Seconds");
                Thread.Sleep(2500);
            }
        }
        private static void StartAsMain(Notus.Variable.Common.ClassSetting NodeSettings)
        {
            bool exitOuterLoop = false;
            while (exitOuterLoop == false)
            {
                using (Notus.Validator.Main MainObj = new Notus.Validator.Main())
                {
                    MainObj.Settings = NodeSettings;
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