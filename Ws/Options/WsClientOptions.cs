using XT.MNet.Tcp.Options;

namespace XT.MNet.Ws.Options;

public sealed class WsClientOptions : TcpClientOptions
{

    public WsClientOptions()
    {

        Handshaker = new WsClientHandshaker();
        FrameFactory = new WsFrameFactory();

    }

}
