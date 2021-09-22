// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts
{
    /// <summary>
    /// Defines modes to determine the layout direction of text.
    /// </summary>
    internal enum LayoutMode
    {
        /// <summary>
        /// Text is laid out horizontally from top to bottom.
        /// </summary>
        HorizontalTopBottom,

        /// <summary>
        /// Text is laid out horizontally from bottom to top.
        /// </summary>
        HorizontalBottomTop,

        /// <summary>
        /// Text is laid out vertically. Currently unsupported.
        /// </summary>
        Vertical
    }
}
