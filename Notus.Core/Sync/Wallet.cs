using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NP = Notus.Print;
using NVG = Notus.Variable.Globals;

namespace Notus.Sync
{
    public class Wallet : IDisposable
    {
        public Wallet()
        {
            NP.Success(NVG.Settings, "Time Synchronizer Has Started");
        }
        ~Wallet()
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
