// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        private bool isDisposed;

        public Buffer(int length)
        {
            Guard.MustBeGreaterThanOrEqualTo(length, 0, nameof(length));
            int itemSizeBytes = Unsafe.SizeOf<T>();
            int bufferSizeInBytes = length * itemSizeBytes;
            this.buffer = ArrayPool<byte>.Shared.Rent(bufferSizeInBytes);
            this.length = length;
            this.isDisposed = false;
        }

        public Span<T> GetSpan()
        {
            if (this.buffer is null)
            {
                ThrowObjectDisposedException();
            }
#if SUPPORTS_CREATESPAN
            ref byte r0 = ref MemoryMarshal.GetReference<byte>(this.buffer);
            return MemoryMarshal.CreateSpan(ref Unsafe.As<byte, T>(ref r0), this.length);
#else
            return MemoryMarshal.Cast<byte, T>(this.buffer).Slice(0, this.length);
#endif
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
