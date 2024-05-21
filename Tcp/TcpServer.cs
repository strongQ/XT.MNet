using System;
using XT.MNet.Helpers;
using XT.MNet.Tcp.Interfaces;
using XT.MNet.Tcp.Options;

namespace XT.MNet.Tcp;

public class TcpServer : TcpBase, IDisposable
{

    public TcpServerOptions Options { get; private set; }

    public int ConnectionCount
    {
        get
        {
            return _Connections.Count;
        }
    }

    public delegate void ConnectHandler(TcpServerConnection connection);
    public event ConnectHandler? OnConnect;

    public delegate void DisconnectHandler(TcpServerConnection connection);
    public event DisconnectHandler? OnDisconnect;

    private readonly ConcurrentDictionary<string, TcpServerConnection> _Connections;

    public TcpServer(TcpServerOptions options)
    {

        if (options.IsSecure)
        {
            ArgumentNullException.ThrowIfNull(options.Certificate, nameof(options.Certificate));
        }

        if (!IPAddress.TryParse(options.Address, out var address))
        {
            throw new ArgumentException($"{nameof(options.Address)} is not a valid IP address.");
        }

        Options = options;
        EndPoint = new IPEndPoint(address, options.Port);

        Logger = Options.Logger;
        _Connections = new ConcurrentDictionary<string, TcpServerConnection>();

        EventEmitter = new EventEmitter(Options.Serializer);

        InitFactory();

    }

    public void Start()
    {

        if (RunTokenSource != null)
        {
            return;
        }

        Logger.LogDebug("{Source} Starting the tcp server...", this);

        RunTokenSource = new CancellationTokenSource();
        BindSocket();

        var _ = DoAccept(RunTokenSource.Token);

        Logger.LogInformation("{Source} Server was started. {Endpoint}", this, EndPoint);

    }

    public void Stop()
    {

        if (RunTokenSource == null)
        {
            return;
        }

        Logger.LogDebug("{Source} Stopping the tcp server...", this);

        _Connections.Clear();

        RunTokenSource?.Cancel();
        RunTokenSource?.Dispose();

        RunTokenSource = null;

        try
        {

            Socket?.Shutdown(SocketShutdown.Both);

        }
        catch (Exception)
        {

        }

        Socket?.Dispose();
        Socket = null;

        Logger.LogInformation("{Source} Server was stopped. {Endpoint}", this, EndPoint);

    }



    public void Broadcast(string uid, Memory<byte> payload)
    {

        foreach (var connection in _Connections.Values)
        {

            connection.Send(uid, payload);

        }

    }

    public void Broadcast(Memory<byte> payload)
    {

        foreach (var connection in _Connections.Values)
        {

            connection.Send(payload);

        }

    }



    public void On(string identifier, ServerEventDelegateAsync handler)
    {
        InternalOn(identifier, handler);
    }

    public void On(ServerTcpEvent handle)
    {
        EventEmitter.On(handle);
    }

    private void InternalOn(string identifier, ServerEventDelegateAsync handler)
    {
        EventEmitter.On(identifier, handler);
    }

    private async Task DoAccept(CancellationToken token)
    {

        while (!token.IsCancellationRequested)
        {

            var connection = await Accept(token);
            if (connection == null) return;

            Logger.LogDebug("{Source} New connection {identifier}", this, connection.UniqueId);

            var _ = DoReceive(connection, token);
            _ = DoSend(connection, token);

        }

    }

    private async ValueTask<TcpServerConnection?> Accept(CancellationToken token = default)
    {

        while (!token.IsCancellationRequested && Socket != null)
        {

            try
            {

                var socket = await Socket.AcceptAsync(token);
                socket.NoDelay = true;

                if (ConnectionType == TcpUnderlyingConnectionType.FastSocket)
                {

                    return new TcpServerConnection()
                    {
                        DuplexPipe = ConnectionFactory.Create(socket, null),
                        Server = this,
                        Socket = socket,
                        UniqueId = RandomUtils.RandomString(TcpConstants.UniqueIdLength),
                    };

                }
                else
                {

                    var stream = await GetStream(socket);
                    if (stream == null)
                    {
                        continue;
                    }

                    return new TcpServerConnection()
                    {
                        DuplexPipe = ConnectionFactory.Create(socket, stream),
                        Server = this,
                        Socket = socket,
                        UniqueId = RandomUtils.RandomString(TcpConstants.UniqueIdLength),
                    };

                }

            }
            catch (ObjectDisposedException)
            {

                return null;

            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.OperationAborted)
            {

                return null;

            }
            catch (Exception)
            {

                // The connection got reset while it was in the backlog, so we try again.

            }

        }

        return null;

    }



    private async Task DoReceive(TcpServerConnection connection, CancellationToken token)
    {

        var frame = Options.FrameFactory.Create();
        var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(connection.DisconnectToken.Token, token);

        try
        {

            Options.Handshaker.StartHandshake(connection);

            while (!token.IsCancellationRequested && !connection.DisconnectToken.IsCancellationRequested)
            {

                var result = await connection.DuplexPipe.Input.ReadAsync(combinedToken.Token);

                try
                {
                    var position = ParseFrame(connection, ref frame, ref result);

                    connection.DuplexPipe.Input.AdvanceTo(position);
                }
                catch (Exception ex)
                {
                    int a = 0;
                }

                if (result.IsCanceled || result.IsCompleted)
                {
                    break;
                }

            }

        }
        catch (Exception err)
        {

            Logger.LogDebug("{Source} DoReceive error on connection {UniqueId}, {Error}", this, connection.UniqueId, err);

        }
        finally
        {

            // disconnect

            frame?.Dispose();
            await connection.DisposeAsync();

            try
            {
                combinedToken?.Dispose();
            }
            catch { }

            if (connection.UniqueId != null && _Connections.TryRemove(connection.UniqueId, out _))
            {
                OnDisconnect?.Invoke(connection);
            }

        }

    }

    //private SequencePosition ParseFrame(TcpServerConnection connection, ref ITcpFrame frame, ref ReadResult result)
    //{

    //    var buffer = result.Buffer;

    //    if (buffer.Length == 0)
    //    {
    //        return buffer.Start;
    //    }

    //    if (connection.IsHandshaked)
    //    {

    //        var endPosition = frame.Read(ref buffer);

    //        if (frame.Identifier != null)
    //        {

    //            EventEmitter.ServerEmit(frame.Identifier, frame, connection);

    //            frame.Dispose(); // just disposes internal variables, no need to worry
    //            frame = Options.FrameFactory.Create();

    //        }


    //        return endPosition;

    //    }
    //    else if (Options.Handshaker.Handshake(connection, ref buffer, out var headerPosition))
    //    {

    //        connection.IsHandshaked = true;
    //        InsertNewConnection(connection);

    //        Logger.LogDebug("{Source} New connection handshaked {identifier}", this, connection.UniqueId);
    //        OnConnect?.Invoke(connection);

    //        return headerPosition;

    //    }
    //    else if (buffer.Length > Options.MaxHandshakeSizeBytes)
    //    {

    //        throw new Exception($"Handshake not valid and exceeded {nameof(Options.MaxHandshakeSizeBytes)}.");

    //    }

    //    return buffer.End;





    //}
    ReadOnlySequence<byte> _store = ReadOnlySequence<byte>.Empty;
    private SequencePosition ParseFrame(TcpServerConnection connection, ref ITcpFrame frame, ref ReadResult result)
    {
        var buffer = result.Buffer;

        if (!_store.IsEmpty)
        {
            var combined = new ReadOnlySequence<byte>(_store.ToArray().Concat(buffer.ToArray()).ToArray());
            buffer = combined;

        }

        // 检查缓冲区是否为空
        if (buffer.Length == 0)
        {
            return buffer.Start;
        }

        // 如果已经完成握手
        if (connection.IsHandshaked)
        {
            if (Options.SpecialChar != null)
            {
                // 使用SequenceReader来处理buffer
                SequenceReader<byte> reader = new SequenceReader<byte>(buffer);

                // 查找特殊字符（例如'#'）
                if (reader.TryAdvanceTo((byte)Options.SpecialChar, advancePastDelimiter: true))
                {
                    // 处理消息
                    var consumed = reader.Consumed;
                    var messageBuffer = buffer.Slice(0, consumed - 1);
                    EventEmitter.ServerEmit(messageBuffer.FirstSpan, connection);

                    if (!_store.IsEmpty)
                    {
                        consumed = consumed - _store.Length;
                        _store = ReadOnlySequence<byte>.Empty; // 清空_store
                    }

                    // 返回消息结束后的位置
                    return result.Buffer.GetPosition(consumed);

                }
                else
                {
                    // 如果没有找到特殊字符，则将buffer存储到_store中，以便下次处理
                    _store = buffer;
                }
            }
            else
            {
                return ProcessMessage(connection, ref frame, ref buffer);
            }
        }
        else if (Options.Handshaker.Handshake(connection, ref buffer, out var headerPosition))
        {
            // 握手逻辑...
            connection.IsHandshaked = true;
            InsertNewConnection(connection);

            Logger.LogDebug("{Source} New connection handshaked {identifier}", this, connection.UniqueId);
            OnConnect?.Invoke(connection);

            return headerPosition;
        }
        else if (buffer.Length > Options.MaxHandshakeSizeBytes)
        {
            // 握手数据过大异常处理
            throw new Exception($"Handshake not valid and exceeded {nameof(Options.MaxHandshakeSizeBytes)}.");
        }

        // 如果没有找到帧的结束标志，返回缓冲区的末尾
        return buffer.End;
    }

    private SequencePosition ProcessMessage(TcpServerConnection connection, ref ITcpFrame frame, ref ReadOnlySequence<byte> buffer)
    {
        var endPosition = frame.Read(ref buffer);

        if (frame.Identifier != null)
        {

            EventEmitter.ServerEmit(frame.Identifier, frame, connection);

            frame.Dispose(); // just disposes internal variables, no need to worry
            frame = Options.FrameFactory.Create();

        }
        return endPosition;
    }



    private void InsertNewConnection(TcpServerConnection connection)
    {

        while (!_Connections.TryAdd(connection.UniqueId, connection))
        {

            connection.UniqueId = RandomUtils.RandomString(TcpConstants.UniqueIdLength);

        }

    }

    private async Task DoSend(TcpServerConnection connection, CancellationToken token)
    {

        try
        {

            while (!token.IsCancellationRequested
                && await connection.OutgoingFramesQueue.Reader.WaitToReadAsync(token))
            {

                try
                {

                    var frame = await connection.OutgoingFramesQueue.Reader.ReadAsync(token);

                    if (frame.Data.Length == 0)
                    {
                        continue;
                    }

                    if (frame.IsRawOnly)
                    {

                        await connection.DuplexPipe.Output.WriteAsync(frame.Data, token); // flushes automatically

                    }
                    else
                    {

                        SendFrame(connection, frame);
                        await connection.DuplexPipe.Output.FlushAsync(token); // not flushed inside frame

                    }

                }
                catch (Exception)
                {

                }

            }

        }
        catch (Exception)
        {

        }

    }

    private void SendFrame(TcpServerConnection connection, ITcpFrame frame)
    {

        int binarySize = frame.GetBinarySize();

        if (binarySize <= TcpConstants.SafeStackBufferSize)
        {

            Span<byte> span = stackalloc byte[binarySize];
            frame.Write(ref span);

            connection.DuplexPipe.Output.Write(span);

        }
        else
        {

            using var memoryOwner = MemoryPool<byte>.Shared.Rent(binarySize); // gives back memory at end of scope
            Span<byte> span = memoryOwner.Memory.Span[..binarySize]; // rent memory can be bigger, clamp it

            frame.Write(ref span);

            connection.DuplexPipe.Output.Write(span);

        }

    }

    private void BindSocket()
    {

        Socket listenSocket;
        try
        {

            listenSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

            listenSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            listenSocket.NoDelay = true;

            listenSocket.Bind(EndPoint);

            Logger.LogDebug("{Source} Binding to following endpoint {Endpoint}", this, EndPoint);

        }
        catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {

            throw new Exception(e.Message, e);

        }

        Socket = listenSocket;
        listenSocket.Listen();

    }

    private void InitFactory()
    {

        TcpUnderlyingConnectionType type = TcpUnderlyingConnectionType.NetworkStream;

        if (Options.ConnectionType != TcpUnderlyingConnectionType.Unset)
        {

            Logger.LogDebug("{Source} Underlying connection type overwritten, initialising with: {Type}", this, Options.ConnectionType);
            type = Options.ConnectionType;

        }
        else
        {

            if (Options.IsSecure)
            {
                type = TcpUnderlyingConnectionType.SslStream;
            }
            else
            {
                type = TcpUnderlyingConnectionType.FastSocket;
            }

            Logger.LogDebug("{Source} Underlying connection type chosen automatically: {Type}", this, type);

        }

        CreateFactory(type);

    }

    private void CreateFactory(TcpUnderlyingConnectionType type)
    {

        ConnectionType = type;

        switch (ConnectionType)
        {

            case TcpUnderlyingConnectionType.FastSocket:
                ConnectionFactory = new SocketConnectionFactory(Options.SocketConnectionOptions);
                break;

            case TcpUnderlyingConnectionType.SslStream:
                ConnectionFactory = new StreamConnectionFactory(Options.StreamConnectionOptions);
                break;

            case TcpUnderlyingConnectionType.NetworkStream:
                ConnectionFactory = new StreamConnectionFactory(Options.StreamConnectionOptions);
                break;

        }

    }

    private async Task<Stream?> GetStream(Socket socket)
    {

        NetworkStream stream = new(socket);

        if (ConnectionType == TcpUnderlyingConnectionType.NetworkStream)
        {
            return stream;
        }

        SslStream? sslStream = null;

        try
        {

            sslStream = new SslStream(stream, false);

            var task = sslStream.AuthenticateAsServerAsync(Options.Certificate!, false, SslProtocols.None, true);
            await task.WaitAsync(TimeSpan.FromSeconds(30));

            return sslStream;

        }
        catch (Exception err)
        {

            sslStream?.Dispose();

            if (err is TimeoutException)
            {
                Logger.LogDebug("{Source} Ssl handshake timeouted.", this);
            }

            Logger.LogDebug("{Source} Certification fail get stream. {Error}", this, err);
            return null;

        }

    }

    public void Dispose()
    {

        ConnectionFactory?.Dispose();

    }

}
