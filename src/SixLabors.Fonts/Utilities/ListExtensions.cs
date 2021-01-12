// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Utilities
{
    internal static class ListExtensions
    {
        public static Memory<T> EnsureLength<T>(this Memory<T> list, int value)
        {
            if (value <= list.Length)
            {
                return list.Slice(0, value);
            }

            var result = new Memory<T>(new T[value]);
            list.CopyTo(result);
            return result;
        }
    }
}
