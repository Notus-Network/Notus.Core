using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NVG = Notus.Variable.Globals;
using NVC = Notus.Variable.Constant;
using ND = Notus.Date;
namespace Notus.Validator
{
    public static class Helper
    {
        public static void CheckBlockAndEmptyCounter(int blockTypeNo)
        {
            if (blockTypeNo == 300)
            {
                NVG.Settings.EmptyBlockCount++;
                NVG.Settings.OtherBlockCount = 0;
            }
            else
            {
                NVG.Settings.OtherBlockCount++;
                NVG.Settings.EmptyBlockCount = 0;
            }
        }
        public static bool RightBlockValidator(Notus.Variable.Class.BlockData incomeBlock)
        {
            bool innerSendToMyChain=false;
            ulong queueTimePeriod = (ulong)(NVC.BlockListeningForPoolTime + NVC.BlockGeneratingTime + NVC.BlockDistributingTime);
            ulong blockTimeVal = ND.ToLong(incomeBlock.info.time);
            ulong blockGenarationTime = blockTimeVal - (blockTimeVal % queueTimePeriod);

            if (NVG.Settings.Nodes.Queue.ContainsKey(blockGenarationTime) == true)
            {
                string blockValidator = incomeBlock.validator.count.First().Key;
                if (NVG.LocalBlockLoaded == true)
                {
                    Console.WriteLine("blockTimeVal        : " + blockTimeVal.ToString());
                    Console.WriteLine("blockGenarationTime : " + blockGenarationTime.ToString());
                    Console.WriteLine("blockValidator      : " + blockValidator);
                    Console.WriteLine("NVG.Settings.Nodes.Queue[blockGenarationTime].Wallet) : " + NVG.Settings.Nodes.Queue[blockGenarationTime].Wallet);
                }
                if (string.Equals(blockValidator, NVG.Settings.Nodes.Queue[blockGenarationTime].Wallet))
                {
                    innerSendToMyChain = true;
                }
            }
            else
            {
                if (NVG.LocalBlockLoaded == true)
                {
                    Console.WriteLine("Time Does Not Have From Nodes List");
                    Console.WriteLine("blockTimeVal : " + blockTimeVal.ToString() + " - " + incomeBlock.info.time);
                    Console.WriteLine("Main.cs -> blockGenarationTime [ " +
                        incomeBlock.info.rowNo.ToString() +
                        " ]: " +
                        blockGenarationTime.ToString()
                    );
                    //Console.WriteLine(JsonSerializer.Serialize(NVG.Settings.Nodes.Queue, NVC.JsonSetting));
                }
            }
            return innerSendToMyChain;
        }
    }
}
