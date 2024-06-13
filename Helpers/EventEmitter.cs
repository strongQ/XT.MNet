using System.Linq.Expressions;
using System.Reflection;
using XT.MNet.Tcp;
using XT.MNet.Tcp.Interfaces;

namespace XT.MNet.Helpers;

public sealed class EventEmitter(ITcpSerializer serializer)
{

    private readonly ITcpSerializer _Serializer = serializer;

    private readonly ConcurrentDictionary<string, EventDelegateAsync> _Handlers = new();



    private readonly ConcurrentDictionary<string, ServerEventDelegateAsync> _handleServerDic = new();

    private  TcpEvent _tcpEvent;

    private  ServerTcpEvent _serverTcpEvent;





    public void On(string eventName, ServerEventDelegateAsync callback)
    {


        if (!_handleServerDic.ContainsKey(eventName))
        {
            _handleServerDic.TryAdd(eventName, callback);
        }

    }

    public void On(TcpEvent callback)
    {
        _tcpEvent = callback;
    }

    public void On(ServerTcpEvent callback)
    {
        _serverTcpEvent = callback;
    }

    public void On(string eventName, EventDelegateAsync callback)
    {

        if (!_Handlers.ContainsKey(eventName))
        {
            _Handlers.TryAdd(eventName, callback);
        }

    }



    public void Emit(string eventName, ITcpFrame frame)
    {

        if (!_Handlers.TryGetValue(eventName, out var handler))
        {
            return;
        }

        handler(frame);

    }

    public void Emit(Memory<byte> datas)
    {
        _tcpEvent?.Invoke(datas);
    }

    public void ServerEmit(string eventName, ITcpFrame frame, TcpServerConnection connection)
    {

        if (!_handleServerDic.TryGetValue(eventName, out var handler))
        {
            return;
        }

        handler.Invoke(frame, connection);

    }

    public void ServerEmit(Memory<byte> datas,TcpServerConnection connection)
    {
        _serverTcpEvent?.Invoke(datas, connection);
    }

}

public delegate Task EventDelegateAsync(ITcpFrame frame);



public delegate Task ServerEventDelegateAsync(ITcpFrame frame, TcpServerConnection connection);

public delegate void ServerTcpEvent(Memory<byte> data,TcpServerConnection connection);

public delegate void TcpEvent(Memory<byte> data);


