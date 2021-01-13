// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Provides a mapped view of an underlying slice, selecting arbitrary indicies
    /// from the source array.
    /// </summary>
    /// <typeparam name="T">The type of item contained in the underlying array.</typeparam>
    internal readonly struct MappedBuffer<T>
        where T : struct
    {
        private readonly BufferSlice<T> data;
        private readonly BufferSlice<int> map;

        /// <summary>
        /// Initializes a new instance of the <see cref="MappedBuffer{T}"/> struct.
        /// </summary>
        /// <param name="data">The data slice.</param>
        /// <param name="map">The map slice.</param>
        public MappedBuffer(in BufferSlice<T> data, in BufferSlice<int> map)
        {
            Guard.MustBeGreaterThanOrEqualTo(data.Length, map.Length, nameof(map));

            this.data = data;
            this.map = map;
        }

        /// <summary>
        /// Gets the number of items in the map.
        /// </summary>
        public int Length => this.map.Length;

        /// <summary>
        /// Returns a reference to specified element of the slice.
        /// </summary>
        /// <param name="index">The index of the element to return.</param>
        /// <returns>The <typeparamref name="T"/>.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to <see cref="Length"/>.
        /// </exception>
        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this.data[this.map[index]];
        }
    }
}
