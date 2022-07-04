/*
using System;
using System.Text.Json;

namespace Notus.Cache
{
    public class Main : IDisposable
    {
        private Notus.Cache.Token Obj_Token;
        private Notus.Variable.Common.ClassSetting Obj_Settings;
        public Notus.Variable.Common.ClassSetting Settings
        {
            get { return Obj_Settings; }
            set { Obj_Settings = value; }
        }
        public void Store(Notus.Variable.Class.BlockData blockData)
        {
            Console.WriteLine("blockData.info.type : " + blockData.info.type);
            string tmpCipherDataStr = Notus.Core.Function.RawCipherData2String(blockData.cipher.data);
            if (blockData.info.type == 160)
            {
                Notus.Variable.Struct.BlockStruct_160 tmpTokenObj = JsonSerializer.Deserialize<Notus.Variable.Struct.BlockStruct_160>(tmpCipherDataStr);
                Obj_Token.Add(tmpTokenObj);
            }
        }
        public void Start()
        {
            Obj_Token = new Notus.Cache.Token();
            Obj_Token.Settings = Obj_Settings;
        }
        public Main()
        {

        }
        ~Main()
        {
            Dispose();
        }
        public void Dispose()
        {
            Obj_Token.Dispose();
        }
    }
}
*/