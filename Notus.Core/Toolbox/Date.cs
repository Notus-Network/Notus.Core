using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Notus.Toolbox
{
    public static class Date
    {
        public static ulong ToLong(System.DateTime convertTime)
        {
            return ulong.Parse(convertTime.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText));
        }

        public static string ToString(System.DateTime DateTimeObj)
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
        public static System.DateTime ToDateTime(string DateTimeStr)
        {
            try
            {
                return System.DateTime.ParseExact(DateTimeStr.Substring(0, 17), "yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return new System.DateTime(1981, 01, 25, 2, 00, 00);
            }
        }

    }
}
