using XT.MNet.Ws;
using XT.MNet.Tcp.Interfaces;

namespace XT.MNet.Ws;

public sealed class WsFrameFactory : ITcpFrameFactory
{

    public ITcpFrame Create()
    {
        return new WsFrame();
    }

}
