namespace XT.MNet.Internal;

internal class SocketAwaitableEventArgs
        : SocketAsyncEventArgs, IValueTaskSource<SocketOperationResult>
{

    private static readonly Action<object?> _ContinuationCompleted = _ => { };

    private readonly PipeScheduler _IoScheduler;

    private Action<object?>? _Continuation;

    public SocketAwaitableEventArgs(PipeScheduler ioScheduler):base(true)
    {
        _IoScheduler = ioScheduler;
    }

    protected override void OnCompleted(SocketAsyncEventArgs _)
    {

        var c = _Continuation;

        if (c != null || (c = Interlocked.CompareExchange(ref _Continuation, _ContinuationCompleted, null)) != null)
        {

            var continuationState = UserToken;
            UserToken = null;
            _Continuation = _ContinuationCompleted; // in case someone's polling IsCompleted

            _IoScheduler.Schedule(c, continuationState);

        }

    }

    public SocketOperationResult GetResult(short token)
    {

        _Continuation = null;

        if (SocketError != SocketError.Success)
        {
            return new SocketOperationResult(CreateException(SocketError));
        }

        return new SocketOperationResult(BytesTransferred);

    }

    protected static SocketException CreateException(SocketError e)
    {

        return new SocketException((int)e);

    }

    public ValueTaskSourceStatus GetStatus(short token)
    {

        return !ReferenceEquals(_Continuation, _ContinuationCompleted) ? ValueTaskSourceStatus.Pending :
                SocketError == SocketError.Success ? ValueTaskSourceStatus.Succeeded :
                ValueTaskSourceStatus.Faulted;

    }

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {

        UserToken = state;
        var prevContinuation = Interlocked.CompareExchange(ref _Continuation, continuation, null);

        if (ReferenceEquals(prevContinuation, _ContinuationCompleted))
        {

            UserToken = null;
            ThreadPool.UnsafeQueueUserWorkItem(continuation, state, preferLocal: true);

        }

    }

}

