using System;
using System.Collections.Concurrent;
using System.Net;
using System.Numerics;
using System.Text.Json;

namespace Notus.Validator
{
    public class Register : IDisposable
    {
        private Notus.Variable.Common.ClassSetting Obj_Settings=new Notus.Variable.Common.ClassSetting();
        public Notus.Variable.Common.ClassSetting Settings
        {
            get { return Obj_Settings; }
            set { Obj_Settings = value; }
        }

        public Register()
        {
        }
        ~Register()
        {
            Dispose();
        }
        public void Dispose()
        {

        }
    }
}
