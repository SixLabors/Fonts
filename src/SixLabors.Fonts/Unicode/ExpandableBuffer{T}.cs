// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// A data buffer of <typeparamref name="T"/> that can be expanded to allow
    /// for the addition of new data.
    /// </summary>
    /// <typeparam name="T">The type of item contained in the buffer.</typeparam>
    internal sealed class ExpandableBuffer<T>
        where T : struct
    {
        private const int DefaultCapacity = 32;
        private T[] data;
        private int size;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpandableBuffer{T}"/> class.
        /// </summary>
        public ExpandableBuffer()
            : this(0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpandableBuffer{T}"/> class.
        /// </summary>
        /// <param name="capacity">The intitial capacity of the buffer.</param>
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

        /// <summary>
        /// Gets or sets the number of items in the buffer.
        /// </summary>
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

        /// <summary>
        /// Returns a reference to specified element of the buffer.
        /// </summary>
        /// <param name="index">The index of the element to return.</param>
        /// <returns>The <typeparamref name="T"/>.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to <see cref="Length"/>.
        /// </exception>
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

        /// <summary>
        /// Appends a given number of empty items to the buffer returning
        /// the items as a slice.
        /// </summary>
        /// <param name="length">The number of items in the slice.</param>
        /// <returns>The <see cref="BufferSlice{T}"/>.</returns>
        public BufferSlice<T> Add(int length)
        {
            int position = this.size;

            // Expand the buffer.
            this.Length += length;

            return this.Slice(position, this.Length - position);
        }

        /// <summary>
        /// Appends the slice to the buffer copying the data across.
        /// </summary>
        /// <param name="value">The buffer slice.</param>
        /// <returns>The <see cref="BufferSlice{T}"/>.</returns>
        public BufferSlice<T> Add(in BufferSlice<T> value)
        {
            int position = this.size;

            // Expand the buffer.
            this.Length += value.Length;

            BufferSlice<T> slice = this.Slice(position, this.Length - position);
            value.CopyTo(slice);

            return slice;
        }

        /// <summary>
        /// Clears the buffer.
        /// Allocated memory is left intact for future usage.
        /// </summary>
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
                // Same expansion algorithm as List<T>.
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

        /// <summary>
        /// Returns the current state of the buffer as a slice.
        /// </summary>
        /// <returns>The <see cref="BufferSlice{T}"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferSlice<T> AsSlice() => this.Slice(this.Length);

        /// <summary>
        /// Returns the current state of the buffer as a slice.
        /// </summary>
        /// <param name="length">The number of items in the slice.</param>
        /// <returns>The <see cref="BufferSlice{T}"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferSlice<T> Slice(int length)
            => new BufferSlice<T>(this.data, 0, length);

        /// <summary>
        /// Returns the current state of the buffer as a slice.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of items in the slice.</param>
        /// <returns>The <see cref="BufferSlice{T}"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferSlice<T> Slice(int start, int length)
            => new BufferSlice<T>(this.data, start, length);
    }
}
