using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
namespace Notus.Validator
{
    public class Menu : IDisposable
    {
        private Notus.Variable.Struct.NodeInfo nodeObj;

        private bool Node_WalletDefined = false;
        private string Node_WalletKey = string.Empty;

        private int indexMainMenu = 0;
        private Notus.Mempool MP_NodeList;
        private int longestLayerText = 0;
        private Dictionary<Notus.Variable.Enum.NetworkLayer, string> layerText = new Dictionary<Notus.Variable.Enum.NetworkLayer, string>() {
            { Notus.Variable.Enum.NetworkLayer.Layer1,"Layer 1 ( Crypto Layer )" },
            { Notus.Variable.Enum.NetworkLayer.Layer2,"Layer 2 ( File Storage Layer )" },
            { Notus.Variable.Enum.NetworkLayer.Layer3,"Layer 3 ( Crypto Message Layer )" },
            { Notus.Variable.Enum.NetworkLayer.Layer4,"Layer 4 ( Secure File Storage Layer )" },
        };
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
                Notus.Variable.Struct.EccKeyPair newWalletKey = Notus.Wallet.ID.GenerateKeyPair();
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
                        MP_NodeList.Set("Node_WalletKey", newWalletKey.WalletKey, true);
                        Node_WalletDefined = true;
                        Node_WalletKey = newWalletKey.WalletKey;

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
                    Console.Write("Miner Key  : ");
                }
                userDefineWalletKey = Console.ReadLine();
                tmpExitWhileLoop = true;
                userDefineWalletKey = userDefineWalletKey.Trim();
                if (userDefineWalletKey.Length != 38)
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
        private void showMySettings(Notus.Variable.Enum.NetworkLayer layerObj)
        {
            Console.WriteLine(SameLengthStr("", longestLayerText + 10, '-'));

            Console.Write(SameLengthStr(layerText[layerObj], longestLayerText) + " : ");

            if (nodeObj.Layer[layerObj].Active == true)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("enable");
                Console.ResetColor();
                Console.WriteLine(SameLengthStr("Main Net Port Number", longestLayerText) + " : " + nodeObj.Layer[layerObj].Port.MainNet.ToString());
                Console.WriteLine(SameLengthStr("Test Net Port Number", longestLayerText) + " : " + nodeObj.Layer[layerObj].Port.TestNet.ToString());
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("disable");
                Console.ResetColor();
            }
        }
        private void showMySettings(string optionText, bool boolObj)
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
        private void showMySettings()
        {
            Console.Clear();
            Console.CursorVisible = false;
            PrintWalletKey_AllMenu();
            showMySettings(Notus.Variable.Enum.NetworkLayer.Layer1);
            showMySettings(Notus.Variable.Enum.NetworkLayer.Layer2);
            showMySettings(Notus.Variable.Enum.NetworkLayer.Layer3);
            showMySettings(Notus.Variable.Enum.NetworkLayer.Layer4);
            showMySettings("Debug Mode", nodeObj.DebugMode);
            showMySettings("Info Mode", nodeObj.InfoMode);

            Console.WriteLine();
            Console.WriteLine("Press any to continue");
            Console.ReadKey();
            Console.Clear();
        }
        private void nodeTypeMenu()
        {
            bool exitFromSubMenuLoop = false;
            int tmpMenuIndexNo = indexMainMenu;
            indexMainMenu = 0;
            while (exitFromSubMenuLoop == false)
            {
                List<string> menuList = new List<string>() { };
                foreach (KeyValuePair<Notus.Variable.Enum.NetworkLayer, string> entry in layerText)
                {
                    menuList.Add(entry.Value);
                }
                menuList.Add("Go Back");
                if (drawMainMenu_NodeType(menuList) == true)
                {
                    setLayerStatus(Notus.Variable.Enum.NetworkLayer.Layer1);
                    setLayerStatus(Notus.Variable.Enum.NetworkLayer.Layer2);
                    setLayerStatus(Notus.Variable.Enum.NetworkLayer.Layer3);
                    setLayerStatus(Notus.Variable.Enum.NetworkLayer.Layer4);
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
            }
            else if (ckey.Key == ConsoleKey.Enter)
            {
                if (indexMainMenu == 0)
                    nodeObj.DebugMode = !nodeObj.DebugMode;
                if (indexMainMenu == 1)
                    nodeObj.InfoMode = !nodeObj.InfoMode;
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
                    "Go Back"
                };

                if (drawMainMenu_DebugInfo(menuList) == true)
                {
                    MP_NodeList.Set("Node_DebugMode", nodeObj.DebugMode == true ? "1" : "0", true);
                    MP_NodeList.Set("Node_InfoMode", nodeObj.InfoMode == true ? "1" : "0", true);
                    exitFromSubMenuLoop = true;
                }
            }
            indexMainMenu = tmpMenuIndexNo;
            Console.Clear();
        }

        private bool GetLayerPortNumber(Notus.Variable.Enum.NetworkLayer layerObj)
        {
            Console.CursorVisible = true;
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
                            nodeObj.Layer[layerObj].Port.MainNet = tmpMainPortNo;
                            nodeObj.Layer[layerObj].Port.TestNet = tmpTestPortNo;
                            setLayerStatus(layerObj);
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
                    GetLayerPortNumber(Notus.Variable.Enum.NetworkLayer.Layer1);
                    return 9;
                }
                if (indexMainMenu == 2) // layer 2'nin portlarını seç
                {
                    GetLayerPortNumber(Notus.Variable.Enum.NetworkLayer.Layer2);
                    return 9;
                }
                if (indexMainMenu == 3) // layer 3'ün portlarını seç
                {
                    GetLayerPortNumber(Notus.Variable.Enum.NetworkLayer.Layer3);
                    return 9;
                }
                if (indexMainMenu == 4) // layer 4'ün portlarını seç
                {
                    GetLayerPortNumber(Notus.Variable.Enum.NetworkLayer.Layer4);
                    return 9;
                }
            }
            Console.Clear();
            return 9;
        }

        private void nodePortMenu()
        {
            Dictionary<int, string> PortList = new Dictionary<int, string>();
            if (nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer1].Active == true)
            {
                PortList.Add(1, layerText[Notus.Variable.Enum.NetworkLayer.Layer1]);
            }
            if (nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer2].Active == true)
            {
                PortList.Add(2, layerText[Notus.Variable.Enum.NetworkLayer.Layer2]);
            }
            if (nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer3].Active == true)
            {
                PortList.Add(3, layerText[Notus.Variable.Enum.NetworkLayer.Layer3]);
            }
            if (nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer4].Active == true)
            {
                PortList.Add(4, layerText[Notus.Variable.Enum.NetworkLayer.Layer4]);
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
            setLayerStatus(Notus.Variable.Enum.NetworkLayer.Layer1);
            setLayerStatus(Notus.Variable.Enum.NetworkLayer.Layer2);
            setLayerStatus(Notus.Variable.Enum.NetworkLayer.Layer3);
            setLayerStatus(Notus.Variable.Enum.NetworkLayer.Layer4);
            indexMainMenu = tmpMenuIndexNo;
            Console.Clear();
        }
        private void mainMenu()
        {
            Console.Clear();
            bool startNodeSelected = false;
            while (startNodeSelected == false)
            {
                int selectedMenuItem = drawMainMenu_MainMenu(new List<string>()
                {
                    "Start Node",
                    "Node Type",
                    "Change Ports",
                    "Change Wallet Key",
                    "Debug / Info Mode",
                    "Show My Settings",
                    "Exit"
                });
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
                }

                if (selectedMenuItem == 1) // "Node Type"
                {
                    nodeTypeMenu();
                } // else if  "Node Type"

                if (selectedMenuItem == 2) // "Change Ports"
                {
                    nodePortMenu();
                } // else if "Change Wallet Key"

                if (selectedMenuItem == 3) // "Change Wallet Key"
                {
                    walletMenu();
                } // else if "Change Wallet Key"

                if (selectedMenuItem == 4) // "Debug / Info Mode"
                {
                    debugInfoMenu();
                }
                if (selectedMenuItem == 5) // show my settings
                {
                    showMySettings();
                }
                if (selectedMenuItem == 6) // exit
                {
                    //Dispose();
                    Environment.Exit(0);
                    while (true) { }
                }
            }
        }
        private int drawMainMenu_MainMenu(List<string> items)
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
                    scrnText = scrnText + "[ " + (nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer1].Active == true ? "X" : " ") + " ] ";
                }
                if (i == 1)
                {
                    scrnText = scrnText + "[ " + (nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer2].Active == true ? "X" : " ") + " ] ";
                }
                if (i == 2)
                {
                    scrnText = scrnText + "[ " + (nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer3].Active == true ? "X" : " ") + " ] ";
                }
                if (i == 3)
                {
                    scrnText = scrnText + "[ " + (nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer4].Active == true ? "X" : " ") + " ] ";
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
                    nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer1].Active = !nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer1].Active;
                if (indexMainMenu == 1)
                    nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer2].Active = !nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer2].Active;
                if (indexMainMenu == 2)
                    nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer3].Active = !nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer3].Active;
                if (indexMainMenu == 3)
                    nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer4].Active = !nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer4].Active;
            }
            else if (ckey.Key == ConsoleKey.Enter)
            {
                if (indexMainMenu == 0)
                    nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer1].Active = !nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer1].Active;
                if (indexMainMenu == 1)
                    nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer2].Active = !nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer2].Active;
                if (indexMainMenu == 2)
                    nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer3].Active = !nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer3].Active;
                if (indexMainMenu == 3)
                    nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer4].Active = !nodeObj.Layer[Notus.Variable.Enum.NetworkLayer.Layer4].Active;
                if (indexMainMenu == 4)
                    return true;
            }

            Console.Clear();
            return false;
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


        private void setLayerStatus(Notus.Variable.Enum.NetworkLayer layerObj)
        {
            MP_NodeList.Set(layerObj.ToString(), JsonSerializer.Serialize(nodeObj.Layer[layerObj]), true);
        }
        private void checkLayerStatus(Notus.Variable.Enum.NetworkLayer layerObj)
        {
            Notus.Variable.Struct.LayerInfo tmpLayerObj = new Notus.Variable.Struct.LayerInfo()
            {
                Active = false,
                Port = new Notus.Variable.Struct.CommunicationPorts()
                {
                    DevNet = 0,
                    MainNet = 0,
                    TestNet = 0
                }
            };
            string tmpNodeType = MP_NodeList.Get(layerObj.ToString(), "");
            if (tmpNodeType != "")
            {
                try
                {
                    nodeObj.Layer[layerObj] = JsonSerializer.Deserialize<Notus.Variable.Struct.LayerInfo>(tmpNodeType);
                }
                catch
                {
                    nodeObj.Layer[layerObj] = tmpLayerObj;
                }
            }
            else
            {
                nodeObj.Layer[layerObj] = tmpLayerObj;
            }
        }
        public void Start()
        {
            MP_NodeList = new Notus.Mempool(Notus.Variable.Constant.MemoryPoolName["MainNodeWalletConfig"]);
            MP_NodeList.AsyncActive = false;
            //MP_NodeList.Clear();
            string tmpWalletStr = MP_NodeList.Get("Node_WalletKey", "");
            if (tmpWalletStr.Length > 0)
            {
                Node_WalletKey = tmpWalletStr;
                Node_WalletDefined = true;
                checkLayerStatus(Notus.Variable.Enum.NetworkLayer.Layer1);
                checkLayerStatus(Notus.Variable.Enum.NetworkLayer.Layer2);
                checkLayerStatus(Notus.Variable.Enum.NetworkLayer.Layer3);
                checkLayerStatus(Notus.Variable.Enum.NetworkLayer.Layer4);
            }
            nodeObj.DebugMode = (MP_NodeList.Get("Node_DebugMode", "1") == "1" ? true : false);
            nodeObj.InfoMode = (MP_NodeList.Get("Node_InfoMode", "1") == "1" ? true : false);
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

        public Menu()
        {
            foreach (KeyValuePair<Notus.Variable.Enum.NetworkLayer, string> entry in layerText)
            {
                if (entry.Value.Length > longestLayerText)
                {
                    longestLayerText = entry.Value.Length;
                }
            }
            nodeObj = new Notus.Variable.Struct.NodeInfo()
            {
                DebugMode = true,
                InfoMode = true,
                Layer = new Dictionary<Notus.Variable.Enum.NetworkLayer, Notus.Variable.Struct.LayerInfo>()
                {
                    {
                        Notus.Variable.Enum.NetworkLayer.Layer1,
                        new Notus.Variable.Struct.LayerInfo()
                        {
                            Port=new Notus.Variable.Struct.CommunicationPorts()
                            {
                                DevNet = Notus.Variable.Constant.PortNo[Notus.Variable.Enum.NetworkLayer.Layer1][Variable.Enum.NetworkType.DevNet],
                                MainNet = Notus.Variable.Constant.PortNo[Notus.Variable.Enum.NetworkLayer.Layer1][Variable.Enum.NetworkType.MainNet],
                                TestNet = Notus.Variable.Constant.PortNo[Notus.Variable.Enum.NetworkLayer.Layer1][Variable.Enum.NetworkType.TestNet]
                            }
                        }
                    },
                    {
                        Notus.Variable.Enum.NetworkLayer.Layer2,
                        new Notus.Variable.Struct.LayerInfo(){
                            Port=new Notus.Variable.Struct.CommunicationPorts()
                            {
                                DevNet = Notus.Variable.Constant.PortNo[Notus.Variable.Enum.NetworkLayer.Layer2][Variable.Enum.NetworkType.DevNet],
                                MainNet = Notus.Variable.Constant.PortNo[Notus.Variable.Enum.NetworkLayer.Layer2][Variable.Enum.NetworkType.MainNet],
                                TestNet = Notus.Variable.Constant.PortNo[Notus.Variable.Enum.NetworkLayer.Layer2][Variable.Enum.NetworkType.TestNet]
                            }
                      }
                    },
                    {
                        Notus.Variable.Enum.NetworkLayer.Layer3,
                        new Notus.Variable.Struct.LayerInfo(){
                            Port=new Notus.Variable.Struct.CommunicationPorts()
                            {
                                DevNet = Notus.Variable.Constant.PortNo[Notus.Variable.Enum.NetworkLayer.Layer3][Variable.Enum.NetworkType.DevNet],
                                MainNet = Notus.Variable.Constant.PortNo[Notus.Variable.Enum.NetworkLayer.Layer3][Variable.Enum.NetworkType.MainNet],
                                TestNet = Notus.Variable.Constant.PortNo[Notus.Variable.Enum.NetworkLayer.Layer3][Variable.Enum.NetworkType.TestNet]
                            }
                      }
                    },
                    {
                        Notus.Variable.Enum.NetworkLayer.Layer4,
                        new Notus.Variable.Struct.LayerInfo(){
                            Port = new Notus.Variable.Struct.CommunicationPorts()
                            {
                                DevNet = Notus.Variable.Constant.PortNo[Notus.Variable.Enum.NetworkLayer.Layer4][Variable.Enum.NetworkType.DevNet],
                                MainNet = Notus.Variable.Constant.PortNo[Notus.Variable.Enum.NetworkLayer.Layer4][Variable.Enum.NetworkType.MainNet],
                                TestNet = Notus.Variable.Constant.PortNo[Notus.Variable.Enum.NetworkLayer.Layer4][Variable.Enum.NetworkType.TestNet]
                            }
                        }
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
