// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Buffers;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts;

/// <summary>
/// An disposable buffer that is backed by an array pool.
/// </summary>
/// <typeparam name="T">The type of buffer element.</typeparam>
internal ref struct Buffer<T>
    where T : unmanaged
{
    private int length;
    private readonly byte[] buffer;
    private readonly Span<T> span;
    private bool isDisposed;

    public Buffer(int length)
    {
        Guard.MustBeGreaterThanOrEqualTo(length, 0, nameof(length));
        int itemSizeBytes = Unsafe.SizeOf<T>();
        int bufferSizeInBytes = length * itemSizeBytes;
        this.buffer = ArrayPool<byte>.Shared.Rent(bufferSizeInBytes);
        this.length = length;

        using ByteMemoryManager<T> manager = new(this.buffer);
        this.Memory = manager.Memory[..this.length];
        this.span = this.Memory.Span;

        this.isDisposed = false;
    }

    public Memory<T> Memory { get; }

    public readonly Span<T> GetSpan()
    {
        if (this.buffer is null)
        {
            ThrowObjectDisposedException();
        }

        return this.span;
    }

    public void Dispose()
    {
        if (this.isDisposed)
        {
            return;
        }

        ArrayPool<byte>.Shared.Return(this.buffer);
        this.length = 0;
        this.isDisposed = true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowObjectDisposedException() => throw new ObjectDisposedException("Buffer<T>");
}
