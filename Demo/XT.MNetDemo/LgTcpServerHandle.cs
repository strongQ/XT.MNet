using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.VisualBasic;

using System.Drawing;
using System.Drawing.Imaging;


namespace XT.MNetDemo;

/// <summary>
/// 服务错误数据
/// </summary>
public class HandleErrorEventArgs
{
    /// <summary>
    /// 错误信息
    /// </summary>
    public string Error { get; set; }
    /// <summary>
    /// 方法
    /// </summary>

    public string Method { get; set; }
}

public enum LgTcpState
{
    Closed,
    Open,
    Listening,
    ConnectionPending,
    ResolvingHost,
    HostResolved,
    Connecting,
    Connected,
    Closing,
    Error
}
/// <summary>
/// Lg tcp服务
/// </summary>
public class LgTcpServerHandle
{
    // 缓存区
    private byte[] _buffer=new byte[1024];
    private byte[] _byteBuffer=new byte[1024];
    private List<byte[]> _bufferCol = new List<byte[]>();
    /// <summary>
    /// 连接的客户端
    /// </summary>
    private Socket _client;
    /// <summary>
    /// 监听的服务端
    /// </summary>
    private Socket _server;
   
    /// <summary>
    /// 错误事件
    /// </summary>
    public event EventHandler<HandleErrorEventArgs> ErrorEvent;
    /// <summary>
    /// 状态改变事件
    /// </summary>
    public event EventHandler<LgTcpState> StateChangeEvent;
    /// <summary>
    /// 客户端数据接收事件
    /// </summary>
    public event EventHandler<Byte[]> RecievedEvent;
    /// <summary>
    /// 服务端来自客户端的连接请求事件
    /// </summary>
    public event EventHandler<Socket> ConnectionRequestEvent;




    private LgTcpState _state;
    /// <summary>
    /// 服务状态
    /// </summary>
    public LgTcpState State
    {
        get { return _state; }
        set 
        { 
           if(_state != value )
            {
                StateChangeEvent?.Invoke(this, value);
            }
            _state = value;
        }
    }

    /// <summary>
    /// 端口
    /// </summary>
    public int Port { get; set; }
    /// <summary>
    /// IP
    /// </summary>
    public string IP { get; set; }


    #region 客户端
    /// <summary>
    /// 启动服务
    /// </summary>
    public bool StartConnect(string ip,int port)
    {
        IP = ip;
        Port = port;
        if (this.State != LgTcpState.Connected && this.State != LgTcpState.Listening)
        {

            try
            {
                State = LgTcpState.ResolvingHost;

                bool result = IPAddress.TryParse(IP, out IPAddress realip);
                if (!result)
                {
                    ErrorEvent?.Invoke(this, new HandleErrorEventArgs
                    {
                        Error = "IP Parse Error",
                        Method = "StartConnect"
                    });
                    return false;
                }
                State = LgTcpState.HostResolved;

                _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint pEndPoint = new IPEndPoint(realip, this.Port);
                State = LgTcpState.Connecting;
                // 异步开始连接
                _client.BeginConnect(pEndPoint, new AsyncCallback(ClientConnected), null);

                return true;
            }
            catch (Exception ex)
            {
                ErrorEvent?.Invoke(this, new HandleErrorEventArgs
                {
                    Error = ex.Message,
                    Method = "StartConnect"
                });
                return false;
            }
        }
        else
        {
            ErrorEvent?.Invoke(this, new HandleErrorEventArgs
            {
                Error = "Socket Client is already start",
                Method = "StartConnect"
            });

            return false;

        }
    }
    /// <summary>
    /// 添加到缓存区
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="count"></param>
    private void AddToBuffer(byte[] bytes, int count)
    {

        int num = _buffer.Length==0 ? -1 : _buffer.Length;
        int num1 = num + count;
        byte[] newBuffer = new byte[num1 + 1];
        Array.Copy(_buffer, newBuffer, _buffer.Length);
        _buffer = newBuffer;
        Array.Copy(bytes, 0, _buffer, num + 1, count);
        // 以数字4结尾
        byte num2 = 4;
        int i = Array.IndexOf<byte>(_buffer, num2);
        while (i != -1)
        {
            // Your logic here
            int length = _buffer.Length - (i + 1);
            byte[] numArray = new byte[i];
            byte[] numArray1 = new byte[length];
            Array.Copy(_buffer, 0, numArray, 0, i);
            Array.Copy(_buffer, i + 1, numArray1, 0, length);
            _buffer = new byte[numArray1.Length + 1];
            Array.Copy(numArray1, this._buffer, numArray1.Length);
            _bufferCol.Add(numArray);
            if (_bufferCol.Count > 10)
            {
                _bufferCol.RemoveAt(0);
            }
            RecievedEvent?.Invoke(this, numArray);
            // Move to the next index after num2
            i = Array.IndexOf<byte>(_buffer, num2, i + 1);
        }

        if (count < bytes.Length - 1 && _buffer.Length > 0)
        {
            _bufferCol.Add(_buffer);
            RecievedEvent?.Invoke(this, _buffer);
            _buffer = new byte[1024];
        }
    }
    /// <summary>
    /// 开始读取接受到的数据
    /// </summary>
    /// <param name="ar"></param>
    private void DoRead(IAsyncResult ar)
    {

        try
        {
            int num = _client.EndReceive(ar);
            if (num >= 1)
            {
                this.AddToBuffer(this._byteBuffer, num);
                Array.Clear(this._byteBuffer, 0, num);

                _client.BeginReceive(this._byteBuffer, 0, 1024, SocketFlags.None, new AsyncCallback(DoRead), null);
            }
            else
            {
                this.CloseClient();
                _byteBuffer = new byte[1025];

                return;
            }
        }
        catch (Exception ex)
        {
            this.CloseClient();
            _byteBuffer = new byte[1025];
            ErrorEvent?.Invoke(this, new HandleErrorEventArgs
            {
                Error = ex.Message,
                Method = "DoRead"
            });
        }
    }
    /// <summary>
    /// 关闭客户端
    /// </summary>
    public void CloseClient()
    {

        if (State == LgTcpState.Connected)
        {
            _client.Close();
            _client = null;
        }
        if (_server != null)
        {
            State = LgTcpState.Listening;
        }
        else
        State = LgTcpState.Closed;
    }

    /// <summary>
    /// 客户端已经连接
    /// </summary>
    /// <param name="asyn"></param>
    private void ClientConnected(IAsyncResult asyn)
    {
        try
        {
            _client.EndConnect(asyn);
            State = LgTcpState.Connected;

            _client.BeginReceive(this._byteBuffer, 0, 1024, SocketFlags.None, new AsyncCallback(DoRead), null);
        }
        catch (Exception ex)
        {
            ErrorEvent?.Invoke(this, new HandleErrorEventArgs
            {
                Error = ex.Message,
                Method = "ClientConnected"
            });
        }
    }
    /// <summary>
    /// socket接入
    /// </summary>
    /// <param name="request"></param>
    public void AddAccept(Socket request)
    {     
        try
        {
            State=LgTcpState.ConnectionPending;
            _client = request;
          
            State=LgTcpState.Connected;
            
            _client.BeginReceive(_byteBuffer, 0, 1024, SocketFlags.None, new AsyncCallback(DoReceive), null);          
        }
        catch (Exception ex)
        {
            ErrorEvent?.Invoke(this, new HandleErrorEventArgs
            {
                Error = ex.Message,
                Method = "AddAccept"
            });
           
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ar"></param>
    private void DoReceive(IAsyncResult ar)
    {
        int num;

        try
        {
            lock (_client)
            {
                num = _client.EndReceive(ar);

                if (num >= 1)
                {
                    AddToBuffer(this._byteBuffer, num);
                    Array.Clear(this._byteBuffer, 0, num);


                    _client.BeginReceive(this._byteBuffer, 0, 1024, SocketFlags.None, new AsyncCallback(DoReceive), null);

                }
                else
                {
                    CloseClient();
                    _byteBuffer = new byte[1025];
                   
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            
            this.CloseClient();
            this._byteBuffer = new byte[1025];
            ErrorEvent?.Invoke(this, new HandleErrorEventArgs
            {
                Error = ex.Message,
                Method = "DoRecieve"
            });
        }
    }
    #endregion


    #region 服务端
    /// <summary>
    /// 启动监听
    /// </summary>
    /// <param name="ip"></param>
    /// <param name="port"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<bool> StartListen(string ip,int port,CancellationToken token)
    {
        IP = ip;
        Port = port;
        return await Task.Run(DoListen,token);
    }
    /// <summary>
    /// 接受到客户端
    /// </summary>
    private void ClientAccept(IAsyncResult result)
    {
        try
        {
            Socket socket = this._server.EndAccept(result);
            if (this.State == LgTcpState.Listening)
            {
              
                ConnectionRequestEvent?.Invoke(this,socket);                   
            }
            else
            {
                _client.Disconnect(true);
                _client.Close();
                ConnectionRequestEvent?.Invoke(this, socket);
                //socket.Disconnect(true);
            }
           
         

            _server.BeginAccept(new AsyncCallback(ClientAccept), null);
        }
        catch (Exception ex)
        {         
            CloseServer();
            ErrorEvent?.Invoke(this, new HandleErrorEventArgs
            {
                Error = ex.Message,
                Method = "ClientAccept"
            });
        }
    }
    /// <summary>
    /// 关闭服务端
    /// </summary>
    public void CloseServer()
    {


        _server.Close();
        
        State = LgTcpState.Closed;
    }
    /// <summary>
    /// 服务端监听
    /// </summary>
    private bool DoListen()
    {
        try
        {
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint pEndPoint = new IPEndPoint(IPAddress.Any, Port);
            _server.Bind(pEndPoint);
            _server.Listen(1);
            State= LgTcpState.Listening;
         
            _server.BeginAccept(new AsyncCallback(ClientAccept),null);

            return true;
        }
        catch (Exception ex)
        {
           
            this.CloseServer();
            State=LgTcpState.Closed;

            ErrorEvent?.Invoke(this, new HandleErrorEventArgs
            {
                Error = ex.Message,
                Method = "DoListen"
            });

            return false;
        }
    }
    #endregion


    #region 读取
    /// <summary>
    /// 读取数据，返回字符串
    /// </summary>
    /// <returns></returns>
    public string ReadStrData()
    {
        byte[] numArray = ReadData();
        
        int num = numArray.Length;
        string data = string.Empty;
        for (int i = 0; i <= num; i++)
        {
            if (numArray[i] != 10)
            {
                data = string.Concat(data, Strings.ChrW((int)numArray[i]));
            }
            else
            {
                data = string.Concat(data, "\n");
            }
        }
        return data;
    }
    /// <summary>
    /// 读取数据
    /// </summary>
    /// <returns></returns>
    public byte[] ReadData()
    {
        byte[] item = _bufferCol.FirstOrDefault();
        if (item == null) return [];
        _bufferCol.RemoveAt(0);
        var bytes = new byte[item.Length + 1];
        item.CopyTo(bytes, 0);
        return bytes;
    }
  




    #endregion

    #region 写入
    /// <summary>
    /// 发送数据
    /// </summary>
    /// <param name="Data"></param>
    /// <returns></returns>
    public async Task<bool> Send(string Data)
    {
        return await Send(Encoding.ASCII.GetBytes(Data));
    }
    /// <summary>
    /// 发送数据
    /// </summary>
    /// <param name="Data"></param>
    /// <returns></returns>
    public async Task<bool> Send(byte[] Data)
    {
        if (State == LgTcpState.Connected)
        {
            try
            {
               if(_client!=null)
               await _client.SendAsync(Data);
               return true;
            }
            catch (Exception ex)
            {
               
               
                CloseClient();
                ErrorEvent?.Invoke(this, new HandleErrorEventArgs
                {
                    Error = ex.Message,
                    Method = "Send"
                });
            }         
        }
        return false;
    }
   

    #endregion




}
