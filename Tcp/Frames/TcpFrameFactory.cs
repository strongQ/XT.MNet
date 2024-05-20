using XT.MNet.Tcp.Interfaces;

namespace XT.MNet.Tcp.Frames;

public sealed class TcpFrameFactory : ITcpFrameFactory
{

    public ITcpFrame Create()
    {
        return new TcpFrame();
    }

}
