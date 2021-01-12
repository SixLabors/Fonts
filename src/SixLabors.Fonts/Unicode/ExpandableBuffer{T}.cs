// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Unicode
{
    internal sealed class ExpandableBuffer<T>
    {
        private const int DefaultCapacity = 32;
        private T[] data;
        private int size;

        public ExpandableBuffer()
            : this(0)
        {
        }

        public ExpandableBuffer(int capacity)
        {
            Guard.MustBeGreaterThanOrEqualTo(capacity, 0, nameof(capacity));

            if (capacity == 0)
            {
                this.data = Array.Empty<T>();
            }
            else
            {
                this.data = new T[capacity];
            }
        }

        public int Length
        {
            get => this.size;

            set
            {
                if (value != this.size)
                {
                    if (value > 0)
                    {
                        this.EnsureCapacity(value);
                        this.size = value;
                    }
                    else
                    {
                        this.data = Array.Empty<T>();
                        this.size = 0;
                    }
                }
            }
        }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                DebugGuard.MustBeBetweenOrEqualTo(index, 0, this.size, nameof(index));
                if ((uint)index < (uint)this.data.Length)
                {
                    return ref this.data[index];
                }

                unsafe
                {
                    return ref Unsafe.AsRef<T>(null);
                }
            }
        }

        public BufferSlice<T> Add(int length)
        {
            int position = this.size;

            // Expand the buffer.
            this.Length += length;

            return this.Slice(position, this.Length - position);
        }

        public BufferSlice<T> Add(BufferSlice<T> value)
        {
            int position = this.size;

            // Expand the buffer.
            this.Length += value.Length;

            BufferSlice<T> slice = this.Slice(position, this.Length - position);
            value.CopyTo(slice);

            return slice;
        }

        public void Clear()
        {
            // Clear to allow GC to claim any reference types.
            this.data.AsSpan(0, this.size).Clear();
            this.size = 0;
        }

        private void EnsureCapacity(int min)
        {
            if (this.data.Length < min)
            {
                uint newCapacity = this.data.Length == 0 ? DefaultCapacity : (uint)this.data.Length * 2u;
                if (newCapacity > int.MaxValue)
                {
                    newCapacity = int.MaxValue;
                }

                var buffer = new T[newCapacity];

                if (this.size > 0)
                {
                    Array.Copy(this.data, buffer, this.size);
                }

                this.data = buffer;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferSlice<T> AsSlice() => this.Slice(this.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferSlice<T> Slice(int length)
            => new BufferSlice<T>(this.data, 0, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferSlice<T> Slice(int start, int length)
            => new BufferSlice<T>(this.data, start, length);
    }
}
