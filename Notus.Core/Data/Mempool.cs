using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NP = Notus.Print;
namespace Notus
{
    public class Mempool : IDisposable
    {
        private bool DebugModeActive = true;
        public bool DebugMode
        {
            get { return DebugModeActive; }
            set { DebugModeActive = value; }
        }

        private bool InfoModeActive = true;
        public bool InfoMode
        {
            get { return InfoModeActive; }
            set { InfoModeActive = value; }
        }
        private bool AsyncMethodActivated = true;
        public bool AsyncActive
        {
            get { return AsyncMethodActivated; }
            set { AsyncMethodActivated = value; }
        }

        private bool LoopIsWorking = false;
        private string PoolNameForDb = string.Empty;
        private Notus.Data.Sql SqlObj;
        private Notus.Threads.Timer TimerObj;
        private ConcurrentDictionary<string, Notus.Variable.Struct.MempoolDataList> Obj_DataList = new ConcurrentDictionary<string, Notus.Variable.Struct.MempoolDataList>();
        public ConcurrentDictionary<string, Notus.Variable.Struct.MempoolDataList> DataList
        {
            get { return Obj_DataList; }
        }
        public Mempool(string PoolName)
        {
            ExecuteWithClass(PoolName);
        }
        public Mempool(string PoolName, bool ClearPoolData)
        {
            ExecuteWithClass(PoolName);
            if (ClearPoolData == true)
            {
                Clear();
            }
        }
        private void ExecuteWithClass(string PoolName)
        {
            PoolNameForDb = PoolName + ".db";
            Obj_DataList.Clear();
            SqlObj = new Notus.Data.Sql();
            SqlObj.Open(PoolNameForDb);
            string newTableSql = "CREATE TABLE key_value(" +
                "key TEXT NOT NULL UNIQUE," +
                "data TEXT NOT NULL," +
                "expire INTEGER NOT NULL," +
                "added TEXT NOT NULL," +
                "remove TEXT NOT NULL" +
            ");";
            try
            {
                SqlObj.TableExist("key_value", newTableSql);
            }
            catch (Exception err)
            {
                NP.Basic(DebugModeActive, "Error Text [90ecab593] : " + err.Message);
            }

            SqlObj.Select("key_value",
                (Dictionary<string, string> rList) =>
                {
                    string yKeyName = "";
                    string yData = "";
                    string yExpire = "";
                    string yAdded = "";
                    string yRemove = "";
                    foreach (KeyValuePair<string, string> entry in rList)
                    {
                        if (entry.Key == "key")
                        {
                            yKeyName = entry.Value;
                        }
                        if (entry.Key == "data")
                        {
                            yData = entry.Value;
                        }
                        if (entry.Key == "expire")
                        {
                            yExpire = entry.Value;
                        }
                        if (entry.Key == "added")
                        {
                            yAdded = entry.Value;
                        }
                        if (entry.Key == "remove")
                        {
                            yRemove = entry.Value;
                        }
                    }

                    Obj_DataList.TryAdd(yKeyName, new Notus.Variable.Struct.MempoolDataList()
                    {
                        Data = yData,
                        expire = int.Parse(yExpire),
                        added = StringToDateTime(yAdded),
                        remove = StringToDateTime(yRemove)
                    });
                },
                new List<string>() { "key", "data", "expire", "added", "remove" },
                new Dictionary<string, string>() { }
            );



            TimerObj = new Notus.Threads.Timer(2000);
            TimerObj.Start(() =>
            {
                RemoveExpireData();
            }, true);
        }
        private bool UpdateFromTable_Sub(string KeyName)
        {
            return SqlObj.Update("key_value",
                new Dictionary<string, string>(){
                        { "data", Obj_DataList[KeyName].Data },
                        { "expire", Obj_DataList[KeyName].expire.ToString() },
                        { "added", DateTimeToString(Obj_DataList[KeyName].added) },
                        { "remove", DateTimeToString(Obj_DataList[KeyName].remove) }
                },
                new Dictionary<string, string>(){
                        { "key",KeyName}
                }
            );
        }
        private bool UpdateFromTable(string KeyName)
        {
            if (AsyncMethodActivated == true)
            {
                Task.Run(() =>
                {
                    UpdateFromTable_Sub(KeyName);
                }
                );
                return true;
            }
            else
            {
                return UpdateFromTable_Sub(KeyName);
            }
        }
        private bool AddToTable_AsyncMethod(string KeyName, Notus.Variable.Struct.MempoolDataList tListData)
        {
            if (Obj_DataList.ContainsKey(KeyName))
            {
                return SqlObj.Insert("key_value", new Dictionary<string, string>(){
                                { "key", KeyName},
                                { "data", tListData.Data},
                                { "expire", tListData.expire.ToString() },
                                { "added", DateTimeToString(tListData.added) },
                                { "remove", DateTimeToString(tListData.remove) }
                            });
            }
            return false;
        }
        private bool AddToTable(string KeyName)
        {
            if (Obj_DataList.ContainsKey(KeyName))
            {
                Notus.Variable.Struct.MempoolDataList tListData = new Notus.Variable.Struct.MempoolDataList();
                try
                {
                    tListData.added = Obj_DataList[KeyName].added;
                    tListData.Data = Obj_DataList[KeyName].Data;
                    tListData.expire = Obj_DataList[KeyName].expire;
                    tListData.remove = Obj_DataList[KeyName].remove;

                    if (AsyncMethodActivated == true)
                    {
                        Task.Run(() =>
                        {
                            AddToTable_AsyncMethod(KeyName, tListData);
                        }
                        );
                        return true;
                    }
                    else
                    {
                        return AddToTable_AsyncMethod(KeyName, tListData);
                    }
                }
                catch (Exception err)
                {
                    NP.Basic(DebugModeActive, "Error Text [90ecab567]   : " + err.Message);
                    NP.Basic(DebugModeActive, "Mempool Name [90ecab567] : " + PoolNameForDb);
                }
            }
            return false;
        }
        private void DeleteFromTable_SubMethod(string KeyName,bool showError = false) {
            bool resultVal=SqlObj.Delete("key_value", new Dictionary<string, string>(){
                { "key", KeyName }
            });
            if (showError == true)
            {
                //Console.WriteLine("Control-Point-a41234");
                //Console.WriteLine("--------------------------");
                //Console.WriteLine(resultVal);
                //Console.WriteLine("KeyName");
                //Console.WriteLine(KeyName);
            }
        }
        private void DeleteFromTable(string KeyName,bool showError=false)
        {
            if (AsyncMethodActivated == true)
            {
                Task.Run(() =>
                {
                    DeleteFromTable_SubMethod(KeyName, showError);
                }
                );
            }
            else
            {
                DeleteFromTable_SubMethod(KeyName, showError);
            }
        }
        private bool SubAdd(string KeyName, string Data, int Expire)
        {
            if (Obj_DataList.ContainsKey(KeyName))
            {
                Obj_DataList[KeyName].remove = DateTime.Now.AddSeconds(Expire);
                Obj_DataList[KeyName].expire = Expire;
                Obj_DataList[KeyName].Data = Data;
                return UpdateFromTable(KeyName);
            }
            else
            {
                Obj_DataList.TryAdd(KeyName, new Notus.Variable.Struct.MempoolDataList()
                {
                    Data = Data,
                    expire = Expire,
                    added = DateTime.Now,
                    remove = DateTime.Now.AddSeconds(Expire)
                });
                return AddToTable(KeyName);
            }
        }
        public void Clear()
        {
            Obj_DataList.Clear();
            SqlObj.Clear("key_value");
        }
        public int Count()
        {
            return Obj_DataList.Count;
        }
        public void Each(System.Action<string, string> incomeAction, int UseThisNumberAsCountOrMiliSeconds = 1000, Notus.Variable.Enum.MempoolEachRecordLimitType UseThisNumberType = Notus.Variable.Enum.MempoolEachRecordLimitType.Count)
        {
            if (Obj_DataList.Count > 0)
            {
                KeyValuePair<string, Variable.Struct.MempoolDataList>[] tmpObj_DataList = Obj_DataList.ToArray();
                DateTime startTime = DateTime.Now;
                int recordCount = 0;
                for (int i = 0; i < tmpObj_DataList.Count(); i++)
                {
                    if (UseThisNumberAsCountOrMiliSeconds > 0)
                    {
                        if (UseThisNumberType == Notus.Variable.Enum.MempoolEachRecordLimitType.Count)
                        {
                            recordCount++;
                            if (recordCount > UseThisNumberAsCountOrMiliSeconds)
                            {
                                break;
                            }
                        }
                        else
                        {
                            if ((DateTime.Now- startTime).TotalMilliseconds > UseThisNumberAsCountOrMiliSeconds)
                            {
                                break;
                            }
                        }
                    }
                    incomeAction(tmpObj_DataList[i].Key, tmpObj_DataList[i].Value.Data);
                }
            }
        }
        public void GetOne(System.Action<string, string> incomeAction)
        {
            if (Obj_DataList.Count > 0)
            {
                KeyValuePair<string, Variable.Struct.MempoolDataList>[] tmpObj_DataList = Obj_DataList.ToArray();
                incomeAction(tmpObj_DataList[0].Key, tmpObj_DataList[0].Value.Data);
            }
        }
        public bool Expire(string KeyName, int Expire)
        {
            if (Obj_DataList.ContainsKey(KeyName))
            {
                Obj_DataList[KeyName].remove = DateTime.Now.AddSeconds(Expire);
                Obj_DataList[KeyName].expire = Expire;
                UpdateFromTable(KeyName);
                return true;
            }
            else
            {
            }
            return false;
        }
        public void Remove(string KeyName,bool showError=false)
        {
            if (Obj_DataList.ContainsKey(KeyName) == false)
            {
                if (showError == true)
                {
                    //Console.WriteLine("ContainsKey == false");
                }
            }
            bool tst=Obj_DataList.TryRemove(KeyName,out _);
            if(showError== true)
            {
                //Console.WriteLine(tst);
                //Console.WriteLine("bool tst=Obj_DataList.TryRemove(KeyName,out _);");
                //Console.WriteLine(JsonSerializer.Serialize(Obj_DataList));
            }
            DeleteFromTable(KeyName,showError);
        }   
        public string Get(string KeyName, string? ReturnIfKeyDoesntExist = null)
        {
            if (Obj_DataList.ContainsKey(KeyName))
            {
                return Obj_DataList[KeyName].Data;
            }
            if (ReturnIfKeyDoesntExist == null)
            {
                return string.Empty;
            }
            if (ReturnIfKeyDoesntExist.Length==0)
            {
                return string.Empty;
            }
            return ReturnIfKeyDoesntExist;
        }
        public void Set(string KeyName, string Data, bool AddIfDoesntExist = false)
        {
            if (Obj_DataList.ContainsKey(KeyName))
            {
                Obj_DataList[KeyName].Data = Data;
                UpdateFromTable(KeyName);
            }
            else
            {
                if (AddIfDoesntExist == true)
                {
                    Add(KeyName, Data);
                }
            }
        }
        public void Set(string KeyName, string Data, int Expire, bool AddIfDoesntExist = false)
        {
            if (Obj_DataList.ContainsKey(KeyName))
            {
                Obj_DataList[KeyName].Data = Data;
                Obj_DataList[KeyName].remove = DateTime.Now.AddSeconds(Expire);
                Obj_DataList[KeyName].expire = Expire;
                UpdateFromTable(KeyName);
            }
            else
            {
                if (AddIfDoesntExist == true)
                {
                    Add(KeyName, Data, Expire);
                }
            }
        }
        public bool Add(string KeyName, string Data)
        {
            return SubAdd(KeyName, Data, 0);
        }
        public bool Add(string KeyName, string Data, int Expire)
        {
            return SubAdd(KeyName, Data, Expire);
        }
        private string DateTimeToString(DateTime DateTimeObj)
        {
            try
            {
                return DateTimeObj.ToString("yyyyMMddHHmmssfff");
            }
            catch (Exception err)
            {
                NP.Basic(DebugModeActive, "Error Text [90ecab524] : " + err.Message);
                return "19810125020000000";
            }
        }
        private DateTime StringToDateTime(string DateTimeStr)
        {
            try
            {
                return DateTime.ParseExact(DateTimeStr, "yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception err)
            {
                NP.Basic(DebugModeActive, "Error Text [90ecab547] : " + err.Message);
                return new DateTime(1981, 01, 25, 2, 00, 00);
            }
        }
        private void RemoveExpireData()
        {
            if (LoopIsWorking == false)
            {
                LoopIsWorking = true;
                if (Obj_DataList.Count > 0)
                {
                    try
                    {
                        foreach (KeyValuePair<string, Notus.Variable.Struct.MempoolDataList> entry in Obj_DataList)
                        {
                            if (entry.Value.expire > 0)
                            {
                                if (0 > (entry.Value.remove - DateTime.Now).TotalSeconds)
                                {
                                    Obj_DataList.TryRemove(entry.Key,out _);
                                    DeleteFromTable(entry.Key);
                                }
                            }
                        }
                    }
                    catch { }
                }
                LoopIsWorking = false;
            }
        }
        ~Mempool()
        {
            Dispose();
        }
        public void Dispose()
        {
            try
            {
                TimerObj.Dispose();
            }
            catch (Exception err)
            {
            }

            Obj_DataList.Clear();
            try
            {
                SqlObj.Close();
                Thread.Sleep(150);
                SqlObj.Dispose();
            }
            catch (Exception err)
            {
            }
        }
    }
}
