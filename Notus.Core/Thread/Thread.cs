using System;
using System.Timers;

namespace Notus.Threads
{
    public class Thread : IDisposable
    {
        private System.Threading.Thread ThreadObject;
        private System.Action DefinedFunctionObj;
        public Thread()
        {
        }
        public Thread(System.Action incomeAction)
        {
            Start(incomeAction);
        }
        public void Kill()
        {
        }
        public void Start(System.Action incomeAction)
        {
            DefinedFunctionObj = incomeAction;
            ThreadObject = new System.Threading.Thread(() =>
            {
                DefinedFunctionObj();
            });
            ThreadObject.Start();
        }
        public void Dispose()
        {
            Kill();
        }
        ~Thread() { }
    }

}
