using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NP = Notus.Print;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Sync
{
    public class Validator : IDisposable
    {
        private bool enoughPrinted = false;
        private bool notEnoughPrinted = false;
        private Notus.Threads.Timer? ValidatorCountTimerObj;
        public Validator()
        {
            NP.Success(NVG.Settings, "Validator Count Sync Has Started");
        }
        ~Validator()
        {
            Dispose();
        }
        public void Start()
        {
            ValidatorCountTimerObj = new Notus.Threads.Timer(5);
            ValidatorCountTimerObj.Start(() =>
            {
                if (NVG.NodeList != null)
                {
                    KeyValuePair<string, NVS.NodeQueueInfo>[]? nList = NVG.NodeList.ToArray();
                    if (nList != null)
                    {
                        int onlineNodeCount = 0;
                        for (int i = 0; i < nList.Length; i++)
                        {
                            if (nList[i].Value.Status == NVS.NodeStatus.Online)
                            {
                                onlineNodeCount++;
                            }
                        }
                        NVG.OnlineNodeCount = onlineNodeCount;
                    }
                    if (Notus.Variable.Constant.MinimumNodeCount >= NVG.OnlineNodeCount)
                    {
                        if (enoughPrinted == false)
                        {
                            NP.Success("Enough NodeCount For Executing");
                            enoughPrinted = true;
                            notEnoughPrinted = false;
                        }
                    }
                    else
                    {
                        if (enoughPrinted == true)
                        {
                            if (notEnoughPrinted == false)
                            {
                                NP.Success("Not Enough NodeCount For Executing");
                                notEnoughPrinted = true;
                            }
                            enoughPrinted = false;
                        }
                        //if(enoughPrinted==true)
                    }
                    //private bool enoughPrinted = false;
                    //private bool notEnoughPrinted = false;

                }
            }, true);  //TimerObj.Start(() =>
        }
        public void Dispose()
        {
            if (ValidatorCountTimerObj != null)
            {
                ValidatorCountTimerObj.Dispose();
            }
        }
    }
}
