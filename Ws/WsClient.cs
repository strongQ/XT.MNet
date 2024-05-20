using XT.MNet.Ws.Options;
using TcpClient = XT.MNet.Tcp.TcpClient;

namespace XT.MNet.Ws;

public class WsClient : TcpClient
{

    public WsClient(WsClientOptions options)
        : base(options)
    {

    }

}
