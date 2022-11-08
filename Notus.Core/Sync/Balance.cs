using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NP = Notus.Print;
using NVG = Notus.Variable.Globals;

namespace Notus.Sync
{
    public class Balance : IDisposable
    {
        public Balance()
        {
            NP.Success(NVG.Settings, "Time Synchronizer Has Started");
        }
        ~Balance()
        {
            Dispose();
        }
        public void Dispose()
        {
            /*
            if (UtcTimerObj != null)
            {
                UtcTimerObj.Dispose();
            }
            */
        }
    }
}
