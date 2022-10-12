// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Provides a readonly mapped view of an underlying slice, selecting arbitrary indices
    /// from the source array.
    /// </summary>
    /// <typeparam name="T">The type of item contained in the underlying array.</typeparam>
    internal readonly struct ReadonlyMappedArraySlice<T>
        where T : struct
    {
        private readonly ReadOnlyArraySlice<T> data;
        private readonly ReadOnlyArraySlice<int> map;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadonlyMappedArraySlice{T}"/> struct.
        /// </summary>
        /// <param name="data">The data slice.</param>
        /// <param name="map">The map slice.</param>
        public ReadonlyMappedArraySlice(in ReadOnlyArraySlice<T> data, in ReadOnlyArraySlice<int> map)
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
        public readonly T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.data[this.map[index]];
        }
    }
}
