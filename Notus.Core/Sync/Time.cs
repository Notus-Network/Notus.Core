using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Notus.Communication;
using NVG = Notus.Variable.Globals;
using NP = Notus.Print;
using ND = Notus.Date;
using NVS = Notus.Variable.Struct;
using NGF = Notus.Variable.Globals.Functions;
namespace Notus.Sync
{
    public class Time : IDisposable
    {
        private Notus.Threads.Timer? UtcTimerObj;
        public void Start()
        {
            NP.Success(NVG.Settings, "Time Synchronizer Has Started");
            UtcTimerObj = new Notus.Threads.Timer(1);
            UtcTimerObj.Start(() =>
            {
                if (NVG.NOW.DiffUpdated == true)
                {
                    NGF.UpdateTime();
                }
            }, true);  //TimerObj.Start(() =>            
        }
        public Time()
        {
        }
        ~Time()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (UtcTimerObj != null)
            {
                UtcTimerObj.Dispose();
            }
        }

    }
}
