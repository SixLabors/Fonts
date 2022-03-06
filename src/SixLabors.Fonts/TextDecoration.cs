// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Provides enumeration of various text decorations.
    /// </summary>
    [Flags]
    public enum TextDecoration
    {
        /// <summary>
        /// No attributes are applied
        /// </summary>
        None = 0,

        /// <summary>
        /// The text is underlined
        /// </summary>
        Underline = 1 << 0,

        /// <summary>
        /// The text contains a horizontal line through the center.
        /// </summary>
        Strikeout = 1 << 1,

        /// <summary>
        /// The text contains a horizontal line above it
        /// </summary>
        Overline = 1 << 2
    }
}
