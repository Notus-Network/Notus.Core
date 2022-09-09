using System;
using System.Numerics;

namespace Notus.Network
{
    public class Price : IDisposable
    {
        public Int64 Fee()
        {
            //BigInteger asaa = BigInteger.
            return 15000;
        }
        public Price()
        {
        }
        ~Price()
        {

        }
        public void Dispose()
        {
            try
            {
                //Stop();
            }
            catch (Exception err){
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Info,
                    5000064,
                    err.Message,
                    "BlockRowNo",
                    null,
                    err
                );
            }
        }
    }
}
