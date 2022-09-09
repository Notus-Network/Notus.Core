using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Notus
{
    public class Time
    {
        public static Notus.Variable.Struct.UTCTimeStruct GetNtpTime()
        {
            Notus.Variable.Struct.UTCTimeStruct tmpReturn=new Notus.Variable.Struct.UTCTimeStruct();
            tmpReturn.UtcTime = Notus.Time.GetFromNtpServer(true);
            tmpReturn.Now = DateTime.Now;
            tmpReturn.After = (tmpReturn.Now > tmpReturn.UtcTime);
            tmpReturn.Difference = (tmpReturn.After == true ? (tmpReturn.Now - tmpReturn.UtcTime ) : (tmpReturn.UtcTime - tmpReturn.Now));
            return tmpReturn;
        }
        public static Notus.Variable.Struct.UTCTimeStruct RefreshNtpTime(Notus.Variable.Struct.UTCTimeStruct currentUtcTime)
        {
            currentUtcTime.Now = DateTime.Now;
            if (currentUtcTime.After == true)
            {
                currentUtcTime.Now = DateTime.Now.Subtract(currentUtcTime.Difference);
            }
            else
            {
                currentUtcTime.Now = DateTime.Now.Add(currentUtcTime.Difference);
            }
            return currentUtcTime;
        }
        public static DateTime GetFromNtpServer(bool WaitUntilGetFromServer=false)
        {
            return GetExactTime(WaitUntilGetFromServer);
        }
        public static DateTime GetExactTime(bool WaitUntilGetFromServer=false)
        {
            if (WaitUntilGetFromServer == true)
            {
                long exactTimeLong = 0;
                int count = 0;
                while (exactTimeLong == 0)
                {
                    exactTimeLong=(long)GetExactTime_Int();
                    if (exactTimeLong == 0)
                    {
                        Thread.Sleep((count > 10 ? 5000 : 500));
                        count++;
                    }
                }
                return new DateTime(1900, 1, 1).AddMilliseconds(exactTimeLong);
            }
            return new DateTime(1900, 1, 1).AddMilliseconds((long)GetExactTime_Int());
        }
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

        private static ulong GetExactTime_UTC_SubFunc(string server)
        {
            if (string.IsNullOrEmpty(server)) throw new ArgumentException("Must be non-empty", nameof(server));

            byte[] ntpData = new byte[48];
            ntpData[0] = 0x1B;
            IPAddress[] addresses = Dns.GetHostEntry(server).AddressList;
            for (int i = 0; i < addresses.Length; i++)
            {
                try
                {
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) { ReceiveTimeout = 3000 };
                    socket.Connect(new IPEndPoint(addresses[i], 123));
                    socket.Send(ntpData);
                    socket.Receive(ntpData);
                    socket.Close();
                    ulong intPart = ((ulong)ntpData[40] << 24) | ((ulong)ntpData[41] << 16) | ((ulong)ntpData[42] << 8) | ntpData[43];
                    ulong fractPart = ((ulong)ntpData[44] << 24) | ((ulong)ntpData[45] << 16) | ((ulong)ntpData[46] << 8) | ntpData[47];
                    ulong milliseconds = intPart * 1000 + fractPart * 1000 / 0x100000000L;
                    return milliseconds;
                }
                catch(Exception err)
                {
                    Notus.Print.Log(
                        Notus.Variable.Enum.LogLevel.Info,
                        77007777,
                        err.Message,
                        "BlockRowNo",
                        null,
                        err
                    );
                }
            }
            return 0;
        }
        public static ulong GetExactTime_Int()
        {
            return GetExactTime_UTC_SubFunc("pool.ntp.org");
        }

        public static DateTime GetExactTime_DateTime()
        {
            return new DateTime(1900, 1, 1).AddMilliseconds((long)GetExactTime_Int());
        }

    }
}
