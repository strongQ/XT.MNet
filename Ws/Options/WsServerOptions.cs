using XT.MNet.Tcp.Options;

namespace XT.MNet.Ws.Options;

public sealed class WsServerOptions : TcpServerOptions
{

    public WsServerOptions()
    {

        Handshaker = new WsServerHandshaker();
        FrameFactory = new WsFrameFactory();

    }

}
