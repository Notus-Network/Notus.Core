using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NP = Notus.Print;
using NVG = Notus.Variable.Globals;
namespace Notus.Memory
{
    public class Balance : IDisposable
    {
        private Notus.Threads.Timer TimerObj;

        // cüzdan adresinin bilinen zaman değeri
        private ConcurrentDictionary<string, ulong> WalletTime = new ConcurrentDictionary<string, ulong>();

        // cüzdana erişim zamanı ( ulong) -> cüzdan adresi
        private SortedDictionary<ulong, string> AccessTime = new SortedDictionary<ulong, string>();

        //cüzdan adresi -> bakiye değişkeni
        private ConcurrentDictionary<string, Notus.Variable.Struct.WalletBalanceStruct> WalletList = new ConcurrentDictionary<string, Notus.Variable.Struct.WalletBalanceStruct>();
        public Balance()
        {
            TimerObj = new Notus.Threads.Timer(2000);
            TimerObj.Start(() =>
            {
                if (WalletList.Count > Notus.Variable.Constant.WalletMemoryCountLimit)
                {
                    // 1 milyondan büyük ise, en eski cüzdan bakiyesini sil
                    KeyValuePair<ulong, string> firstRow = AccessTime.First();
                    AccessTime.Remove(firstRow.Key);
                    WalletTime.TryRemove(firstRow.Value, out _);
                    WalletList.TryRemove(firstRow.Value, out _);
                }
            }, true);
        }

        public void Set(string WalletId)
        {
            ulong exactTime = NVG.NOW.Int;
            WalletTime.TryAdd(WalletId, exactTime);
            AccessTime.TryAdd(exactTime, WalletId);

            // burada wallet adresi eklenecek,
            // eklenme tarihi güncellenecek
            WalletList.TryAdd(WalletId, new Notus.Variable.Struct.WalletBalanceStruct() { });
        }
        public Notus.Variable.Struct.WalletBalanceStruct? Get(string WalletId)
        {
            if (WalletList.ContainsKey(WalletId))
            {
                return WalletList[WalletId];
            }
            return null;
        }
        ~Balance()
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
        }
    }
}
