// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Unicode
{
    internal readonly struct BufferSlice<T>
    {
        private readonly T[] data;

        public BufferSlice(T[] data)
            : this(data, 0, data.Length)
        {
        }

        public BufferSlice(T[] data, int start, int length)
        {
            DebugGuard.MustBeGreaterThanOrEqualTo(start, 0, nameof(start));
            DebugGuard.MustBeLessThanOrEqualTo(length, data.Length, nameof(length));
            DebugGuard.MustBeLessThanOrEqualTo(start + length, data.Length, nameof(this.data));

            this.data = data;
            this.Start = start;
            this.Length = length;
        }

        /// <summary>
        /// Gets an empty <see cref="BufferSlice{T}"/>
        /// </summary>
        public static BufferSlice<T> Empty => new BufferSlice<T>(new T[0]);

        public int Start { get; }

        public int Length { get; }

        public Span<T> Span => new Span<T>(this.data, this.Start, this.Length);

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                DebugGuard.MustBeBetweenOrEqualTo(index, 0, this.Length, nameof(index));
                int i = index + this.Start;
                if ((uint)i < (uint)this.data.Length)
                {
                    return ref this.data[i];
                }

                unsafe
                {
                    return ref Unsafe.AsRef<T>(null);
                }
            }
        }

        /// <summary>
        /// Defines an implicit conversion of an array to a <see cref="BufferSlice{T}"/>
        /// </summary>
        public static implicit operator BufferSlice<T>(T[] array) => new BufferSlice<T>(array, 0, array.Length);

        public void CopyTo(BufferSlice<T> buffer) => this.Span.CopyTo(buffer.Span);

        public void Fill(T value) => this.Span.Fill(value);

        public BufferSlice<T> Slice(int start, int length) => new BufferSlice<T>(this.data, start, length);
    }
}
