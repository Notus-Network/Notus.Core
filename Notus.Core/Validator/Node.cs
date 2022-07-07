using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Notus.Validator
{
    public class Node : IDisposable
    {
        private bool LightNodeActive = false;
        private Notus.Variable.Common.ClassSetting NodeSettings = new Notus.Variable.Common.ClassSetting();
        private List<Notus.Variable.Struct.TaskListInfo> TaskList = new List<Notus.Variable.Struct.TaskListInfo>();
        public void Start(Notus.Variable.Common.ClassSetting nodeSetting)
        {
            if (nodeSetting.LocalNode == true)
            {
                Notus.Toolbox.Print.Info(true, "LocalNode Activated");
            }
            Notus.Toolbox.IO.NodeFolderControl(nodeSetting.Network, nodeSetting.Layer);

            if (nodeSetting.Network == Notus.Variable.Enum.NetworkType.DevNet)
            {
                if (nodeSetting.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
                {
                    Notus.Toolbox.Print.Info(true, "Activated DevNET for Main Layer");
                }
                else
                {
                    Notus.Toolbox.Print.Info(true, "Activated DevNET for " + nodeSetting.Layer.ToString());
                }
            }
            else
            {
                if (nodeSetting.Network == Notus.Variable.Enum.NetworkType.TestNet)
                {
                    if (nodeSetting.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
                    {
                        Notus.Toolbox.Print.Info(true, "Activated TestNET for Main Layer");
                    }
                    else
                    {
                        Notus.Toolbox.Print.Info(true, "Activated TestNET for " + nodeSetting.Layer.ToString());
                    }
                }
                else
                {
                    if (nodeSetting.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
                    {
                        Notus.Toolbox.Print.Info(true, "Activated MainNET for Main Layer");
                    }
                    else
                    {
                        Notus.Toolbox.Print.Info(true, "Activated MainNET for " + nodeSetting.Layer.ToString());
                    }
                }
            }
            nodeSetting = Notus.Toolbox.Network.IdentifyNodeType(nodeSetting, 5);

            switch (nodeSetting.NodeType)
            {
                case Notus.Variable.Enum.NetworkNodeType.Main:
                    StartAsMain(nodeSetting);
                    break;

                case Notus.Variable.Enum.NetworkNodeType.Master:
                    StartAsMaster(nodeSetting);
                    break;

                case Notus.Variable.Enum.NetworkNodeType.Replicant:
                    StartAsReplicant(nodeSetting);
                    break;

                default:
                    break;
            }
            Notus.Toolbox.Print.Warning(true, "Task Ended");
        }
        private void StartAsMaster(Notus.Variable.Common.ClassSetting nodeSetting)
        {

        }
        private void StartAsMain(Notus.Variable.Common.ClassSetting nodeSetting)
        {
            bool exitOuterLoop = false;
            while (exitOuterLoop == false)
            {
                using (Notus.Validator.Main MainObj = new Notus.Validator.Main())
                {
                    MainObj.Settings = nodeSetting;
                    MainObj.EmptyTimerActive = false;
                    MainObj.CryptoTimerActive = true;
                    MainObj.Start();
                }

                Notus.Toolbox.Print.Basic(NodeSettings.InfoMode, "Sleep For 2.5 Seconds");
                Thread.Sleep(2500);
            }
        }
        private void StartAsReplicant(Notus.Variable.Common.ClassSetting nodeSetting)
        {
            bool exitOuterLoop = false;
            while (exitOuterLoop == false)
            {
                try
                {
                    using (Notus.Validator.Replicant ReplicantObj = new Notus.Validator.Replicant())
                    {
                        ReplicantObj.Settings = nodeSetting;
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
        public void PreStart()
        {
            string nodeSettingStr = JsonSerializer.Serialize(NodeSettings);

            foreach (System.Collections.Generic.KeyValuePair<Notus.Variable.Enum.NetworkLayer, bool> entry in NodeSettings.ActiveLayer)
            {
                if (entry.Value == true)
                {
                    Console.WriteLine(entry.Key.ToString() + " starting");
                    if (NodeSettings.DevelopmentNode == true)
                    {
                        Notus.Variable.Struct.TaskListInfo tmpTaskInfo = new Notus.Variable.Struct.TaskListInfo() { };
                        tmpTaskInfo.TokenSourceObj = new CancellationTokenSource();
                        tmpTaskInfo.TokenObj = tmpTaskInfo.TokenSourceObj.Token;
                        tmpTaskInfo.TokenObj.Register(() =>
                            Console.WriteLine("ct1-cancel-callback")
                        );

                        tmpTaskInfo.SettingsObj = JsonSerializer.Deserialize<Notus.Variable.Common.ClassSetting>(nodeSettingStr);
                        tmpTaskInfo.SettingsObj.Layer=entry.Key;
                        tmpTaskInfo.SettingsObj.Network = Notus.Variable.Enum.NetworkType.DevNet;

                        tmpTaskInfo.TaskObj = Task.Run(() =>
                        {
                            Start(tmpTaskInfo.SettingsObj);

                        }, tmpTaskInfo.TokenObj);

                        TaskList.Add(tmpTaskInfo);
                    }
                    else
                    {

                    }
                }
            }
        }
        private void SubPrepare()
        {
            using (Notus.Validator.Menu menuObj = new Notus.Validator.Menu())
            {
                menuObj.Start();
                NodeSettings = menuObj.DefineMySetting(NodeSettings);
                Console.WriteLine(JsonSerializer.Serialize(NodeSettings, new JsonSerializerOptions() { WriteIndented = true }));
                Console.ReadLine();
                //Console.WriteLine("test-2");
            }
        }
        public void Prepare()
        {
            SubPrepare();
        }
        public void Prepare(string[] args)
        {
            if (args.Length > 0)
            {
                CheckParameter(args);
            }
            else
            {
                SubPrepare();
            }
        }
        private void CheckParameter(string[] args)
        {
            //NodeSettings.DebugMode = false;
            for (int a = 0; a < args.Length; a++)
            {
                if (string.Equals(args[a], "--testnet"))
                {
                    NodeSettings.Network = Notus.Variable.Enum.NetworkType.TestNet;
                }
                if (string.Equals(args[a], "--mainnet"))
                {
                    NodeSettings.Network = Notus.Variable.Enum.NetworkType.MainNet;
                }
                if (string.Equals(args[a], "--devnet"))
                {
                    NodeSettings.Network = Notus.Variable.Enum.NetworkType.DevNet;
                }


                if (string.Equals(args[a], "--light"))
                {
                    LightNodeActive = true;
                }


                if (string.Equals(args[a], "--replicant"))
                {
                    NodeSettings.NodeType = Notus.Variable.Enum.NetworkNodeType.Replicant;
                }
                if (string.Equals(args[a], "--main"))
                {
                    NodeSettings.NodeType = Notus.Variable.Enum.NetworkNodeType.Main;
                }
                if (string.Equals(args[a], "--master"))
                {
                    NodeSettings.NodeType = Notus.Variable.Enum.NetworkNodeType.Master;
                }


                if (string.Equals(args[a], "--debug"))
                {
                    NodeSettings.DebugMode = true;
                }
                if (string.Equals(args[a], "--info"))
                {
                    NodeSettings.InfoMode = true;
                }


                if (string.Equals(args[a], "--layer1"))
                {
                    NodeSettings.Layer = Notus.Variable.Enum.NetworkLayer.Layer1;
                }
                if (string.Equals(args[a], "--layer2"))
                {
                    NodeSettings.Layer = Notus.Variable.Enum.NetworkLayer.Layer2;
                }
                if (string.Equals(args[a], "--layer3"))
                {
                    NodeSettings.Layer = Notus.Variable.Enum.NetworkLayer.Layer3;
                }
                if (string.Equals(args[a], "--layer4"))
                {
                    NodeSettings.Layer = Notus.Variable.Enum.NetworkLayer.Layer4;
                }
                if (string.Equals(args[a], "--layer5"))
                {
                    NodeSettings.Layer = Notus.Variable.Enum.NetworkLayer.Layer5;
                }
                if (string.Equals(args[a], "--layer6"))
                {
                    NodeSettings.Layer = Notus.Variable.Enum.NetworkLayer.Layer6;
                }
                if (string.Equals(args[a], "--layer7"))
                {
                    NodeSettings.Layer = Notus.Variable.Enum.NetworkLayer.Layer7;
                }
                if (string.Equals(args[a], "--layer8"))
                {
                    NodeSettings.Layer = Notus.Variable.Enum.NetworkLayer.Layer8;
                }
                if (string.Equals(args[a], "--layer9"))
                {
                    NodeSettings.Layer = Notus.Variable.Enum.NetworkLayer.Layer9;
                }
                if (string.Equals(args[a], "--layer10"))
                {
                    NodeSettings.Layer = Notus.Variable.Enum.NetworkLayer.Layer10;
                }
            }
        }

        public Node()
        {
            const string Const_EncryptKey = "key-password-string";
            const bool Const_EncryptionActivated = false;

            NodeSettings = new Notus.Variable.Common.ClassSetting()
            {
                LocalNode = true,
                InfoMode = true,
                DebugMode = true,

                EncryptMode = Const_EncryptionActivated,
                HashSalt = Notus.Encryption.Toolbox.GenerateSalt(),
                EncryptKey = Const_EncryptKey,

                SynchronousSocketIsActive = true,
                Layer = Notus.Variable.Enum.NetworkLayer.Layer1,
                Network = Notus.Variable.Enum.NetworkType.MainNet,
                NodeType = Notus.Variable.Enum.NetworkNodeType.Suitable,

                NodeWallet = new Notus.Variable.Struct.EccKeyPair()
                {
                    CurveName = "",
                    PrivateKey = "",
                    PublicKey = "",
                    WalletKey = "",
                    Words = new string[] { },
                },
                PrettyJson = true,
                GenesisAssigned = false,

                WaitTickCount = 4,
                IpInfo = new Notus.Variable.Struct.NodeIpInfo()
                {
                    Local = "",
                    Public = ""
                },
                ActiveLayer = new System.Collections.Generic.Dictionary<Notus.Variable.Enum.NetworkLayer, bool>(),
                GenesisCreated = false,
            };
        }
        ~Node()
        {

        }
        public void Dispose()
        {

        }
    }
}