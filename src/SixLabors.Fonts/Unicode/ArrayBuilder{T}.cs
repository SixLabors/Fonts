// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// A helper type for avoiding allocations while building arrays.
    /// </summary>
    /// <typeparam name="T">The type of item contained in the array.</typeparam>
    internal struct ArrayBuilder<T>
        where T : struct
    {
        private const int DefaultCapacity = 4;
        private const int MaxCoreClrArrayLength = 0x7FeFFFFF;

        // Starts out null, initialized on first Add.
        private T[]? data;
        private int size;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayBuilder{T}"/> struct.
        /// </summary>
        /// <param name="capacity">The intitial capacity of the array.</param>
        public ArrayBuilder(int capacity)
            : this()
        {
            Guard.MustBeGreaterThanOrEqualTo(capacity, 0, nameof(capacity));

            this.data = new T[capacity];
            this.size = capacity;
        }

        /// <summary>
        /// Gets or sets the number of items in the array.
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
                        this.size = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Returns a reference to specified element of the array.
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
                if ((uint)index < (uint)this.data!.Length)
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
        /// Appends a given number of empty items to the array returning
        /// the items as a slice.
        /// </summary>
        /// <param name="length">The number of items in the slice.</param>
        /// <param name="clear">Whether to clear the new slice, Defaults to <see langword="true"/>.</param>
        /// <returns>The <see cref="ArraySlice{T}"/>.</returns>
        public ArraySlice<T> Add(int length, bool clear = true)
        {
            int position = this.size;

            // Expand the array.
            this.Length += length;

            ArraySlice<T> slice = this.Slice(position, this.Length - position);
            if (clear)
            {
                slice.Span.Clear();
            }

            return slice;
        }

        /// <summary>
        /// Appends the slice to the array copying the data across.
        /// </summary>
        /// <param name="value">The array slice.</param>
        /// <returns>The <see cref="ArraySlice{T}"/>.</returns>
        public ArraySlice<T> Add(in ArraySlice<T> value)
        {
            int position = this.size;

            // Expand the array.
            this.Length += value.Length;

            ArraySlice<T> slice = this.Slice(position, this.Length - position);
            value.CopyTo(slice);

            return slice;
        }

        /// <summary>
        /// Clears the array.
        /// Allocated memory is left intact for future usage.
        /// </summary>
        public void Clear() =>

            // No need to actually clear since we're not allowing reference types.
            this.size = 0;

        private void EnsureCapacity(int min)
        {
            int length = this.data?.Length ?? 0;
            if (length < min)
            {
                // Same expansion algorithm as List<T>.
                uint newCapacity = length == 0 ? DefaultCapacity : (uint)length * 2u;
                if (newCapacity > MaxCoreClrArrayLength)
                {
                    newCapacity = MaxCoreClrArrayLength;
                }

                if (newCapacity < min)
                {
                    newCapacity = (uint)min;
                }

                var array = new T[newCapacity];

                if (this.size > 0)
                {
                    Array.Copy(this.data, array, this.size);
                }

                this.data = array;
            }
        }

        /// <summary>
        /// Returns the current state of the array as a slice.
        /// </summary>
        /// <returns>The <see cref="ArraySlice{T}"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArraySlice<T> AsSlice() => this.Slice(this.Length);

        /// <summary>
        /// Returns the current state of the array as a slice.
        /// </summary>
        /// <param name="length">The number of items in the slice.</param>
        /// <returns>The <see cref="ArraySlice{T}"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArraySlice<T> Slice(int length)
            => new ArraySlice<T>(this.data!, 0, length);

        /// <summary>
        /// Returns the current state of the array as a slice.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of items in the slice.</param>
        /// <returns>The <see cref="ArraySlice{T}"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArraySlice<T> Slice(int start, int length)
            => new ArraySlice<T>(this.data!, start, length);
    }
}
