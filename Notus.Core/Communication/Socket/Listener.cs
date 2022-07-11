using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
namespace Notus.Communication
{
    public class Listener : IDisposable
    {
        private class SocketStateObject
        {
            public const int BufferSize = 4194304;
            public byte[] buffer = new byte[BufferSize];
            public System.Text.StringBuilder sb = new System.Text.StringBuilder();
            public System.Net.Sockets.Socket workSocket = null;
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

        private bool exitFromLoop = false;

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

        private bool Val_ReturnByteArray = false;
        public bool ReturnByteArray
        {
            get
            {
                return Val_ReturnByteArray;
            }
            set
            {
                Val_ReturnByteArray = value;
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

        private int ListenerCountVal = 10000;
        public int ListenerCount
        {
            get
            {
                return ListenerCountVal;
            }
            set
            {
                ListenerCountVal = value;
            }
        }

        private int SendBufferSizeVal = 2097152;
        public int SendBufferSize
        {
            get
            {
                return SendBufferSizeVal;
            }
            set
            {
                SendBufferSizeVal = value;
            }
        }

        private int SendTimeOutVal = 1000;
        public int SendTimeOut
        {
            get
            {
                return SendTimeOutVal;
            }
            set
            {
                SendTimeOutVal = value;
            }
        }

        private bool OnErrorFunctionDefined = false;
        private System.Action<int, string> OnErrorFunctionObj = null;

        private bool OnReceiveFunctionDefined = false;
        private System.Func<string, System.Net.IPEndPoint, System.Net.IPEndPoint, string> NewReceiveFunctionObj = null;
        private System.Func<byte[], System.Net.IPEndPoint, System.Net.IPEndPoint, byte[]> NewReceiveFunctionObj_ReturnByteArray = null;
        private string CommIpAddress = "";
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

        private bool ScktDataEndTextIsActive = true;
        public bool DataEndTextIsActive
        {
            get
            {
                return ScktDataEndTextIsActive;
            }
            set
            {
                ScktDataEndTextIsActive = value;
            }
        }
        private bool SynchronousSocketIsActive_Val = true;
        public bool SynchronousSocketIsActive
        {
            get
            {
                return SynchronousSocketIsActive_Val;
            }
            set
            {
                SynchronousSocketIsActive_Val = value;
            }
        }
        System.Net.Sockets.Socket ListenTcpObj;
        private Notus.Threads.Thread ThreadObj = new Notus.Threads.Thread();
        private System.Threading.ManualResetEvent AllDoneObj = new System.Threading.ManualResetEvent(false);
        public void OnError(System.Action<int, string> OnErrorFunc)
        {
            OnErrorFunctionObj = OnErrorFunc;
            OnErrorFunctionDefined = true;
        }
        public void OnReceive(System.Func<string, System.Net.IPEndPoint, System.Net.IPEndPoint,string> OnReceiveFunc)
        {
            NewReceiveFunctionObj = OnReceiveFunc;
            OnReceiveFunctionDefined = true;
        }
        public void OnReceive(System.Func<byte[], System.Net.IPEndPoint, System.Net.IPEndPoint, byte[]> OnReceiveFunc)
        {
            NewReceiveFunctionObj_ReturnByteArray = OnReceiveFunc;
            OnReceiveFunctionDefined = true;
        }
        private void SendDummyData()
        {
            try
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(
                    new IPEndPoint(
                        System.Net.IPAddress.Parse(CommIpAddress),
                        CommPortNo
                    )
                );
                socket.Send(System.Text.Encoding.ASCII.GetBytes("<ping>"));
                socket.Disconnect(false);
                socket.Close();
            }
            catch (System.Net.Sockets.SocketException err)
            {
                Notus.Toolbox.Print.Basic(DebugModeActive, "Error text [ 9870 ] : " + err.Message);
            }
            catch (Exception err)
            {
                Notus.Toolbox.Print.Basic(DebugModeActive, "Error text [ 9870 ] : " + err.Message);
            }
        }

        public void Stop()
        {
            bool tmpDebugMode = DebugModeActive;
            DebugModeActive = false;
            exitFromLoop = true;
            OnErrorFunctionDefined = false;
            OnReceiveFunctionDefined = false;
            SendDummyData();
            DateTime JustRightNow = DateTime.Now.AddSeconds(1);
            while (DateTime.Now > JustRightNow)
            {

            }
            try
            {
                if (ListenTcpObj != null)
                {
                    ListenTcpObj.Dispose();
                }
            }
            catch { }

            try
            {
                if (AllDoneObj != null)
                {
                    AllDoneObj.Reset();
                }
            }
            catch { }

            try
            {
                if (AllDoneObj!=null)
                {
                    AllDoneObj.Close();
                }
            }
            catch { }

            DebugModeActive = tmpDebugMode;
        }
        public void Begin(bool WithNewThread = true)
        {
            System.Net.IPAddress ipAddress;
            if (System.Net.IPAddress.TryParse(CommIpAddress, out _) == true)
            {
                ipAddress = System.Net.IPAddress.Parse(CommIpAddress);
            }
            else
            {
                //ipAddress = System.Net.IPAddress.IPv6Any;
                ipAddress = System.Net.IPAddress.Any;
            }
            System.Net.IPEndPoint localEndPoint = new System.Net.IPEndPoint(ipAddress, CommPortNo);

            ListenTcpObj = new System.Net.Sockets.Socket(
                ipAddress.AddressFamily,
                System.Net.Sockets.SocketType.Stream,
                System.Net.Sockets.ProtocolType.Tcp
            );

            try
            {
                ListenTcpObj.Bind(localEndPoint);
                ListenTcpObj.Listen(ListenerCountVal);
            }
            catch (System.Net.Sockets.SocketException err)
            {
                Notus.Toolbox.Print.Basic(DebugModeActive, err.Message);
                if (OnErrorFunctionDefined == true)
                {
                    OnErrorFunctionObj(3568, err.ToString());
                }
            }
            catch (Exception err)
            {
                Notus.Toolbox.Print.Basic(DebugModeActive, err.Message);
                if (OnErrorFunctionDefined == true)
                {
                    OnErrorFunctionObj(3568, err.ToString());
                }
            }

            if (SynchronousSocketIsActive_Val == false)
            {
                if (WithNewThread == true)
                {
                    ThreadObj.Start(() =>
                    {
                        while (exitFromLoop == false)
                        {
                            try
                            {
                                AllDoneObj.Reset();
                                if (ListenTcpObj != null)
                                {
                                    ListenTcpObj.BeginAccept(new System.AsyncCallback(AcceptCallback), ListenTcpObj);
                                }
                                AllDoneObj.WaitOne();
                            }
                            catch (System.Net.Sockets.SocketException err)
                            {
                                Notus.Toolbox.Print.Basic(DebugModeActive, err.Message);
                                if (OnErrorFunctionDefined == true)
                                {
                                    OnErrorFunctionObj(97864, err.ToString());
                                }
                            }
                            catch (Exception err)
                            {
                                Notus.Toolbox.Print.Basic(DebugModeActive, err.Message);
                                if (OnErrorFunctionDefined == true)
                                {
                                    OnErrorFunctionObj(129064, err.ToString());
                                }
                            }
                        }
                    });
                }
                else
                {
                    while (exitFromLoop == false)
                    {
                        try
                        {
                            AllDoneObj.Reset();
                            ListenTcpObj.BeginAccept(new System.AsyncCallback(AcceptCallback), ListenTcpObj);
                            AllDoneObj.WaitOne();
                        }
                        catch (System.Net.Sockets.SocketException err)
                        {
                            Notus.Toolbox.Print.Basic(DebugModeActive, err.Message);

                            if (OnErrorFunctionDefined == true)
                            {
                                OnErrorFunctionObj(4586, err.ToString());
                            }
                        }
                        catch (Exception err)
                        {
                            Notus.Toolbox.Print.Basic(DebugModeActive, err.Message);

                            if (OnErrorFunctionDefined == true)
                            {
                                OnErrorFunctionObj(4586, err.ToString());
                            }
                        }
                    }
                    ListenTcpObj.Dispose();
                }
            }
            else
            {
                ThreadObj.Start(() =>
                {
                    byte[] scktBuffer = new byte[65536];
                    while (exitFromLoop == false)
                    {
                        bool responseToClient = false;
                        Socket handler = null;
                        try
                        {
                            handler = ListenTcpObj.Accept();
                            System.Net.IPEndPoint remoteIpEndPoint = handler.RemoteEndPoint as System.Net.IPEndPoint;
                            System.Net.IPEndPoint localIpEndPoint = handler.LocalEndPoint as System.Net.IPEndPoint;
                            int bytesRead = 0;
                            string tmpIncomeData = string.Empty;
                            while (true)
                            {
                                bytesRead = handler.Receive(scktBuffer,0, scktBuffer.Length, SocketFlags.None,out SocketError errorCode);
                                if (errorCode == SocketError.Success)
                                {
                                    if (ScktDataEndTextIsActive == true)
                                    {
                                        tmpIncomeData += System.Text.Encoding.ASCII.GetString(scktBuffer, 0, bytesRead);
                                        if (tmpIncomeData.IndexOf(ScktDataEndText) > -1)
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        tmpIncomeData += System.Text.Encoding.ASCII.GetString(scktBuffer, 0, bytesRead);
                                        break;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }

                            if (Val_ReturnByteArray == true)
                            {
                                try
                                {
                                    if (OnReceiveFunctionDefined == true)
                                    {
                                        byte[] resultArray = new byte[bytesRead];
                                        Array.Copy(scktBuffer, resultArray, bytesRead);
                                        handler.Send(NewReceiveFunctionObj_ReturnByteArray(resultArray, remoteIpEndPoint, localIpEndPoint));
                                    }
                                    else
                                    {
                                        handler.Send(new byte[] { 1, 1, 1, 1 });
                                    }
                                }
                                catch (System.Net.Sockets.SocketException err4)
                                {
                                    Notus.Toolbox.Print.Basic(DebugModeActive, err4.Message);
                                    if (OnErrorFunctionDefined == true)
                                    {
                                        OnErrorFunctionObj(88556325, err4.ToString());
                                    }
                                    else
                                    {
                                        responseToClient = true;
                                    }
                                }
                                catch (Exception err6)
                                {
                                    Notus.Toolbox.Print.Basic(DebugModeActive, err6.Message);
                                    if (OnErrorFunctionDefined == true)
                                    {
                                        OnErrorFunctionObj(532197753, err6.ToString());
                                    }
                                    else
                                    {
                                        responseToClient = true;
                                    }
                                }
                            }


                            if (Val_ReturnByteArray == false)
                            {
                                if (ScktDataEndTextIsActive == true)
                                {
                                    try
                                    {
                                        if (OnReceiveFunctionDefined == true)
                                        {
                                            
                                            if (string.Compare(tmpIncomeData, "<ping>") == 0)
                                            {
                                                handler.Send(Encoding.UTF8.GetBytes(NewReceiveFunctionObj("<ping>", remoteIpEndPoint, localIpEndPoint)));
                                            }
                                            else
                                            {
                                                handler.Send(Encoding.UTF8.GetBytes(NewReceiveFunctionObj(tmpIncomeData, remoteIpEndPoint, localIpEndPoint)));
                                            }
                                        }
                                        else
                                        {
                                            handler.Send(Encoding.UTF8.GetBytes("<ok>"));
                                        }
                                    }
                                    catch (Exception err2)
                                    {
                                        Notus.Toolbox.Print.Basic(DebugModeActive, err2.Message);
                                        if (OnErrorFunctionDefined == true)
                                        {
                                            OnErrorFunctionObj(22165, err2.ToString());
                                        }
                                        else
                                        {
                                            responseToClient = true;
                                        }
                                    }
                                }
                                else
                                {
                                    byte[] resultArray = new byte[bytesRead];
                                    Array.Copy(scktBuffer, resultArray, bytesRead);
                                    handler.Send(resultArray);
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            Notus.Toolbox.Print.Basic(DebugModeActive, err.Message);
                            if (OnErrorFunctionDefined == true)
                            {
                                OnErrorFunctionObj(09817, err.ToString());
                            }
                            else
                            {
                                responseToClient = true;
                            }
                        }

                        if (handler != null)
                        {
                            if (responseToClient == true)
                            {
                                handler.Send(System.Text.Encoding.ASCII.GetBytes("false-test"));
                            }

                            try
                            {
                                handler.Shutdown(SocketShutdown.Both);
                                handler.Close();
                            }
                            catch
                            { }
                        }
                    }
                    /*
                    while (exitFromLoop == false)
                    {
                        try
                        {
                            AllDoneObj.Reset();
                            if (ListenTcpObj != null)
                            {
                                ListenTcpObj.BeginAccept(new System.AsyncCallback(AcceptCallback), ListenTcpObj);
                            }
                            AllDoneObj.WaitOne();
                        }
                        catch (System.Net.Sockets.SocketException err)
                        {
                            Notus.Toolbox.Print.Basic(DebugModeActive, err.Message);
                            if (OnErrorFunctionDefined == true)
                            {
                                OnErrorFunctionObj(220983, err.ToString());
                            }
                        }
                        catch (Exception err)
                        {
                            Notus.Toolbox.Print.Basic(DebugModeActive, err.Message);
                            if (OnErrorFunctionDefined == true)
                            {
                                OnErrorFunctionObj(0987536, err.ToString());
                            }
                        }
                    }
                    */

                });
            }
        }
        private void AcceptCallback(IAsyncResult ar)
        {
            bool tmpObjectDisposed = false;
            AllDoneObj.Set();
            System.Net.Sockets.Socket listener = (System.Net.Sockets.Socket)ar.AsyncState;
            System.Net.Sockets.Socket handler;
            try
            {
                handler = listener.EndAccept(ar);
            }
            catch (ObjectDisposedException)
            {
                tmpObjectDisposed = true;
                return;
            }

            if (tmpObjectDisposed == false)
            {
                SocketStateObject state = new SocketStateObject();
                state.workSocket = handler;
                handler.BeginReceive(state.buffer, 0, SocketStateObject.BufferSize, 0, new System.AsyncCallback(ReadCallback), state);
            }
        }
        private void ReadCallback(IAsyncResult ar)
        {
            SocketStateObject state = (SocketStateObject)ar.AsyncState;
            System.Net.Sockets.Socket handler = state.workSocket;
            SocketError errorCode;
            System.Net.IPEndPoint remoteIpEndPoint = handler.RemoteEndPoint as System.Net.IPEndPoint;
            System.Net.IPEndPoint localIpEndPoint = handler.LocalEndPoint as System.Net.IPEndPoint;

            int bytesRead = handler.EndReceive(ar,out errorCode);
            if (bytesRead > 0 && errorCode == SocketError.Success)
            {
                if (Val_ReturnByteArray == true)
                {
                    try
                    {
                        if (OnReceiveFunctionDefined == true)
                        {
                            byte[] resultArray = new byte[bytesRead];
                            Array.Copy(state.buffer, resultArray, bytesRead);
                            Send_ByteArray(handler, NewReceiveFunctionObj_ReturnByteArray(resultArray, remoteIpEndPoint, localIpEndPoint));
                        }
                        else
                        {
                            byte[] resultArray = new byte[] { 1, 1, 1, 1 };
                            Send_ByteArray(handler, resultArray);
                        }
                    }
                    catch (System.Net.Sockets.SocketException err)
                    {
                        Notus.Toolbox.Print.Basic(DebugModeActive, err.Message);

                        if (OnErrorFunctionDefined == true)
                        {
                            OnErrorFunctionObj(88556325, err.ToString());
                        }
                    }
                    catch (Exception err)
                    {
                        Notus.Toolbox.Print.Basic(DebugModeActive, err.Message);

                        if (OnErrorFunctionDefined == true)
                        {
                            OnErrorFunctionObj(63298714, err.ToString());
                        }
                    }
                }
                else
                {
                    String content = String.Empty;
                    state.sb.Append(Encoding.UTF8.GetString(state.buffer, 0, bytesRead));
                    content = state.sb.ToString();
                    bool processIncomeData = true;
                    if (ScktDataEndTextIsActive == true)
                    {
                        if (content.IndexOf(ScktDataEndText) == -1)
                        {
                            processIncomeData = false;
                        }
                    }
                    if (processIncomeData == true)
                    {
                        try
                        {
                            if (OnReceiveFunctionDefined == true)
                            {
                                content = content.Substring(0, content.Length - ScktDataEndText.Length);
                                if (string.Compare(content, "<ping>") == 0)
                                {
                                    Send(handler, NewReceiveFunctionObj("<ping>", remoteIpEndPoint, localIpEndPoint));
                                }
                                else
                                {
                                    Send(handler, NewReceiveFunctionObj(content, remoteIpEndPoint, localIpEndPoint));
                                }
                            }
                            else
                            {
                                Send(handler, "<ok>");
                            }
                        }
                        catch (Exception err)
                        {
                            Notus.Toolbox.Print.Basic(DebugModeActive, err.Message);
                            if (OnErrorFunctionDefined == true)
                            {
                                OnErrorFunctionObj(22165, err.ToString());
                            }
                        }
                    }
                    else
                    {
                        handler.BeginReceive(state.buffer, 0, SocketStateObject.BufferSize, 0, new System.AsyncCallback(ReadCallback), state);
                    }
                }
            }
        }
        private void Send(System.Net.Sockets.Socket handler, String data)
        {
            Send_ByteArray(handler, Encoding.UTF8.GetBytes(data + ScktDataEndText));
        }
        private void Send_ByteArray(System.Net.Sockets.Socket handler, byte[] byteData)
        {
            handler.SendBufferSize = SendBufferSizeVal;
            handler.BeginSend(byteData, 0, byteData.Length, 0, new System.AsyncCallback(SendCallback), handler);
        }
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                System.Net.Sockets.Socket handler = (System.Net.Sockets.Socket)ar.AsyncState;
                handler.SendBufferSize = SendBufferSizeVal;
                handler.SendTimeout = SendTimeOutVal;
                int bytesSent = handler.EndSend(ar);
                if (KeepConnectionAlive == false)
                {
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
            }
            catch (Exception err)
            {
                Notus.Toolbox.Print.Basic(DebugModeActive, err.Message);
                if (OnErrorFunctionDefined == true)
                {
                    OnErrorFunctionObj(5326, err.ToString());
                }
            }
        }
        public Listener()
        {
            CommPortNo = PortNo;
        }
        public Listener(int PortNo)
        {
            CommPortNo = PortNo;
            CommIpAddress = "";
        }
        public Listener(int PortNo, string IPAddress)
        {
            CommPortNo = PortNo;
            CommIpAddress = IPAddress;
        }
        public Listener(int PortNo, string IPAddress, System.Func<string, System.Net.IPEndPoint, System.Net.IPEndPoint, string> OnReceiveFunc, bool WithNewThread = true)
        {
            CommPortNo = PortNo;
            CommIpAddress = IPAddress;
            NewReceiveFunctionObj = OnReceiveFunc;
            OnReceiveFunctionDefined = true;
            Begin(WithNewThread);
        }
        public Listener(int PortNo, string IPAddress, System.Func<string, System.Net.IPEndPoint, System.Net.IPEndPoint,string> OnReceiveFunc, System.Action<int, string> OnErrorFunc, bool WithNewThread = true)
        {
            CommPortNo = PortNo;
            CommIpAddress = IPAddress;
            NewReceiveFunctionObj = OnReceiveFunc;
            OnReceiveFunctionDefined = true;

            OnErrorFunctionDefined = true;
            OnErrorFunctionObj = OnErrorFunc;
            Begin(WithNewThread);
        }

        public void Dispose()
        {
            exitFromLoop = true;
        }
        ~Listener() { }
    }
}
