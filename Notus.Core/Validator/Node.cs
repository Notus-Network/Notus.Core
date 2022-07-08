using System;
using System.Threading;

namespace Notus.Validator
{
    public static class Node
    {
        public static void Start(Notus.Variable.Common.ClassSetting NodeSettings, bool EmptyTimerActive, bool CryptoTimerActive, bool LightNodeActive)
        {
            if (NodeSettings.LocalNode == true)
            {
                Notus.Toolbox.Print.Info(true, "LocalNode Activated");
            }
            Notus.Toolbox.IO.NodeFolderControl(NodeSettings.Network, NodeSettings.Layer);

            if (NodeSettings.Network == Notus.Variable.Enum.NetworkType.DevNet)
            {
                if (NodeSettings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
                {
                    Notus.Toolbox.Print.Info(true, "Activated DevNET for Main Layer");
                }
                else
                {
                    Notus.Toolbox.Print.Info(true, "Activated DevNET for " + NodeSettings.Layer.ToString());
                }
            }
            else
            {
                if (NodeSettings.Network == Notus.Variable.Enum.NetworkType.TestNet)
                {
                    if (NodeSettings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
                    {
                        Notus.Toolbox.Print.Info(true, "Activated TestNET for Main Layer");
                    }
                    else
                    {
                        Notus.Toolbox.Print.Info(true, "Activated TestNET for " + NodeSettings.Layer.ToString());
                    }
                }
                else
                {
                    if (NodeSettings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
                    {
                        Notus.Toolbox.Print.Info(true, "Activated MainNET for Main Layer");
                    }
                    else
                    {
                        Notus.Toolbox.Print.Info(true, "Activated MainNET for " + NodeSettings.Layer.ToString());
                    }
                }
            }
            NodeSettings = Notus.Toolbox.Network.IdentifyNodeType(NodeSettings, 5);

            switch (NodeSettings.NodeType)
            {
                case Notus.Variable.Enum.NetworkNodeType.Main:
                    StartAsMain(NodeSettings, EmptyTimerActive, CryptoTimerActive);
                    break;

                case Notus.Variable.Enum.NetworkNodeType.Master:
                    StartAsMaster(NodeSettings);
                    break;

                case Notus.Variable.Enum.NetworkNodeType.Replicant:
                    StartAsReplicant(NodeSettings, LightNodeActive);
                    break;

                default:
                    break;
            }
            Notus.Toolbox.Print.Warning(true, "Task Ended");
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

                Notus.Toolbox.Print.Basic(NodeSettings.InfoMode, "Sleep For 2.5 Seconds");
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
                    Notus.Toolbox.Print.Danger(true, "Replicant Outer Error Text : " + err.Message);
                }
            }
        }
    }
}