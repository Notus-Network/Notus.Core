using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
namespace Notus.Communication
{
     public class Sender : IDisposable
    {
        private bool DebugModeActive = false;
        public bool DebugMode
        {
            get
            {
                return DebugModeActive;
            }
            set
            {
                DebugModeActive = value;
            }
        }

        private int CommPortNo = 0;
        public int PortNo
        {
            get
            {
                return CommPortNo;
            }
            set
            {
                CommPortNo = value;
            }
        }

        private bool KeepConnectionAlive = false;
        public bool KeepAlive
        {
            get
            {
                return KeepConnectionAlive;
            }
            set
            {
                KeepConnectionAlive = value;
            }
        }

        private int SendBufferSize = 8192;
        public int BufferSize
        {
            get
            {
                return SendBufferSize;
            }
            set
            {
                SendBufferSize = value;
            }
        }

        private string ScktDataEndText = Notus.Variable.Constant.SocketMessageEndingText;
        public string DataEndText
        {
            get
            {
                return ScktDataEndText;
            }
            set
            {
                ScktDataEndText = value;
            }
        }

        private System.Net.Sockets.Socket SocObj;

        private string CommIpAddress = "";
        public string ErrorText;
        public string IPAddress
        {
            get
            {
                return CommIpAddress;
            }
            set
            {
                CommIpAddress = value;
            }
        }
        public (bool, string) Send(string socData)
        {
            try
            {
                SocObj.Send(System.Text.Encoding.UTF8.GetBytes(socData + ScktDataEndText));
                byte[] buffer = new byte[SendBufferSize];
                int iRx = SocObj.Receive(buffer);
                char[] chars = new char[iRx];
                System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                int charLen = d.GetChars(buffer, 0, iRx, chars, 0);
                string resultStr = new System.String(chars);
                return (true, resultStr.Substring(0, resultStr.Length - ScktDataEndText.Length));
            }
            catch (Exception err)
            {
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Info,
                    700088554,
                    err.Message,
                    "BlockRowNo",
                    null,
                    err
                );
                Notus.Print.Basic(DebugModeActive, err.Message);
                ErrorText = err.Message;
            }
            return (false, ErrorText);
        }
        public bool Connect(int PortNo, string IPAddress)
        {
            System.Net.IPAddress ipAddress;
            CommPortNo = PortNo;
            CommIpAddress = IPAddress;
            if (System.Net.IPAddress.TryParse(CommIpAddress, out _) == true)
            {
                ipAddress = System.Net.IPAddress.Parse(CommIpAddress);
                try
                {
                    SocObj = new System.Net.Sockets.Socket(
                        System.Net.Sockets.AddressFamily.InterNetwork,
                        System.Net.Sockets.SocketType.Stream,
                        System.Net.Sockets.ProtocolType.Tcp
                    );
                    SocObj.Connect(new System.Net.IPEndPoint(ipAddress, PortNo));
                    return true;
                }
                catch (Exception err)
                {
                    Notus.Print.Log(
                        Notus.Variable.Enum.LogLevel.Info,
                        900000004,
                        err.Message,
                        "BlockRowNo",
                        null,
                        err
                    );

                    Notus.Print.Basic(DebugModeActive, err.Message);
                    ErrorText = err.Message;
                }
            }
            return false;
        }

        public Sender()
        {
        }
        public Sender(int bufferSize)
        {
            if (bufferSize > 0)
            {
                SendBufferSize = bufferSize;
            }
        }
        public void Dispose()
        {
            SocObj.Shutdown(SocketShutdown.Both);
            SocObj.Close();
            SocObj.Dispose();
        }
        ~Sender() { }
    }

}
