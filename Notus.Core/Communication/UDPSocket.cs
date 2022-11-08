using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
//using NT = Notus.Time;
using ND = Notus.Date;
using NVC = Notus.Variable.Constant;
using NVG = Notus.Variable.Globals;
namespace Notus.Communication
{
    public class UDP
    {
        private Dictionary<string, double> timeOut = new Dictionary<string, double>();
        private Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private const int bufSize = 512;
        private State state = new State();
        private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback recv = null;
        private System.Action<DateTime, string>? Func_OnReceive = null;
        private bool closeOnlyListenVal = false;
        public void OnReceive(System.Action<DateTime, string> onReceive)
        {
            Func_OnReceive = onReceive;
        }
        public void CloseOnlyListen()
        {
            closeOnlyListenVal = true;
        }
        public void OnlyListen(int listenPort, System.Action<DateTime, string, string> onReceive)
        {
            UdpClient? listener = null;
            try
            {
                listener = new UdpClient(listenPort);
            }
            catch (Exception err)
            {
                //Console.WriteLine("Socket Error [SCCCCC] : " + err.Message);
            }
            if (listener != null)
            {
                IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listenPort);
                closeOnlyListenVal = false;
                string received_data;
                byte[] receive_byte_array;
                DateTime suAn = DateTime.UtcNow;
                try
                {
                    while (!closeOnlyListenVal)
                    {
                        receive_byte_array = listener.Receive(ref groupEP);
                        suAn = DateTime.UtcNow;
                        received_data = Encoding.ASCII.GetString(receive_byte_array, 0, receive_byte_array.Length);
                        onReceive(suAn, received_data, groupEP.ToString());
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine("UDP Socket Error [ASASASAS]: " + err.ToString());
                }
                listener.Close();
            }
        }
        private void Receive()
        {
            _socket.BeginReceiveFrom(state.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv = (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = _socket.EndReceiveFrom(ar, ref epFrom);
                _socket.BeginReceiveFrom(so.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv, so);
                DateTime UtcNow = DateTime.UtcNow;
                if (Func_OnReceive != null)
                {
                    string gelenZaman = Encoding.ASCII.GetString(so.buffer, 0, bytes);
                    Func_OnReceive(UtcNow, gelenZaman);
                }
            }, state);
        }
        public UDP(int port = 0)
        {
            if (port > 0)
            {
                Server("", port, true);
            }
        }
        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }
        public void Server(string address, int port, bool useIpAny = false)
        {
            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            if (useIpAny == true)
            {
                _socket.Bind(new IPEndPoint(IPAddress.Any, port));
            }
            else
            {
                _socket.Bind(new IPEndPoint(IPAddress.Parse(address), port));
            }
            Receive();
        }
        public void Client(string address, int port)
        {
            _socket.Connect(IPAddress.Parse(address), port);
            Receive();
        }
        public void Send(string text)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);
            _socket.BeginSend(data, 0, data.Length, SocketFlags.None, (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = _socket.EndSend(ar);
            }, state);
        }
    }
}