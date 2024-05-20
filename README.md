
# XT.MNet TCP Server/Client

[NuGet](https://www.nuget.org/packages/MNet)


Fork From [MarvinDrude](https://github.com/MarvinDrude/MNet)

Just a small lightweight library for TCP Communication in .NET/C#. It utilizes some techniques from internal
kestrel sockets for some performance benefits. Some notes on things used:


## Remarks on used technologies

- Uses some "stackalloc" and "MemoryPool<byte>.Shared" for less heap allocations
- Usage of "System.IO.Pipelines" for better buffering
- Custom schedulers for Tasks
- Custom "PinnedBlockMemoryPool" from kestrel
- Expression Compilation for better performance of events
- For Secure Ssl falls back to SslStream under the hood, but still amazingly fast

## Simple Usage
You should always first register all the event handlers before calling Server.Start/Client.Connect, in order for you to not miss any instant messages.

### Creation of a TCP Server
```csharp
var server = new TcpServer(new TcpServerOptions() {
    Address = "127.0.0.1", 
    Port = 43434,
    Logger = debugLogger, // ILogger of your liking, default is just console one
    SpecialChar='#' // only work for raw data
  
});
server.Start();
```

#### Connect / Disconnect (Connect event is after successful handshake)
```csharp
server.OnConnect += (connection) => {
    ...
};

server.OnDisconnect += (connection) => {
    ...
};
```

#### Register event handler for raw bytes messages
```csharp
server.On("test-bytes", (buffer, connection) => {

    // important, will only work by using ReadOnlyMemory<byte> here, not byte[], Memory<byte> etc.
    Console.WriteLine("Length: " + buffer.Length);

    // send a raw bytes message (important for sending must be of type Memory<byte>)
    connection.Send("test-bytes", new Memory<byte>([0, 2, 3, 5]));

});
```

#### Register Server Raw Data
```csharp
server.On((buffer, connection) => {

    Console.WriteLine($" {DateTime.Now} Recieve: {Encoding.UTF8.GetString(buffer)} ");


});
```



### Creation of a TCP Client
```csharp
var client = new TcpClient(new TcpClientOptions() {
    Address = "127.0.0.1",
    Port = 43434,
    Logger = debugLogger, // ILogger of your liking, default is just console one
   
});
client.Connect();
```

#### Connect / Disconnect (Connect event is after successful handshake)
```csharp
client.OnConnect += () => {
    ...
};

client.OnDisconnect += () => {
    ...
};
```

#### Register event handler for raw bytes messages
```csharp
client.On("test-bytes", (buffer) => {

    // important, will only work by using ReadOnlyMemory<byte> here, not byte[], Memory<byte> etc.
    Console.WriteLine("Length: " + buffer.Length);

    // send a raw bytes message (important for sending must be of type Memory<byte>)
    client.Send("test-bytes", new Memory<byte>([0, 2, 3, 5]));

});
```

#### Register Raw
```csharp

client.On(data =>
{
    var str = Encoding.UTF8.GetString(data);
    Console.WriteLine($"{DateTime.Now} {str}");
});
```


