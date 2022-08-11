// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts
{
    /// <summary>
    /// An disposable buffer that is backed by an array pool.
    /// </summary>
    /// <typeparam name="T">The type of buffer element.</typeparam>
    internal ref struct Buffer<T>
        where T : struct
    {
        private int length;
        private readonly byte[] buffer;
        private readonly Memory<T> memory;
        private readonly Span<T> span;
        private bool isDisposed;

        public Buffer(int length)
        {
            Guard.MustBeGreaterThanOrEqualTo(length, 0, nameof(length));
            int itemSizeBytes = Unsafe.SizeOf<T>();
            int bufferSizeInBytes = length * itemSizeBytes;
            this.buffer = ArrayPool<byte>.Shared.Rent(bufferSizeInBytes);
            this.length = length;

            ByteMemoryManager<T> manager = new(this.buffer);
            this.memory = manager.Memory.Slice(0, this.length);
            this.span = this.memory.Span;

            this.isDisposed = false;
        }

        /// <summary>
        /// Gets an array slice over the buffer.
        /// Do not allow this to escape the scope of the parent struct!!
        /// </summary>
        /// <returns>The <see cref="ArraySlice{T}"/>.</returns>
        public ArraySlice<T> DangerousGetSlice()
        {
            if (this.buffer is null)
            {
                ThrowObjectDisposedException();
            }

            return new ArraySlice<T>(Unsafe.As<T[]>(this.buffer), 0, this.length);
        }

        public Span<T> GetSpan()
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
}
