﻿using System.Diagnostics;
using XT.MNet.Extensions;

namespace XT.MNet.Internal;

internal sealed class SocketSender(PipeScheduler ioScheduler)
    : SocketAwaitableEventArgs(ioScheduler)
{

    private List<ArraySegment<byte>>? _BufferList;

    public ValueTask<SocketOperationResult> SendAsync(Socket socket, in ReadOnlySequence<byte> buffers)
    {

        if (buffers.IsSingleSegment)
        {
            return SendAsync(socket, buffers.First);
        }

        SetBufferList(buffers);

        if (socket.SendAsync(this))
        {
            return new ValueTask<SocketOperationResult>(this, 0);
        }

        var bytesTransferred = BytesTransferred;
        var error = SocketError;

        return error == SocketError.Success
            ? new ValueTask<SocketOperationResult>(new SocketOperationResult(bytesTransferred))
            : new ValueTask<SocketOperationResult>(new SocketOperationResult(CreateException(error)));

    }

    public void Reset()
    {

        // We clear the buffer and buffer list before we put it back into the pool
        // it's a small performance hit but it removes the confusion when looking at dumps to see this still
        // holds onto the buffer when it's back in the pool
        if (BufferList != null)
        {

            BufferList = null;
            _BufferList?.Clear();

        }
        else
        {

            SetBuffer(null, 0, 0);

        }

    }

    private ValueTask<SocketOperationResult> SendAsync(Socket socket, ReadOnlyMemory<byte> memory)
    {

        SetBuffer(MemoryMarshal.AsMemory(memory));

        if (socket.SendAsync(this))
        {
            return new ValueTask<SocketOperationResult>(this, 0);
        }

        var bytesTransferred = BytesTransferred;
        var error = SocketError;

        return error == SocketError.Success
            ? new ValueTask<SocketOperationResult>(new SocketOperationResult(bytesTransferred))
            : new ValueTask<SocketOperationResult>(new SocketOperationResult(CreateException(error)));

    }

    private void SetBufferList(in ReadOnlySequence<byte> buffer)
    {

        Debug.Assert(!buffer.IsEmpty);
        Debug.Assert(!buffer.IsSingleSegment);

        if (_BufferList == null)
        {
            _BufferList = [];
        }

        foreach (var b in buffer)
        {
            _BufferList.Add(b.GetArray());
        }

        // The act of setting this list, sets the buffers in the internal buffer list
        BufferList = _BufferList;

    }

}


