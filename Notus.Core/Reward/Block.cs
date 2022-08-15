using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

namespace Notus.Reward
{
    public class Block : IDisposable
    {
        // son blok tipinin oluşturulması zamanı
        public string LastTypeUid = string.Empty;

        // son empty blok zamanı
        public string LastBlockUid = string.Empty;

        public Queue<KeyValuePair<string, string>> RewardList = new Queue<KeyValuePair<string, string>>();
        private bool TimerIsRunning = false;
        private Notus.Threads.Timer TimerObj;
        public void Execute(
            Notus.Variable.Common.ClassSetting objSettings,
            System.Action<Notus.Variable.Class.BlockData>? Func_NewBlockIncome = null
        )
        {
            TimerObj = new Notus.Threads.Timer(60000);
            TimerObj.Start(() =>
            {
                if (TimerIsRunning == false)
                {
                    TimerIsRunning = true;
                    if (LastBlockUid.Length > 0 && LastTypeUid.Length > 0)
                    {
                        DateTime tmpLastTypeStr = Notus.Date.ToDateTime(Notus.Block.Key.GetTimeFromKey(LastTypeUid));
                        DateTime tmpLastBlockStr = Notus.Date.ToDateTime(Notus.Block.Key.GetTimeFromKey(LastBlockUid));
                        TimeSpan ts = tmpLastBlockStr - tmpLastTypeStr;
                        int dayAsSecond = 24 * 60 * 60;
                        int howManySecondAgo = (int)ts.TotalSeconds;

                        //Console.WriteLine("tmpLastTypeStr : " + ((int)ts.TotalSeconds).ToString());
                        //Console.WriteLine("tmpLastTypeStr : " + tmpLastTypeStr);
                        //Console.WriteLine("tmpLastBlockStr : " + tmpLastBlockStr);

                        if (howManySecondAgo > dayAsSecond)
                        {
                            List<long> blockRowNo = new List<long>();
                            Dictionary<string, int> minerCount = new Dictionary<string, int>();
                            Notus.Block.Storage storageObj = new Notus.Block.Storage(false);
                            storageObj.Network = objSettings.Network;
                            storageObj.Layer = objSettings.Layer;
                            bool tmpExitLoop = false;
                            while (tmpExitLoop == false)
                            {
                                Notus.Variable.Class.BlockData? tmpBlockData = storageObj.ReadBlock(LastBlockUid);
                                if (tmpBlockData != null)
                                {
                                    string blockValidator = tmpBlockData.miner.count.First().Key;
                                    if (minerCount.ContainsKey(blockValidator) == false)
                                    {
                                        minerCount.Add(blockValidator, 0);
                                    }
                                    minerCount[blockValidator] = minerCount[blockValidator] + 1;
                                    blockRowNo.Add(tmpBlockData.info.rowNo);

                                    LastBlockUid = tmpBlockData.prev.Substring(0, 90);
                                    if (string.Equals(LastTypeUid, LastBlockUid) == true)
                                    {
                                        tmpExitLoop = true;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("tmpBlockData = NULL;");
                                }
                            }
                            Console.WriteLine("Reward Distribution");
                            Console.WriteLine(JsonSerializer.Serialize(minerCount, new JsonSerializerOptions() { WriteIndented = true }));
                            Console.WriteLine(JsonSerializer.Serialize(blockRowNo));
                        }
                    }
                    /*
                    Console.WriteLine(JsonSerializer.Serialize(RewardList));
                    //blok zamanı ve utc zamanı çakışıyor
                    DateTime tmpLastTime = Notus.Date.ToDateTime(
                        Obj_Settings.LastBlock.info.time
                    ).AddSeconds(howManySeconds);

                    // get utc time from validatır Queue
                    DateTime utcTime = ValidatorQueueObj.GetUtcTime();
                    if (utcTime > tmpLastTime)
                    {
                        if (ValidatorQueueObj.MyTurn)
                        {
                            if ((DateTime.Now - EmptyBlockGeneratedTime).TotalSeconds > 30)
                            {
                                //Console.WriteLine((DateTime.Now - EmptyBlockGeneratedTime).TotalSeconds);
                                EmptyBlockGeneratedTime = DateTime.Now;
                                Notus.Print.Success(Obj_Settings, "Empty Block Executed");
                                Obj_BlockQueue.AddEmptyBlock();
                            }
                            EmptyBlockNotMyTurnPrinted = false;
                        }
                        else
                        {
                            if (EmptyBlockNotMyTurnPrinted == false)
                            {
                                //Notus.Print.Warning(Obj_Settings, "Not My Turn For Empty Block");
                                EmptyBlockNotMyTurnPrinted = true;
                            }
                        }
                    }
                    */
                    TimerIsRunning = false;
                }
            }, true);
        }
        public Block()
        {
        }
        ~Block()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (TimerObj != null)
            {
                TimerObj.Dispose();
            }
        }
    }
}
