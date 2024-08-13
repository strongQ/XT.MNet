namespace XT.MNet.Helpers;

internal sealed class PinnedBlockMemoryPool : MemoryPool<byte>
{

    /// <summary>
    /// The size of a block. 4096 is chosen because most operating systems use 4k pages.
    /// </summary>
    private const int _BlockSize = 4096;
    private const int AnySize = -1;

    public override int MaxBufferSize { get; } = _BlockSize;
    public static int BlockSize => _BlockSize;

    private readonly ConcurrentQueue<MemoryPoolBlock> _Blocks = new();
    private bool _IsDisposed;
    private readonly object _DisposeSync = new();

    public override IMemoryOwner<byte> Rent(int size = AnySize)
    {

        if (size > _BlockSize)
        {
            throw new ArgumentOutOfRangeException(nameof(size), $"The value of {nameof(size)} cannot be greater than {_BlockSize}.");
        }

        if (_IsDisposed)
        {
            throw new ObjectDisposedException(this.GetType().FullName);
        }


        if (_Blocks.TryDequeue(out var block))
        {
            return block;
        }

        return new MemoryPoolBlock(this, BlockSize);

    }

    internal void Return(MemoryPoolBlock block)
    {

        if (!_IsDisposed)
        {
            _Blocks.Enqueue(block);
        }

    }

    protected override void Dispose(bool disposing)
    {

        if (_IsDisposed)
        {
            return;
        }

        lock (_DisposeSync)
        {

            _IsDisposed = true;

            if (!disposing)
            {
                return;
            }

            while (_Blocks.TryDequeue(out _))
            {

            }

        }

    }

}

internal sealed class MemoryPoolBlock : IMemoryOwner<byte>
{

    internal MemoryPoolBlock(PinnedBlockMemoryPool pool, int length)
    {

        Pool = pool;

        var pinnedArray = GC.AllocateUninitializedArray<byte>(length, pinned: true);
        Memory = MemoryMarshal.CreateFromPinnedArray(pinnedArray, 0, pinnedArray.Length);

    }

    public PinnedBlockMemoryPool Pool { get; }

    public Memory<byte> Memory { get; }

    public void Dispose()
    {

        Pool.Return(this);

    }

}