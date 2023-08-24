// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Provides enumeration of various text attributes.
    /// </summary>
    [Flags]
    public enum TextAttributes
    {
        /// <summary>
        /// No attributes are applied
        /// </summary>
        None = 0,

        /// <summary>
        /// The text set slightly below the normal line of type.
        /// </summary>
        Subscript = 1 << 0,

        /// <summary>
        /// The text set slightly above the normal line of type.
        /// </summary>
        Superscript = 1 << 1,
    }
}
