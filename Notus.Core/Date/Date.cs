using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Notus
{
    public static class Date
    {
        public static ulong ToLong(DateTime convertTime)
        {
            return ulong.Parse(convertTime.ToString(Variable.Constant.DefaultDateTimeFormatText));
        }

        public static DateTime ToString(ulong ConverTime)
        {
            return ToDateTime(ConverTime.ToString().PadRight(17,'0'));
        }
        public static string ToString(DateTime DateTimeObj)
        {
            try
            {
                return DateTimeObj.ToString("yyyyMMddHHmmssfff");
            }
            catch
            {
                return "19810125020000000";
            }
        }
        public static DateTime ToDateTime(string DateTimeStr)
        {
            try
            {
                return DateTime.ParseExact(DateTimeStr.Substring(0, 17), "yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return new DateTime(1981, 01, 25, 2, 00, 00);
            }
        }

    }
}
