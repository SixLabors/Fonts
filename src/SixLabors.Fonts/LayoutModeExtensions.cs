// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Runtime.CompilerServices;

namespace SixLabors.Fonts
{
    internal static class LayoutModeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsHorizontal(this LayoutMode mode)
            => mode is LayoutMode.HorizontalTopBottom or LayoutMode.HorizontalBottomTop;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsVertical(this LayoutMode mode)
        {
            const LayoutMode vertical =
                LayoutMode.VerticalLeftRight
                | LayoutMode.VerticalRightLeft
                | LayoutMode.VerticalMixedLeftRight
                | LayoutMode.VerticalMixedRightLeft;

            return (mode & vertical) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsVerticalMixed(this LayoutMode mode)
        {
            const LayoutMode vertical = LayoutMode.VerticalMixedLeftRight | LayoutMode.VerticalMixedRightLeft;

            return (mode & vertical) > 0;
        }
    }
}
