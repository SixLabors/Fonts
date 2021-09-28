// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Defines modes to determine the layout direction of text.
    /// </summary>
    [Flags]
    public enum LayoutMode
    {
        /// <summary>
        /// Text is laid out horizontally from top to bottom.
        /// </summary>
        HorizontalTopBottom = 0,

        /// <summary>
        /// Text is laid out horizontally from bottom to top.
        /// </summary>
        HorizontalBottomTop = 1 << 0,

        /// <summary>
        /// Text is laid out vertically from left to right.
        /// </summary>
        VerticalLeftRight = 1 << 1,

        /// <summary>
        /// Text is laid out vertically from right to left.
        /// </summary>
        VerticalRightLeft = 1 << 2
    }
}
