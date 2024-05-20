using XT.MNet.Tcp;
using XT.MNet.Ws.Options;

namespace XT.MNet.Ws;

public class WsServer : TcpServer
{

    public WsServer(WsServerOptions options)
        : base(options)
    {

    }

}
