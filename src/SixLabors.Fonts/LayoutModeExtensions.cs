// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Runtime.CompilerServices;

namespace SixLabors.Fonts
{
    internal static class LayoutModeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsVertical(this LayoutMode mode)
        {
            const LayoutMode vertical = LayoutMode.VerticalLeftRight | LayoutMode.VerticalRightLeft;
            return (mode & vertical) > 0;
        }
    }
}
