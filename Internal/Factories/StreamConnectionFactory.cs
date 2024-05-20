﻿using XT.MNet.Internal;
using XT.MNet.Internal.Settings;
using XT.MNet.Tcp.Options;

namespace XT.MNet.Internal.Factories;

internal sealed class StreamConnectionFactory
    : ConnectionFactory<StreamConnectionOptions, StreamConnectionQueueSettings, StreamConnection>
{

    public StreamConnectionFactory(StreamConnectionOptions options)
        : base(options)
    {

    }

    protected override StreamConnection CreateConnection(Socket socket, Stream? stream, StreamConnectionQueueSettings settings)
    {

        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        return new StreamConnection(stream, settings.OutputOptions, settings.InputOptions);

    }

}
