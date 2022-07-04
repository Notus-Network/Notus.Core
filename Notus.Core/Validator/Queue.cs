using System;
using System.Text.Json;

namespace Notus.Validator
{
    public class Queue : IDisposable
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

        //empty blok için kontrolü yapacak olan node'u seçen fonksiyon
        public Notus.Variable.Enum.ValidatorOrder EmptyTimer()
        {
            return Notus.Variable.Enum.ValidatorOrder.Primary;
        }

        //oluşturulacak blokları kimin oluşturacağını seçen fonksiyon
        public Notus.Variable.Enum.ValidatorOrder Distrubute(Notus.Variable.Class.BlockData BlockData)
        {
            return Notus.Variable.Enum.ValidatorOrder.Primary;
        }
        public Queue(bool infoModeActive = true, bool debugModeActive = true)
        {
            InfoModeActive = infoModeActive;
            DebugModeActive = debugModeActive;
        }
        ~Queue()
        {
            Dispose();
        }
        public void Dispose()
        {
            //MP_BlockPoolList.Dispose();
        }
    }
}
