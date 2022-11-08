using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using NGF = Notus.Variable.Globals.Functions;
using NVG = Notus.Variable.Globals;
using NVC = Notus.Variable.Constant;
using NVE = Notus.Variable.Enum;
using NVS = Notus.Variable.Struct;
namespace Notus.Validator
{
    public class Menu : IDisposable
    {
        private NVS.NodeInfoForMenu nodeObj;

        private bool Node_WalletDefined = false;
        private string Node_WalletKey = string.Empty;

        private int indexMainMenu = 0;
        private Notus.Mempool MP_NodeList;
        private int longestLayerText = 0;
        private string ChunkString(string str, int chunkSize, string separator, int newLineAfterHowManyChunk = 0, int EmptySpaceAfterNewLine = 0)
        {
            var b = new StringBuilder();
            int newLineChunkSize = 0;
            var stringLength = str.Length;
            for (var i = 0; i < stringLength; i += chunkSize)
            {
                if (i + chunkSize > stringLength)
                {
                    chunkSize = stringLength - i;
                }
                b.Append(str.Substring(i, chunkSize));
                newLineChunkSize++;
                if (newLineAfterHowManyChunk > 0)
                {
                    if (newLineChunkSize == newLineAfterHowManyChunk)
                    {
                        b.Append(Environment.NewLine);
                        if (EmptySpaceAfterNewLine > 0)
                        {
                            b.Append((" ").PadRight(EmptySpaceAfterNewLine - 1, ' '));
                        }
                        newLineChunkSize = 0;
                    }
                    else
                    {
                        if (i + chunkSize != stringLength)
                        {
                            b.Append(separator);
                        }
                    }
                }
                else
                {
                    if (i + chunkSize != stringLength)
                    {
                        b.Append(separator);
                    }
                }
            }
            return b.ToString();
        }

        private void walletMenu_GenerateKey()
        {
            bool exitWalletLoop = false;
            while (exitWalletLoop == false)
            {
                NVS.EccKeyPair newWalletKey = Notus.Wallet.ID.GenerateKeyPair();
                Console.Clear();
                Console.WriteLine("Generated wallet Id");
                Console.WriteLine("-------------------");
                Console.WriteLine("Private Key : " + ChunkString(newWalletKey.PrivateKey, 16, " ", 4, 15));
                Console.WriteLine("Public Key  : " + ChunkString(newWalletKey.PublicKey, 16, " ", 4, 15));
                Console.WriteLine("Wallet Key  : " + newWalletKey.WalletKey);
                Console.WriteLine();
                Console.WriteLine("(u) use this wallet Id");
                Console.WriteLine("(g) generate new wallet key");
                Console.WriteLine("(m) go to menu");

                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Warning : Please note your private key before using");
                Console.ResetColor();

                bool innerWalletLoop = false;
                while (innerWalletLoop == false)
                {
                    ConsoleKeyInfo gwKey = Console.ReadKey();
                    if (gwKey.Key == ConsoleKey.G)
                    {
                        innerWalletLoop = true;
                    }
                    if (gwKey.Key == ConsoleKey.M)
                    {
                        innerWalletLoop = true;
                        exitWalletLoop = true;
                        Console.Clear();
                    }
                    if (gwKey.Key == ConsoleKey.U)
                    {
                        //store public key to DB for every Network
                        //store public key to DB for every Network
                        //store public key to DB for every Network
                        //store public key to DB for every Network
                        //store public key to DB for every Network
                        MP_NodeList.Set("Node_WalletKey", newWalletKey.WalletKey, true);
                        Node_WalletDefined = true;
                        Node_WalletKey = newWalletKey.WalletKey;
                        nodeObj.Wallet.Key = Node_WalletKey;
                        nodeObj.Wallet.Defined = true;

                        innerWalletLoop = true;
                        exitWalletLoop = true;
                        Console.Clear();
                    }
                }
            }
        }
        private void walletMenu_DeleteKey()
        {
            Console.Clear();
            MP_NodeList.Set("Node_WalletKey", "", true);
            Node_WalletDefined = false;
            Node_WalletKey = string.Empty;
            nodeObj.Wallet.Key = string.Empty;
            nodeObj.Wallet.Defined = false;

            Console.Clear();
            Console.WriteLine("Your Wallet ID has been deleted.");
            Thread.Sleep(2500);
            Console.Clear();
        }
        private bool walletMenu_DefineKey()
        {
            bool tmpWalletDefined = false;
            Console.CursorVisible = true;
            bool tmpExitWhileLoop = false;
            string userDefineWalletKey = string.Empty;
            while (tmpExitWhileLoop == false)
            {
                if (userDefineWalletKey == string.Empty)
                {
                    Console.Write("Validator Key  : ");
                }
                userDefineWalletKey = Console.ReadLine();
                tmpExitWhileLoop = true;
                userDefineWalletKey = userDefineWalletKey.Trim();
                if (userDefineWalletKey.Length != NVC.SingleWalletTextLength)
                {
                    userDefineWalletKey = string.Empty;
                    Console.WriteLine();
                    Console.Write("Too short");
                    Thread.Sleep(1500);
                }
            }
            Console.Clear();
            if (userDefineWalletKey != string.Empty)
            {
                MP_NodeList.Set("Node_WalletKey", userDefineWalletKey, true);
                Node_WalletDefined = true;
                tmpWalletDefined = true;
                Node_WalletKey = userDefineWalletKey;

                nodeObj.Wallet.Key = Node_WalletKey;
                nodeObj.Wallet.Defined = true;
            }
            Console.CursorVisible = false;
            return tmpWalletDefined;
        }
        private void walletMenu()
        {
            int prevMenuIndexNo = indexMainMenu;
            indexMainMenu = 0;
            Console.Clear();
            bool exitFromSubMenuLoop = false;
            while (exitFromSubMenuLoop == false)
            {
                int subMenuResult = drawMainMenu_WalletMenu(new List<string>()
                        {
                        "Generate New Key",
                        "Define Your Key",
                        "Delete Your Key",
                        "Go Back"
                        });
                if (subMenuResult == 3) // go back
                {
                    exitFromSubMenuLoop = true;
                }
                if (subMenuResult == 2) // delete your key
                {
                    walletMenu_DeleteKey();
                }
                if (subMenuResult == 0) // generate key
                {
                    walletMenu_GenerateKey();
                }
                if (subMenuResult == 1) // define key
                {
                    if (walletMenu_DefineKey())
                    {
                        exitFromSubMenuLoop = true;
                    }
                }
            }
            indexMainMenu = prevMenuIndexNo;
            Console.Clear();
        }
        private string SameLengthStr(string text, int textLen, char padText = ' ')
        {
            return text.PadRight(textLen, padText);
        }
        private void showMySettings_Obj()
        {
            Console.WriteLine(SameLengthStr("", longestLayerText + 10, '-'));
            Console.Write(SameLengthStr(NVC.LayerText[nodeObj.Layer.Selected], longestLayerText) + " : ");

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("enable");
            Console.ResetColor();
            if (nodeObj.DevelopmentMode == true)
            {
                Console.WriteLine(SameLengthStr("Dev Net Port Number", longestLayerText) + " : " + nodeObj.Layer.Port.DevNet.ToString());
            }
            else
            {
                Console.WriteLine(SameLengthStr("Main Net Port Number", longestLayerText) + " : " + nodeObj.Layer.Port.MainNet.ToString());
                Console.WriteLine(SameLengthStr("Test Net Port Number", longestLayerText) + " : " + nodeObj.Layer.Port.TestNet.ToString());
            }
        }
        private void showMySettings_Str(string optionText, bool boolObj)
        {
            Console.WriteLine(SameLengthStr("", longestLayerText + 10, '-'));
            Console.Write(SameLengthStr(optionText, longestLayerText) + " : ");
            if (boolObj == true)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("enable");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("disable");
                Console.ResetColor();
            }
        }
        private bool showSettings(bool escapePressed)
        {
            Console.Clear();
            Console.CursorVisible = false;
            PrintWalletKey_AllMenu();

            showMySettings_Obj();
            showMySettings_Str("Debug Mode", nodeObj.DebugMode);
            showMySettings_Str("Info Mode", nodeObj.InfoMode);
            showMySettings_Str("Run Local Mode", nodeObj.LocalMode);
            showMySettings_Str("Development Mode", nodeObj.DevelopmentMode);


            Console.WriteLine();
            if (escapePressed == false)
            {
                Console.WriteLine("Press any to continue");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("Press ESC for Menu");
                return true;
            }
            Console.Clear();
            return false;
        }
        private void nodeTypeMenu()
        {
            bool exitFromSubMenuLoop = false;
            int tmpMenuIndexNo = indexMainMenu;
            indexMainMenu = 0;
            while (exitFromSubMenuLoop == false)
            {
                List<string> menuList = new List<string>() { };
                foreach (KeyValuePair<NVE.NetworkLayer, string> entry in NVC.LayerText)
                {
                    menuList.Add(entry.Value);
                }
                menuList.Add("Go Back");
                if (drawMainMenu_NodeType(menuList) == true)
                {
                    setLayerStatus();
                    resetPortMenu(false);
                    exitFromSubMenuLoop = true;
                }
            }
            indexMainMenu = tmpMenuIndexNo;
            Console.Clear();
        }
        private bool drawMainMenu_DebugInfo(List<string> items)
        {
            Console.Clear();
            PrintWalletKey_AllMenu();
            Console.CursorVisible = false;
            const int TextLong = 25;
            for (int i = 0; i < items.Count; i++)
            {
                string mResultStr = items[i].PadRight(TextLong, ' ');
                string scrnText = "    ";
                if (i == indexMainMenu)
                {
                    Console.BackgroundColor = ConsoleColor.Gray;
                    Console.ForegroundColor = ConsoleColor.Black;
                    scrnText = " >> ";
                }
                if (i == 0)
                {
                    scrnText = scrnText + "[ " + (nodeObj.DebugMode == true ? "X" : " ") + " ] ";
                }
                if (i == 1)
                {
                    scrnText = scrnText + "[ " + (nodeObj.InfoMode == true ? "X" : " ") + " ] ";
                }
                if (i == 2)
                {
                    scrnText = scrnText + "[ " + (nodeObj.LocalMode == true ? "X" : " ") + " ] ";
                }
                if (i == 3)
                {
                    scrnText = scrnText + "[ " + (nodeObj.DevelopmentMode == true ? "X" : " ") + " ] ";
                }
                scrnText = scrnText + mResultStr;
                Console.WriteLine(scrnText);
                Console.ResetColor();
            }
            ConsoleKeyInfo ckey = Console.ReadKey();
            if (ckey.Key == ConsoleKey.DownArrow)
            {
                if (indexMainMenu == items.Count - 1)
                {
                    indexMainMenu = 0;
                }
                else
                {
                    indexMainMenu++;
                }
            }
            else if (ckey.Key == ConsoleKey.UpArrow)
            {
                if (indexMainMenu <= 0)
                {
                    indexMainMenu = items.Count - 1;
                }
                else
                {
                    indexMainMenu--;
                }
            }
            else if (ckey.Key == ConsoleKey.LeftArrow)
            {
                Console.Clear();
            }
            else if (ckey.Key == ConsoleKey.RightArrow)
            {
                Console.Clear();
            }
            else if (ckey.Key == ConsoleKey.Spacebar)
            {
                if (indexMainMenu == 0)
                    nodeObj.DebugMode = !nodeObj.DebugMode;
                if (indexMainMenu == 1)
                    nodeObj.InfoMode = !nodeObj.InfoMode;
                if (indexMainMenu == 2)
                    nodeObj.LocalMode = !nodeObj.LocalMode;
                if (indexMainMenu == 3)
                    nodeObj.DevelopmentMode = !nodeObj.DevelopmentMode;
            }
            else if (ckey.Key == ConsoleKey.Enter)
            {
                if (indexMainMenu == 0)
                    nodeObj.DebugMode = !nodeObj.DebugMode;
                if (indexMainMenu == 1)
                    nodeObj.InfoMode = !nodeObj.InfoMode;
                if (indexMainMenu == 2)
                    nodeObj.LocalMode = !nodeObj.LocalMode;
                if (indexMainMenu == 3)
                    nodeObj.DevelopmentMode = !nodeObj.DevelopmentMode;
                return true;
            }

            Console.Clear();
            return false;
        }
        private void debugInfoMenu()
        {
            bool exitFromSubMenuLoop = false;
            int tmpMenuIndexNo = indexMainMenu;
            indexMainMenu = 0;
            while (exitFromSubMenuLoop == false)
            {
                List<string> menuList = new List<string>() {
                    "Debug Mode" ,
                    "Info Mode",
                    "Run Local Mode",
                    "Only Development Mode",
                    "Go Back"
                };

                if (drawMainMenu_DebugInfo(menuList) == true)
                {
                    MP_NodeList.Set("Node_DebugMode", nodeObj.DebugMode == true ? "1" : "0", true);
                    MP_NodeList.Set("Node_InfoMode", nodeObj.InfoMode == true ? "1" : "0", true);
                    MP_NodeList.Set("Node_LocalMode", nodeObj.LocalMode == true ? "1" : "0", true);
                    MP_NodeList.Set("Node_DevelopmentMode", nodeObj.DevelopmentMode == true ? "1" : "0", true);
                    exitFromSubMenuLoop = true;
                }
            }
            indexMainMenu = tmpMenuIndexNo;
            Console.Clear();
        }

        private bool GetLayerPortNumber(NVE.NetworkLayer layerObj)
        {
            Console.CursorVisible = true;
            if (nodeObj.DevelopmentMode == true)
            {
                Console.Write("Developetment Net Port Number : ");
                string okunan = Console.ReadLine();
                if (int.TryParse(okunan, out int tmpDevPortNo))
                {
                    if (tmpDevPortNo > 0 && tmpDevPortNo < 65536)
                    {
                        nodeObj.Layer.Port.DevNet = tmpDevPortNo;
                        nodeObj.Layer.Port.MainNet = 0;
                        nodeObj.Layer.Port.TestNet = 0;
                        setLayerStatus();
                        Console.CursorVisible = false;
                        Console.WriteLine("Port Numbers Saved");
                        Thread.Sleep(2500);
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Wrong port value");
                    }
                }
                else
                {
                    Console.WriteLine("Wrong port value");
                }
            }
            else
            {
                Console.Write("Main Net Port Number : ");
                string okunan = Console.ReadLine();
                if (int.TryParse(okunan, out int tmpMainPortNo))
                {
                    if (tmpMainPortNo > 0 && tmpMainPortNo < 65536)
                    {
                        Console.Write("Test Net Port Number : ");
                        string okunan2 = Console.ReadLine();
                        if (int.TryParse(okunan2, out int tmpTestPortNo))
                        {
                            if (tmpTestPortNo > 0 && tmpTestPortNo < 65536)
                            {
                                nodeObj.Layer.Port.DevNet = 0;
                                nodeObj.Layer.Port.MainNet = tmpMainPortNo;
                                nodeObj.Layer.Port.TestNet = tmpTestPortNo;
                                setLayerStatus();
                                Console.CursorVisible = false;
                                Console.WriteLine("Port Numbers Saved");
                                Thread.Sleep(2500);
                                return true;
                            }
                            else
                            {
                                Console.WriteLine("Wrong port value");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Wrong port value");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Wrong port value");
                    }
                }
                else
                {
                    Console.WriteLine("Wrong port value");
                }
            }

            Console.CursorVisible = false;
            Thread.Sleep(2500);
            return false;
        }
        private int drawMainMenu_NodePortNo(Dictionary<int, string> items)
        {
            Console.Clear();
            PrintWalletKey_AllMenu();
            Console.CursorVisible = false;
            const int TextLong = 40;
            foreach (KeyValuePair<int, string> entry in items)
            {
                string mResultStr = entry.Value.PadRight(TextLong, ' ');
                string scrnText = "    ";
                if (entry.Key == indexMainMenu)
                {
                    Console.BackgroundColor = ConsoleColor.Gray;
                    Console.ForegroundColor = ConsoleColor.Black;
                    scrnText = " >> ";
                }
                scrnText = scrnText + mResultStr;

                Console.WriteLine(scrnText);
                Console.ResetColor();
            }

            ConsoleKeyInfo ckey = Console.ReadKey();
            if (ckey.Key == ConsoleKey.DownArrow)
            {
                if (indexMainMenu == items.Count - 1)
                {
                    indexMainMenu = 0;
                }
                else
                {
                    indexMainMenu++;
                }
            }
            else if (ckey.Key == ConsoleKey.UpArrow)
            {
                if (indexMainMenu <= 0)
                {
                    indexMainMenu = items.Count - 1;
                }
                else
                {
                    indexMainMenu--;
                }
            }
            else if (ckey.Key == ConsoleKey.LeftArrow)
            {
                Console.Clear();
            }
            else if (ckey.Key == ConsoleKey.RightArrow)
            {
                Console.Clear();
            }
            else if (ckey.Key == ConsoleKey.Enter)
            {
                if (indexMainMenu == 0)
                {
                    return 0;
                }
                if (indexMainMenu == 1) // layer 1'in portlarını seç
                {
                    GetLayerPortNumber(NVE.NetworkLayer.Layer1);
                    return 9;
                }
                if (indexMainMenu == 2) // layer 2'nin portlarını seç
                {
                    GetLayerPortNumber(NVE.NetworkLayer.Layer2);
                    return 9;
                }
                if (indexMainMenu == 3) // layer 3'ün portlarını seç
                {
                    GetLayerPortNumber(NVE.NetworkLayer.Layer3);
                    return 9;
                }
                if (indexMainMenu == 4) // layer 4'ün portlarını seç
                {
                    GetLayerPortNumber(NVE.NetworkLayer.Layer4);
                    return 9;
                }
            }
            Console.Clear();
            return 9;
        }

        private void resetPortMenu(bool callFromMenu)
        {
            if (callFromMenu == true)
            {
                Console.Clear();
            }
            Dictionary<NVE.NetworkType, int> tmpPortValue = NVC.PortNo[NVE.NetworkLayer.Layer1];

            if (NVE.NetworkLayer.Layer2 == nodeObj.Layer.Selected)
            {
                tmpPortValue = NVC.PortNo[NVE.NetworkLayer.Layer2];
            }
            if (NVE.NetworkLayer.Layer3 == nodeObj.Layer.Selected)
            {
                tmpPortValue = NVC.PortNo[NVE.NetworkLayer.Layer2];
            }
            if (NVE.NetworkLayer.Layer4 == nodeObj.Layer.Selected)
            {
                tmpPortValue = NVC.PortNo[NVE.NetworkLayer.Layer2];
            }
            nodeObj.Layer.Port.DevNet = tmpPortValue[NVE.NetworkType.DevNet];
            nodeObj.Layer.Port.MainNet = tmpPortValue[NVE.NetworkType.MainNet];
            nodeObj.Layer.Port.TestNet = tmpPortValue[NVE.NetworkType.TestNet];

            setLayerStatus();

            if (callFromMenu == true)
            {
                Console.WriteLine("Layer Ports have been reset");
                Thread.Sleep(3000);
                Console.Clear();
            }
        }
        private void nodePortMenu()
        {
            Dictionary<int, string> PortList = new Dictionary<int, string>();
            if (nodeObj.Layer.Selected == NVE.NetworkLayer.Layer1)
            {
                PortList.Add(1, NVC.LayerText[NVE.NetworkLayer.Layer1]);
            }
            if (nodeObj.Layer.Selected == NVE.NetworkLayer.Layer2)
            {
                PortList.Add(2, NVC.LayerText[NVE.NetworkLayer.Layer2]);
            }
            if (nodeObj.Layer.Selected == NVE.NetworkLayer.Layer3)
            {
                PortList.Add(3, NVC.LayerText[NVE.NetworkLayer.Layer3]);
            }
            if (nodeObj.Layer.Selected == NVE.NetworkLayer.Layer4)
            {
                PortList.Add(4, NVC.LayerText[NVE.NetworkLayer.Layer4]);
            }
            if (PortList.Count == 0)
            {
                Console.Clear();
                Console.WriteLine("Please select which type of node you want to activate");
                Thread.Sleep(2500);
                Console.Clear();
                return;
            }
            PortList.Add(0, "Go Back");

            int tmpMenuIndexNo = indexMainMenu;
            indexMainMenu = 1;
            bool exitWhileLoop = false;
            while (exitWhileLoop == false)
            {
                int menuVal = drawMainMenu_NodePortNo(PortList);
                if (menuVal == 0)
                {
                    exitWhileLoop = true;
                }
            }
            setLayerStatus();
            indexMainMenu = tmpMenuIndexNo;
            Console.Clear();
        }
        private void mainMenu()
        {
            bool useTimerForStart = false;
            if (Node_WalletDefined == true)
            {
                useTimerForStart = true;
            }

            Console.Clear();
            bool startNodeSelected = false;
            while (startNodeSelected == false)
            {
                int selectedMenuItem = drawMainMenu_MainMenu(new List<string>()
                {
                    "Start Node",
                    "Node Type",
                    "Change Ports",
                    "Reset Ports",
                    "Change Wallet Key",
                    "Preferences",
                    "Show My Settings",
                    "Exit"
                }, useTimerForStart);

                useTimerForStart = false;

                if (selectedMenuItem == 0) //"Start Node"
                {
                    if (Node_WalletDefined == true)
                    {
                        startNodeSelected = true;
                    }
                    else
                    {
                        Console.Clear();
                        Console.WriteLine("Wallet Key Not Defined");
                        Thread.Sleep(1000);
                        Console.Clear();
                    }
                } // "Start Node"

                if (selectedMenuItem == 1) // "Node Type"
                {
                    nodeTypeMenu();
                } // else if  "Node Type"

                if (selectedMenuItem == 2) // "Change Ports"
                {
                    nodePortMenu();
                } // else if "Change Ports",

                if (selectedMenuItem == 3) // "reset ports"
                {
                    resetPortMenu(true);
                } // else if "reset ports",

                if (selectedMenuItem == 4) // "Change Wallet Key"
                {
                    walletMenu();
                } // else if "Change Wallet Key"

                if (selectedMenuItem == 5) // "Debug / Info Mode"
                {
                    debugInfoMenu();
                }
                if (selectedMenuItem == 6) // show my settings
                {
                    showSettings(false);
                }
                if (selectedMenuItem == 7) // exit
                {
                    //Dispose();
                    Environment.Exit(0);
                    while (true) { }
                }
            }
        }
        private int drawMainMenu_MainMenu(List<string> items, bool useTimerForStart)
        {
            PrintWalletKey_AllMenu();
            Console.CursorVisible = false;

            const int TextLong = 25;
            for (int i = 0; i < items.Count; i++)
            {
                string mResultStr = items[i].PadRight(TextLong, ' ');
                string scrnText = "    ";
                if (i == indexMainMenu)
                {
                    Console.BackgroundColor = ConsoleColor.Gray;
                    Console.ForegroundColor = ConsoleColor.Black;
                    scrnText = " >> ";
                }
                scrnText = scrnText + "[ " + i.ToString().PadLeft(2, '0') + " ] ";
                scrnText = scrnText + mResultStr;

                Console.WriteLine(scrnText);
                Console.ResetColor();
            }
            if (useTimerForStart == true)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("Press any key for setup");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Please wait for auto-start");
                bool keyExist = false;
                byte iCounter = 0;
                DateTime bitis = DateTime.Now.AddSeconds(10);
                while (bitis > DateTime.Now && keyExist == false)
                {
                    if (Console.KeyAvailable == true)
                    {
                        var readKeyObj = Console.ReadKey().Key;
                        if (readKeyObj == ConsoleKey.UpArrow || readKeyObj == ConsoleKey.DownArrow)
                        {
                            Console.Clear();
                        }
                        if (readKeyObj == ConsoleKey.Enter)
                        {
                            return 0;
                        }
                        keyExist = true;
                    }
                    else
                    {
                        iCounter++;
                        Thread.Sleep(10);
                        if (iCounter > 25)
                        {
                            Console.Write(".");
                            iCounter = 0;
                        }
                    }
                }

                //if not pressed key, start node app
                if (keyExist == false)
                {
                    Console.Clear();
                    showSettings(true);
                    Console.ResetColor();
                    keyExist = false;
                    iCounter = 0;
                    bitis = DateTime.Now.AddSeconds(5);
                    while (bitis > DateTime.Now && keyExist == false)
                    {
                        if (Console.KeyAvailable == true)
                        {
                            if (Console.ReadKey().Key == ConsoleKey.Escape)
                            {
                                keyExist = true;
                            }
                        }
                        else
                        {
                            iCounter++;
                            Thread.Sleep(5);
                            if (iCounter > 25)
                            {
                                Console.Write(".");
                                iCounter = 0;
                            }
                        }
                    }
                    Console.Clear();
                    if (keyExist == true)
                    {
                        return 100;
                    }
                    return 0;
                }

                return 100;
            }
            ConsoleKeyInfo ckey = Console.ReadKey();
            if (ckey.Key == ConsoleKey.DownArrow)
            {
                if (indexMainMenu == items.Count - 1)
                {
                    indexMainMenu = 0;
                }
                else
                {
                    indexMainMenu++;
                }
            }
            else if (ckey.Key == ConsoleKey.UpArrow)
            {
                if (indexMainMenu <= 0)
                {
                    indexMainMenu = items.Count - 1;
                }
                else
                {
                    indexMainMenu--;
                }
            }
            else if (ckey.Key == ConsoleKey.LeftArrow)
            {
                Console.Clear();
            }
            else if (ckey.Key == ConsoleKey.RightArrow)
            {
                Console.Clear();
            }
            else if (ckey.Key == ConsoleKey.Enter)
            {
                return indexMainMenu;
            }

            Console.Clear();
            return -1;
        }


        private bool drawMainMenu_NodeType(List<string> items)
        {
            Console.Clear();
            PrintWalletKey_AllMenu();
            Console.CursorVisible = false;

            const int TextLong = 25;
            for (int i = 0; i < items.Count; i++)
            {
                string mResultStr = items[i].PadRight(TextLong, ' ');
                string scrnText = "    ";
                if (i == indexMainMenu)
                {
                    Console.BackgroundColor = ConsoleColor.Gray;
                    Console.ForegroundColor = ConsoleColor.Black;
                    scrnText = " >> ";
                }
                if (i == 0)
                {
                    scrnText = scrnText + "[ " + (nodeObj.Layer.Selected == NVE.NetworkLayer.Layer1 ? "O" : " ") + " ] ";
                }
                if (i == 1)
                {
                    scrnText = scrnText + "[ " + (nodeObj.Layer.Selected == NVE.NetworkLayer.Layer2 ? "O" : " ") + " ] ";
                }
                if (i == 2)
                {
                    scrnText = scrnText + "[ " + (nodeObj.Layer.Selected == NVE.NetworkLayer.Layer3 ? "O" : " ") + " ] ";
                }
                if (i == 3)
                {
                    scrnText = scrnText + "[ " + (nodeObj.Layer.Selected == NVE.NetworkLayer.Layer4 ? "O" : " ") + " ] ";
                }
                scrnText = scrnText + mResultStr;

                Console.WriteLine(scrnText);
                Console.ResetColor();
            }
            ConsoleKeyInfo ckey = Console.ReadKey();
            if (ckey.Key == ConsoleKey.DownArrow)
            {
                if (indexMainMenu == items.Count - 1)
                {
                    indexMainMenu = 0;
                }
                else
                {
                    indexMainMenu++;
                }
            }
            else if (ckey.Key == ConsoleKey.UpArrow)
            {
                if (indexMainMenu <= 0)
                {
                    indexMainMenu = items.Count - 1;
                }
                else
                {
                    indexMainMenu--;
                }
            }
            else if (ckey.Key == ConsoleKey.LeftArrow)
            {
                Console.Clear();
            }
            else if (ckey.Key == ConsoleKey.RightArrow)
            {
                Console.Clear();
            }
            else if (ckey.Key == ConsoleKey.Spacebar)
            {
                drawMainMenu_NodeType_ChangeLayerStatus(indexMainMenu);
            }
            else if (ckey.Key == ConsoleKey.Enter)
            {
                drawMainMenu_NodeType_ChangeLayerStatus(indexMainMenu);
                if (indexMainMenu == 4)
                    return true;
            }

            Console.Clear();
            return false;
        }

        private void drawMainMenu_NodeType_ChangeLayerStatus(int indexMainMenu)
        {
            if (indexMainMenu == 0)
                nodeObj.Layer.Selected = NVE.NetworkLayer.Layer1;
            if (indexMainMenu == 1)
                nodeObj.Layer.Selected = NVE.NetworkLayer.Layer2;
            if (indexMainMenu == 2)
                nodeObj.Layer.Selected = NVE.NetworkLayer.Layer3;
            if (indexMainMenu == 3)
                nodeObj.Layer.Selected = NVE.NetworkLayer.Layer4;

            if (indexMainMenu >= 0 && indexMainMenu < 4)
            {
                resetPortMenu(false);
            }
        }
        private int drawMainMenu_WalletMenu(List<string> items)
        {
            PrintWalletKey_AllMenu();
            Console.CursorVisible = false;

            const int TextLong = 25;
            for (int i = 0; i < items.Count; i++)
            {
                string mResultStr = items[i].PadRight(TextLong, ' ');
                string scrnText = "    ";
                if (i == indexMainMenu)
                {
                    Console.BackgroundColor = ConsoleColor.Gray;
                    Console.ForegroundColor = ConsoleColor.Black;
                    scrnText = " >> ";
                }
                scrnText = scrnText + "[ " + i.ToString().PadLeft(2, '0') + " ] ";
                scrnText = scrnText + mResultStr;

                Console.WriteLine(scrnText);
                Console.ResetColor();
            }
            ConsoleKeyInfo ckey = Console.ReadKey();
            if (ckey.Key == ConsoleKey.DownArrow)
            {
                if (indexMainMenu == items.Count - 1)
                {
                    indexMainMenu = 0;
                }
                else
                {
                    indexMainMenu++;
                }
            }
            else if (ckey.Key == ConsoleKey.UpArrow)
            {
                if (indexMainMenu <= 0)
                {
                    indexMainMenu = items.Count - 1;
                }
                else
                {
                    indexMainMenu--;
                }
            }
            else if (ckey.Key == ConsoleKey.LeftArrow)
            {
                Console.Clear();
            }
            else if (ckey.Key == ConsoleKey.RightArrow)
            {
                Console.Clear();
            }
            else if (ckey.Key == ConsoleKey.Enter)
            {
                return indexMainMenu;
            }

            Console.Clear();
            return -1;
        }


        private void setLayerStatus()
        {
            MP_NodeList.Set("Node_Layer", JsonSerializer.Serialize(nodeObj.Layer), true);
        }
        private void checkLayerStatus(NVE.NetworkLayer layerObj)
        {
            NVS.LayerInfo tmpLayerObj = new NVS.LayerInfo()
            {
                Selected = NVE.NetworkLayer.Layer1,
                Port = new NVS.CommunicationPorts()
                {
                    DevNet = 0,
                    MainNet = 0,
                    TestNet = 0
                }
            };
            string tmpNodeType = MP_NodeList.Get("Node_Layer", "");
            if (tmpNodeType != "")
            {
                try
                {
                    nodeObj.Layer = JsonSerializer.Deserialize<NVS.LayerInfo>(tmpNodeType);
                }
                catch (Exception err)
                {
                    Notus.Print.Log(
                        NVE.LogLevel.Info,
                        9805000,
                        err.Message,
                        "BlockRowNo",
                        null,
                        err
                    );
                    nodeObj.Layer = tmpLayerObj;
                }
            }
            else
            {
                nodeObj.Layer = tmpLayerObj;
            }
        }
        public void DefineMySetting()
        {
            NVG.Settings.Nodes.My.Begin = NVG.NOW.Int;
            NVG.Settings.Nodes.My.Tick = NVG.Settings.Nodes.My.Begin;

            NVG.Settings.Layer = nodeObj.Layer.Selected;
            NVG.Settings.DebugMode = nodeObj.DebugMode;
            NVG.Settings.InfoMode = nodeObj.InfoMode;
            NVG.Settings.LocalNode = nodeObj.LocalMode;
            NVG.Settings.DevelopmentNode = nodeObj.DevelopmentMode;
            if (NVG.Settings.DevelopmentNode == true)
            {
                NVG.Settings.Network = NVE.NetworkType.DevNet;
                NVG.Settings.NodeWallet.WalletKey = NVC.SingleWalletPrefix_DevelopmentNetwork + nodeObj.Wallet.Key.Substring(NVC.SingleWalletPrefix_DevelopmentNetwork.Length);
            }
            NVG.Settings.Port = nodeObj.Layer.Port;
            NVG.Settings.EncryptKey = new Notus.Hash().CommonHash("sha512", NVG.Settings.NodeWallet.WalletKey);

            NVG.Settings.Nodes.My.Status = NVS.NodeStatus.Online;
            NVG.Settings.Nodes.My.IP.Wallet = NVG.Settings.NodeWallet.WalletKey;
            NVG.Settings.Nodes.My.IP.Port = Notus.Toolbox.Network.GetNetworkPort();
            //Console.WriteLine(NVG.Settings.Nodes.My.IP.Port);
            //Notus.Print.ReadLine();
        }
        public void Start()
        {
            Console.ResetColor();
            MP_NodeList = new Notus.Mempool(NVC.MemoryPoolName["MainNodeWalletConfig"]);
            MP_NodeList.AsyncActive = false;
            //MP_NodeList.Clear();
            string tmpWalletStr = MP_NodeList.Get("Node_WalletKey", "");
            if (tmpWalletStr.Length > 0)
            {
                Node_WalletKey = tmpWalletStr;
                nodeObj.Wallet.Key = Node_WalletKey;
                nodeObj.Wallet.Defined = true;
                Node_WalletDefined = true;
                checkLayerStatus(NVE.NetworkLayer.Layer4);
            }
            nodeObj.DebugMode = (MP_NodeList.Get("Node_DebugMode", "1") == "1" ? true : false);
            nodeObj.InfoMode = (MP_NodeList.Get("Node_InfoMode", "1") == "1" ? true : false);
            nodeObj.LocalMode = (MP_NodeList.Get("Node_LocalMode", "0") == "1" ? true : false);
            nodeObj.DevelopmentMode = (MP_NodeList.Get("Node_DevelopmentMode", "0") == "1" ? true : false);

            if (nodeObj.DevelopmentMode == true)
            {
                if (nodeObj.Layer.Port.DevNet == 0 || nodeObj.Layer.Port.DevNet > 65535)
                {
                    resetPortMenu(false);
                }
            }
            else
            {
                if (nodeObj.Layer.Port.TestNet == 0 || nodeObj.Layer.Port.TestNet > 65535)
                {
                    resetPortMenu(false);
                }
                if (nodeObj.Layer.Port.MainNet == 0 || nodeObj.Layer.Port.MainNet > 65535)
                {
                    resetPortMenu(false);
                }
            }
            mainMenu();
        }
        private void PrintWalletKey_AllMenu()
        {
            if (Node_WalletDefined == true)
            {
                Console.WriteLine("Your Wallet Key : " + Node_WalletKey);
            }
            else
            {
                Console.WriteLine("Your Wallet Key Is Undefined");
            }
        }
        public void PreStart(string[] args)
        {
            bool LightNodeActive = true;
            if (args.Length > 0)
            {
                for (int a = 0; a < args.Length; a++)
                {
                    if (string.Equals(args[a], "--testnet"))
                    {
                        NVG.Settings.Network = NVE.NetworkType.TestNet;
                    }
                    if (string.Equals(args[a], "--mainnet"))
                    {
                        NVG.Settings.Network = NVE.NetworkType.MainNet;
                    }
                    if (string.Equals(args[a], "--devnet"))
                    {
                        NVG.Settings.Network = NVE.NetworkType.DevNet;
                    }

                    if (string.Equals(args[a], "--light"))
                    {
                        LightNodeActive = true;
                    }


                    if (string.Equals(args[a], "--replicant"))
                    {
                        NVG.Settings.NodeType = NVE.NetworkNodeType.Replicant;
                    }
                    if (string.Equals(args[a], "--main"))
                    {
                        NVG.Settings.NodeType = NVE.NetworkNodeType.Main;
                    }
                    if (string.Equals(args[a], "--master"))
                    {
                        NVG.Settings.NodeType = NVE.NetworkNodeType.Master;
                    }


                    if (string.Equals(args[a], "--debug"))
                    {
                        NVG.Settings.DebugMode = true;
                    }
                    if (string.Equals(args[a], "--info"))
                    {
                        NVG.Settings.InfoMode = true;
                    }


                    if (string.Equals(args[a], "--layer1"))
                    {
                        NVG.Settings.Layer = NVE.NetworkLayer.Layer1;
                    }
                    if (string.Equals(args[a], "--layer2"))
                    {
                        NVG.Settings.Layer = NVE.NetworkLayer.Layer2;
                    }
                    if (string.Equals(args[a], "--layer3"))
                    {
                        NVG.Settings.Layer = NVE.NetworkLayer.Layer3;
                    }
                    if (string.Equals(args[a], "--layer4"))
                    {
                        NVG.Settings.Layer = NVE.NetworkLayer.Layer4;
                    }
                    if (string.Equals(args[a], "--layer5"))
                    {
                        NVG.Settings.Layer = NVE.NetworkLayer.Layer5;
                    }
                    if (string.Equals(args[a], "--layer6"))
                    {
                        NVG.Settings.Layer = NVE.NetworkLayer.Layer6;
                    }
                    if (string.Equals(args[a], "--layer7"))
                    {
                        NVG.Settings.Layer = NVE.NetworkLayer.Layer7;
                    }
                    if (string.Equals(args[a], "--layer8"))
                    {
                        NVG.Settings.Layer = NVE.NetworkLayer.Layer8;
                    }
                    if (string.Equals(args[a], "--layer9"))
                    {
                        NVG.Settings.Layer = NVE.NetworkLayer.Layer9;
                    }
                    if (string.Equals(args[a], "--layer10"))
                    {
                        NVG.Settings.Layer = NVE.NetworkLayer.Layer10;
                    }
                }

                if (NVG.Settings.Layer != NVE.NetworkLayer.Layer1)
                {
                    //CryptoTimerActive = false;
                }
            }
            else
            {
            }
        }
        public Menu()
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Gray;
            foreach (KeyValuePair<NVE.NetworkLayer, string> entry in NVC.LayerText)
            {
                if (entry.Value.Length > longestLayerText)
                {
                    longestLayerText = entry.Value.Length;
                }
            }
            nodeObj = new NVS.NodeInfoForMenu()
            {
                DebugMode = true,
                InfoMode = true,
                LocalMode = false,
                Wallet = new NVS.NodeWalletInfo()
                {
                    Defined = false,
                    FullDefined = false,
                    Key = "",
                    PublicKey = "",
                    Sign = ""
                },
                DevelopmentMode = false,
                Layer = new NVS.LayerInfo()
                {
                    Selected = NVE.NetworkLayer.Layer1,
                    Port = new NVS.CommunicationPorts()
                    {
                        DevNet = NVC.PortNo[NVE.NetworkLayer.Layer1][NVE.NetworkType.DevNet],
                        MainNet = NVC.PortNo[NVE.NetworkLayer.Layer1][NVE.NetworkType.MainNet],
                        TestNet = NVC.PortNo[NVE.NetworkLayer.Layer1][NVE.NetworkType.TestNet]
                    }
                }
            };
        }
        ~Menu()
        {
            Dispose();
        }
        public void Dispose()
        {
            MP_NodeList.Dispose();
        }
    }
}