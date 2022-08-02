using System;
using System.Collections.Generic;

namespace Notus
{
    public class Sync
    {
        public static bool Block(Notus.Variable.Common.ClassSetting objSettings)
        {
            return true;
            Console.WriteLine(objSettings.LastBlock.info.uID);
            Console.WriteLine(objSettings.LastBlock.info.type);
            Console.WriteLine(objSettings.LastBlock.info.rowNo);
            Console.ReadLine();
            string[] ZipFileList = Notus.IO.GetZipFiles(objSettings);
            string myGenesisSign = string.Empty;
            DateTime myGenesisTime = DateTime.Now.AddDays(1);

            // we have genesis
            if (ZipFileList.Length > 0)
            {
                Notus.Print.Basic(objSettings, "We Have Block - Lets Check Genesis Time And Hash");
                using (Notus.Block.Storage BS_Storage = new Notus.Block.Storage(false))
                {
                    BS_Storage.Network = objSettings.Network;
                    BS_Storage.Layer = objSettings.Layer;
                    (bool blockExist, Notus.Variable.Class.BlockData blockData) = BS_Storage.ReadBlock(Notus.Variable.Constant.GenesisBlockUid);
                    if (blockExist == true)
                    {
                        if (blockData.info.type == 360)
                        {
                            myGenesisSign = blockData.sign;
                            myGenesisTime = Notus.Date.GetGenesisCreationTimeFromString(blockData);
                        }
                    }
                }
            }
            else
            {
                Notus.Print.Basic(objSettings, "We Do Not Have Any Block");
            }

            //there is no layer on constant
            if (Notus.Validator.List.Main.ContainsKey(objSettings.Layer) == false)
            {
                return false;
            }

            //there is no Network on constant
            if (Notus.Validator.List.Main[objSettings.Layer].ContainsKey(objSettings.Network) == false)
            {
                return false;
            }

            Dictionary<string, Notus.Variable.Class.BlockData> signBlock = new Dictionary<string, Notus.Variable.Class.BlockData>();
            signBlock.Clear();

            Dictionary<string, int> signCount = new Dictionary<string, int>();
            signCount.Clear();

            foreach (Variable.Struct.IpInfo item in Notus.Validator.List.Main[objSettings.Layer][objSettings.Network])
            {
                if (string.Equals(objSettings.IpInfo.Public, item.IpAddress) == false)
                {
                    (bool tmpError, Notus.Variable.Class.BlockData tmpInnerBlockData) =
                    Notus.Toolbox.Network.GetBlockFromNode(item.IpAddress, item.Port, 1, objSettings);
                    if (tmpError == false)
                    {
                        if (signCount.ContainsKey(tmpInnerBlockData.sign) == false)
                        {
                            signCount.Add(tmpInnerBlockData.sign, 0);
                            signBlock.Add(tmpInnerBlockData.sign, tmpInnerBlockData);
                        }
                        signCount[tmpInnerBlockData.sign] = signCount[tmpInnerBlockData.sign] + 1;
                    }
                    else
                    {
                        Notus.Print.Danger(objSettings, "Error Happened While Trying To Get Genesis From Other Node");
                        Notus.Date.SleepWithoutBlocking(100);
                    }
                }
            }

            if (signCount.Count == 0)
            {
                return false;
            }
            int tmpBiggestCount = 0;
            string tmpBiggestSign = string.Empty;
            foreach (KeyValuePair<string, int> entry in signCount)
            {
                if (entry.Value > tmpBiggestCount)
                {
                    tmpBiggestCount = entry.Value;
                    tmpBiggestSign = entry.Key;
                }
            }
            if (string.Equals(tmpBiggestSign, myGenesisSign) == false)
            {
                DateTime otherNodeGenesisTime = Notus.Date.GetGenesisCreationTimeFromString(signBlock[tmpBiggestSign]);
                Int64 otherNodeGenesisTimeVal = Int64.Parse(
                    otherNodeGenesisTime.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText)
                );
                Int64 myGenesisTimeVal = Int64.Parse(
                    myGenesisTime.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText)
                );
                if (myGenesisTimeVal > otherNodeGenesisTimeVal)
                {
                    using (Notus.Block.Storage BS_Storage = new Notus.Block.Storage(false))
                    {
                        BS_Storage.Network = objSettings.Network;
                        BS_Storage.Layer = objSettings.Layer;
                        Notus.Print.Basic(objSettings, "Current Block Were Deleted");
                        Notus.IO.ClearBlocks(objSettings.Network, objSettings.Layer);
                        BS_Storage.AddSync(signBlock[tmpBiggestSign], true);
                        Notus.Print.Basic(objSettings, "Added Block : " + signBlock[tmpBiggestSign].info.uID);
                    }
                    Notus.Date.SleepWithoutBlocking(150);
                }
                else
                {
                    Notus.Print.Basic(objSettings, "Hold Your Genesis Block - We Are Older");
                }
            }
            //Console.WriteLine("Press Enter To Continue");
            //Console.ReadLine();
            return true;
        }
    }
}
