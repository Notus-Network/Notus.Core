using System;
using System.Timers;

namespace Notus.Threads
{
    public class Timer : IDisposable
    {
        private System.Action DefinedFunctionObj;
        private System.Timers.Timer InnerTimerObject;
        private bool TimerStarted = false;
        private int IntervalTimeValue = 5000;
        public int Interval
        {
            get
            {
                return IntervalTimeValue;
            }
            set
            {
                IntervalTimeValue = value;
            }
        }
        public Timer()
        {
        }
        public Timer(int TimerInterval)
        {
            IntervalTimeValue = TimerInterval;
        }

        public void Kill()
        {
            if (TimerStarted == true)
            {
                if (InnerTimerObject != null)
                {
                    InnerTimerObject.Stop();
                    InnerTimerObject.Dispose();
                }
                InnerTimerObject = null;
            }
            DefinedFunctionObj = null;
        }
        private void SubStart(System.Action incomeAction)
        {
            DefinedFunctionObj = incomeAction;
            InnerTimerObject = new System.Timers.Timer(IntervalTimeValue);
            InnerTimerObject.Elapsed += OnTimedEvent_ForScreen;
            InnerTimerObject.AutoReset = true;
            InnerTimerObject.Enabled = true;
            TimerStarted = true;
            //aTimer.Start();
        }
        public void Start(System.Action incomeAction)
        {
            SubStart(incomeAction);
        }
        public void Start(System.Action incomeAction, bool executeImmediately)
        {
            SubStart(incomeAction);
            if (executeImmediately == true)
            {
                DefinedFunctionObj();
            }
        }
        private void OnTimedEvent_ForScreen(Object source, ElapsedEventArgs e)
        {
            DefinedFunctionObj();
        }
        public void Dispose()
        {
            Kill();
        }
        ~Timer() { }
    }
}
