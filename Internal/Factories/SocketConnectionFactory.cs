using XT.MNet.Internal;
using XT.MNet.Internal.Settings;
using XT.MNet.Tcp.Options;

namespace XT.MNet.Internal.Factories;

internal sealed class SocketConnectionFactory
    : ConnectionFactory<SocketConnectionOptions, SocketConnectionQueueSettings, SocketConnection>
{

    public SocketConnectionFactory(SocketConnectionOptions options)
        : base(options)
    {

    }

    protected override SocketConnection CreateConnection(Socket socket, Stream? stream, SocketConnectionQueueSettings settings)
    {

        return new SocketConnection(
            socket, settings.MemoryPool,
            settings.SocketSenderPool.Scheduler,
            settings.SocketSenderPool,
            settings.OutputOptions,
            settings.InputOptions);

    }

}
