// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Runtime.CompilerServices;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Extensions to <see cref="LayoutMode"/>.
    /// </summary>
    public static class LayoutModeExtensions
    {
        /// <summary>
        /// Gets a value indicating whether the layout mode is horizontal.
        /// </summary>
        /// <param name="mode">The layout mode.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsHorizontal(this LayoutMode mode)
            => mode is LayoutMode.HorizontalTopBottom or LayoutMode.HorizontalBottomTop;

        /// <summary>
        /// Gets a value indicating whether the layout mode is vertical.
        /// </summary>
        /// <param name="mode">The layout mode.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsVertical(this LayoutMode mode)
            => mode is LayoutMode.VerticalLeftRight or LayoutMode.VerticalRightLeft;

        /// <summary>
        /// Gets a value indicating whether the layout mode is vertical-mixed only.
        /// </summary>
        /// <param name="mode">The layout mode.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsVerticalMixed(this LayoutMode mode)
            => mode is LayoutMode.VerticalMixedLeftRight or LayoutMode.VerticalMixedRightLeft;
    }
}
