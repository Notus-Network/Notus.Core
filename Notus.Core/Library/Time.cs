/*
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ND = Notus.Date;
using NGF = Notus.Variable.Globals.Functions;
using NVG = Notus.Variable.Globals;
namespace Notus
{
    public class Time
    {
        public static Notus.Variable.Struct.UTCTimeStruct GetNtpTime(string ntpPoolServer = "pool.ntp.org")
        {
            Notus.Variable.Struct.UTCTimeStruct tmpReturn = new Notus.Variable.Struct.UTCTimeStruct();
            burası düzenlensin
            tmpReturn = FindFasterNtpServer(tmpReturn);
            return tmpReturn;
        }
        public static DateTime GetFromNtpServer(bool WaitUntilGetFromServer = false, string ntpPoolServer = "pool.ntp.org")
        {
            if (WaitUntilGetFromServer == true)
            {
                long exactTimeLong = 0;
                int count = 0;
                while (exactTimeLong == 0)
                {
                    exactTimeLong = (long)GetExactTime_UTC_SubFunc(ntpPoolServer);
                    if (exactTimeLong == 0)
                    {
                        Thread.Sleep((count > 10 ? 5000 : 500));
                        count++;
                    }
                }
                return new DateTime(1900, 1, 1).AddMilliseconds(exactTimeLong);
            }
            return new DateTime(1900, 1, 1).AddMilliseconds((long)GetExactTime_UTC_SubFunc(ntpPoolServer));
        }
        */
        /*
        public static ulong NowToUlong(bool milisecondIncluded = true)
        {
            if (milisecondIncluded == true)
            {
                return ulong.Parse(
                    NVG.NOW.Obj.ToString(
                        Notus.Variable.Constant.DefaultDateTimeFormatText
                    )
                );
            }
            return ulong.Parse(
                NVG.NOW.Obj.ToString(
                    Notus.Variable.Constant.DefaultDateTimeFormatText.Substring(0, 14)
                )
            );
        }
        public static ulong DateTimeToUlong(DateTime ConvertTime)
        {
            return ulong.Parse(
                ConvertTime.ToString(
                    Notus.Variable.Constant.DefaultDateTimeFormatText
                )
            );
        }

        public static ulong DateTimeToUlong(DateTime ConvertTime, bool milisecondIncluded)
        {
            if (milisecondIncluded == true)
            {
                return DateTimeToUlong(ConvertTime);
            }
            return ulong.Parse(
                ConvertTime.ToString(
                    Notus.Variable.Constant.DefaultDateTimeFormatTextWithourMiliSecond
                )
            );
        }

        public static Notus.Variable.Struct.UTCTimeStruct FindFasterNtpServer(Notus.Variable.Struct.UTCTimeStruct utcVar)
        {
            string[] serverNameList = {
                "pool.ntp.org",
                "africa.pool.ntp.org",
                "asia.pool.ntp.org",
                "europe.pool.ntp.org",
                "north-america.pool.ntp.org",
                "oceania.pool.ntp.org",
                "south-america.pool.ntp.org"
            };
            TimeSpan timeDiff = new TimeSpan(1, 0, 0, 0);
            DateTime startTime;
            DateTime finishTime;
            foreach (string serverName in serverNameList)
            {
                byte[] ntpData = new byte[48];
                ntpData[0] = 0x1B;
                IPAddress[] addresses = Dns.GetHostEntry(serverName).AddressList;
                bool itsDone = false;
                for (int i = 0; i < addresses.Length && itsDone == false; i++)
                {
                    try
                    {
                        if (itsDone == false)
                        {
                            IPEndPoint ipEndpoint = new IPEndPoint(addresses[i], 123);
                            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                            socket.ReceiveTimeout = 3000;

                            socket.Connect(ipEndpoint);
                            socket.Send(ntpData);

                            startTime = NVG.NOW.Obj;
                            socket.Receive(ntpData);
                            finishTime = NVG.NOW.Obj;
                            socket.Close();
                            ulong intPart = ((ulong)ntpData[40] << 24) | ((ulong)ntpData[41] << 16) | ((ulong)ntpData[42] << 8) | ntpData[43];
                            ulong fractPart = ((ulong)ntpData[44] << 24) | ((ulong)ntpData[45] << 16) | ((ulong)ntpData[46] << 8) | ntpData[47];
                            ulong milliseconds = intPart * 1000 + fractPart * 1000 / 0x100000000L;
                            TimeSpan ts = finishTime - startTime;
                            if (timeDiff > ts)
                            {
                                utcVar.UtcTime = new DateTime(1900, 1, 1).AddMilliseconds(milliseconds).Subtract(ts);
                                utcVar.Now = finishTime;
                                utcVar.pingTime = ts;
                                utcVar.ulongUtc = ND.ToLong(utcVar.UtcTime);
                                utcVar.ulongNow = Notus.Time.DateTimeToUlong(utcVar.Now);
                                utcVar.After = (utcVar.Now > utcVar.UtcTime);
                                utcVar.Difference = (utcVar.After == true ? (utcVar.Now - utcVar.UtcTime) : (utcVar.UtcTime - utcVar.Now));
                                //buradaki difference olayını kontrol et ve node senkronizasyonlarını kontrol et
                                utcVar.Difference = new TimeSpan(0);
                                utcVar.PingServerUrl = serverName;
                            }
                            itsDone = true;
                        }
                    }
                    catch { }
                }
            }
            return utcVar;
        }
        public static ulong GetExactTime_UTC_SubFunc(string server)
        {
            byte[] ntpData = new byte[48];
            ntpData[0] = 0x1B;
            IPAddress[] addresses = Dns.GetHostEntry(server).AddressList;
            for (int i = 0; i < addresses.Length; i++)
            {
                try
                {
                    IPEndPoint ipEndpoint = new IPEndPoint(addresses[i], 123);
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    socket.ReceiveTimeout = 3000;
                    socket.Connect(ipEndpoint);
                    socket.Send(ntpData);
                    socket.Receive(ntpData);
                    socket.Close();
                    ulong intPart = ((ulong)ntpData[40] << 24) | ((ulong)ntpData[41] << 16) | ((ulong)ntpData[42] << 8) | ntpData[43];
                    ulong fractPart = ((ulong)ntpData[44] << 24) | ((ulong)ntpData[45] << 16) | ((ulong)ntpData[46] << 8) | ntpData[47];
                    ulong milliseconds = intPart * 1000 + fractPart * 1000 / 0x100000000L;
                    return milliseconds;
                }
                catch { }
            }
            return 0;
        }
    }
}
*/
