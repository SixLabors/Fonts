// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Unicode
{
    internal readonly struct MappedBuffer<T>
    {
        private readonly BufferSlice<T> data;
        private readonly BufferSlice<int> map;

        public MappedBuffer(BufferSlice<T> data, BufferSlice<int> map)
        {
            this.data = data;
            this.map = map;
        }

        public int Length => this.map.Length;

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this.data[this.map[index]];
        }
    }
}
