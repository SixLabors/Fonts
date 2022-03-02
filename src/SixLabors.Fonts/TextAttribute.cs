// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Provides enumeration of various text attributes.
    /// </summary>
    [Flags]
    public enum TextAttribute
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
        Strikethrough = 1 << 1,

        /// <summary>
        /// The text set slightly below the normal line of type.
        /// </summary>
        Subscript = 1 << 2,

        /// <summary>
        /// The text set slightly above the normal line of type.
        /// </summary>
        Superscript = 1 << 3,
    }
}
