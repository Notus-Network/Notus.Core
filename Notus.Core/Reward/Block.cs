using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;

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
        private Notus.Threads.Timer? TimerObj;
        public void Execute(
            Notus.Variable.Common.ClassSetting objSettings,
            System.Action<Notus.Variable.Struct.EmptyBlockRewardStruct> Func_NewBlockIncome
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

                        if (howManySecondAgo > dayAsSecond)
                        {
                            try
                            {
                                ulong earliestBlockTime = ulong.MaxValue;
                                Notus.Variable.Struct.EmptyBlockRewardStruct rewardBlock = new Notus.Variable.Struct.EmptyBlockRewardStruct()
                                {
                                    Order = 0,
                                    Spend = 0,
                                    Left = 0,
                                    List = new Dictionary<string, List<long>>(),
                                    Addition = new Dictionary<string, Dictionary<ulong, string>>(),
                                    LuckyNode = new Dictionary<string, Dictionary<ulong, string>>()
                                };
                                ulong rewardCount = 0;
                                Dictionary<long, ulong> blockRowTimeList = new Dictionary<long, ulong>();
                                Notus.Block.Storage storageObj = new Notus.Block.Storage(false);
                                storageObj.Network = objSettings.Network;
                                storageObj.Layer = objSettings.Layer;
                                bool tmpNullPrinted = false;
                                bool tmpExitLoop = false;
                                while (tmpExitLoop == false)
                                {
                                    Notus.Variable.Class.BlockData? tmpBlockData = storageObj.ReadBlock(LastBlockUid);
                                    if (tmpBlockData != null)
                                    {
                                        if (tmpBlockData.info.type == 300)
                                        {
                                            string blockValidator = tmpBlockData.miner.count.First().Key;
                                            rewardCount++;
                                            ulong tmpBlockTime = Notus.Date.ToLong(tmpBlockData.info.time);
                                            if (earliestBlockTime > tmpBlockTime)
                                            {
                                                earliestBlockTime = tmpBlockTime;
                                            }

                                            blockRowTimeList.Add(tmpBlockData.info.rowNo, tmpBlockTime);

                                            if (rewardBlock.List.ContainsKey(blockValidator) == false)
                                            {
                                                rewardBlock.List.Add(blockValidator, new List<long>() { });
                                            }

                                            if (rewardBlock.List[blockValidator].IndexOf(tmpBlockData.info.rowNo) == -1)
                                            {
                                                rewardBlock.List[blockValidator].Add(tmpBlockData.info.rowNo);
                                            }
                                        }
                                        LastBlockUid = tmpBlockData.prev.Substring(0, 90);
                                        if (string.Equals(LastTypeUid, tmpBlockData.info.uID) == true)
                                        {
                                            tmpExitLoop = true;
                                        }
                                        tmpNullPrinted = false;
                                    }
                                    else
                                    {
                                        if (tmpNullPrinted == false)
                                        {
                                            tmpNullPrinted = true;
                                            Console.WriteLine("tmpBlockData = NULL;");
                                        }
                                        tmpExitLoop = true;
                                    }
                                }

                                Console.WriteLine("Reward Distribution");
                                ulong decimalNumber = (ulong)Math.Pow(10, (double)objSettings.Genesis.Reserve.Decimal);

                                // genesisi oluşturulmadan önce bu değerler olmadığı için 
                                // burada atanıyor...
                                objSettings.Genesis.Empty.TotalSupply = 550000000;
                                objSettings.Genesis.Empty.LuckyReward = 50;
                                objSettings.Genesis.Empty.Reward = 2;

                                ulong rewardVolume = (rewardCount * objSettings.Genesis.Empty.Reward) * decimalNumber;
                                ulong totalSuppply = objSettings.Genesis.Empty.TotalSupply * decimalNumber;
                                ulong luckyReward = objSettings.Genesis.Empty.LuckyReward * decimalNumber;
                                ulong emptyRewardVolume = rewardVolume - luckyReward;
                                ulong rewardPerBlock = (ulong)Math.Floor((decimal)emptyRewardVolume / rewardCount);

                                ulong rewardLeft = totalSuppply - rewardVolume;
                                ulong checkBlockReardDist = rewardPerBlock * rewardCount;
                                ulong rewardDiff = emptyRewardVolume - checkBlockReardDist;
                                luckyReward = luckyReward + rewardDiff;

                                rewardBlock.Count = rewardCount;
                                rewardBlock.Order = 1;
                                rewardBlock.Spend = rewardVolume;
                                rewardBlock.Left = rewardLeft;
                                foreach (KeyValuePair<string, List<long>> entry in rewardBlock.List)
                                {
                                    string tmpNodeWallet = entry.Key;
                                    if (rewardBlock.Addition.ContainsKey(tmpNodeWallet) == false)
                                    {
                                        rewardBlock.Addition.Add(tmpNodeWallet, new Dictionary<ulong, string>());
                                    }
                                    foreach (long innerEntry in entry.Value)
                                    {
                                        ulong bTime = Notus.Date.ToLong(
                                            Notus.Date.ToDateTime(
                                                blockRowTimeList[innerEntry]
                                            ).AddDays(30)
                                        );

                                        if (rewardBlock.Addition[tmpNodeWallet].ContainsKey(bTime) == false)
                                        {
                                            rewardBlock.Addition[tmpNodeWallet].Add(bTime, "0");
                                        }

                                        //Console.WriteLine((rewardBlock.Addition[tmpNodeWallet][bTime]));
                                        //Console.ReadLine();

                                        BigInteger tmpTotalForNode =
                                            BigInteger.Parse(rewardBlock.Addition[tmpNodeWallet][bTime]) +
                                            rewardPerBlock;

                                        rewardBlock.Addition[tmpNodeWallet][bTime] =
                                        tmpTotalForNode.ToString();
                                    }
                                    /*
                                    */
                                }


                                /*
                                Notus.Variable.Struct.EmptyBlockRewardStruct rewardBlock = new Notus.Variable.Struct.EmptyBlockRewardStruct()
                                {
                                    Order = 0,
                                    Spend = 0,
                                    Left = 0,
                                    List = new Dictionary<string, List<long>>(),
                                    Add = new Dictionary<string, Dictionary<ulong, string>>()
                                };

                                */

                                string luckyNodeWalletStr = "lucky-node-wallet";
                                rewardBlock.LuckyNode.Add(luckyNodeWalletStr, new Dictionary<ulong, string>()
                                {
                                    {
                                        Notus.Date.ToLong(
                                            Notus.Date.ToDateTime(
                                                earliestBlockTime
                                            ).AddDays(30)
                                        ), 
                                        luckyReward.ToString() 
                                    }
                                });
                                //Console.WriteLine();
                                //Console.WriteLine(JsonSerializer.Serialize(rewardBlock));
                                //Console.WriteLine();
                                // Console.WriteLine(JsonSerializer.Serialize(rewardBlock, new JsonSerializerOptions() { WriteIndented = true }));
                                //Console.ReadLine();
                                Func_NewBlockIncome(rewardBlock);
                                //Console.ReadLine();
                            }
                            catch (Exception err)
                            {
                                Console.WriteLine(err.Message);
                            }
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
            //Console.WriteLine("Control-Point-77-99886655");
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
            RewardList.Clear();
        }
    }
}
