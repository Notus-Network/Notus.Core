using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Notus
{
    public class Time
    {
        public static ulong BlockIdToUlong(string blockUid)
        {
            return ulong.Parse(Notus.Block.Key.GetTimeFromKey(blockUid).Substring(0, 17));
        }
        public static ulong NowToUlong()
        {
            return ulong.Parse(DateTime.Now.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText));
        }
        public static ulong DateTimeToUlong(DateTime ConvertTime)
        {
            return ulong.Parse(ConvertTime.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText));
        }
    }
}
