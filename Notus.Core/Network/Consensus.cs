using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.Json;

namespace Notus
{
    public class Consensus
    {
        public Notus.Variable.Enum.ValidatorOrder ValidatorOrder(
            Notus.Variable.Class.BlockData NewBlockStruct,
            List<Notus.Variable.Struct.NodeListResponseStruct> NodeList,
            string MyWalletKey
        )
        {
            //string tmpBlockStr = NVG.NOW.Obj.ToString("yyyyMMddHHmmssffffff");
            string tmpBlockStr = JsonSerializer.Serialize(NewBlockStruct);
            SortedDictionary<BigInteger, string> WalletOrder = new SortedDictionary<BigInteger, string>();
            for (int a = 0; a < NodeList.Count; a++)
            {
                if (NodeList[a].countdown == 0)
                {
                    int innerCount = 1;
                    bool exitWhileLoop = false;
                    while (exitWhileLoop == false)
                    {
                        BigInteger tmpBigVal = new BigInteger(
                            new Notus.HashLib.SHA1().Compute(
                                tmpBlockStr +
                                Notus.Variable.Constant.CommonDelimeterChar +
                                NodeList[a].key +
                                Notus.Variable.Constant.CommonDelimeterChar +
                                innerCount.ToString()
                            )
                        );
                        if (WalletOrder.ContainsKey(tmpBigVal) == false)
                        {
                            WalletOrder.Add(tmpBigVal, NodeList[a].key);
                            exitWhileLoop = true;
                        }
                        innerCount++;
                    }
                }
            }

            int counterVal = 0;
            foreach (KeyValuePair<BigInteger, string> keyItem in WalletOrder)
            {
                //Console.WriteLine("Key: {0}, Value: {1}", keyItem.Key, keyItem.Value);

                if (counterVal == 0)
                {
                    if (string.Equals(MyWalletKey, keyItem.Value) == true)
                    {
                        //Console.WriteLine("birinci hesaplayici");
                        return Notus.Variable.Enum.ValidatorOrder.Primary;
                    }
                }
                if (counterVal == 1)
                {
                    if (string.Equals(MyWalletKey, keyItem.Value) == true)
                    {
                        //Console.WriteLine("yedek hesaplayici");
                        return Notus.Variable.Enum.ValidatorOrder.Controller;
                    }
                }
                if (counterVal == 2)
                {
                    if (string.Equals(MyWalletKey, keyItem.Value) == true)
                    {
                        //Console.WriteLine("yedek hesaplayici");
                        return Notus.Variable.Enum.ValidatorOrder.Backup;
                    }
                }
                if (counterVal > 2)
                {
                    return Notus.Variable.Enum.ValidatorOrder.Wait;
                }
                counterVal++;
            }
            return Notus.Variable.Enum.ValidatorOrder.Wait;
        }
    }
}
