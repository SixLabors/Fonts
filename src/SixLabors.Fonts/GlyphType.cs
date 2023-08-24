// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents the various versions of a glyph records.
    /// </summary>
    public enum GlyphType
    {
        /// <summary>
        /// This is a fall back glyph due to a missing code point.
        /// </summary>
        Fallback,

        /// <summary>
        /// This is a standard glyph to be drawn in the style the user defines.
        /// </summary>
        Standard,

        /// <summary>
        /// This is a single layer of the multi-layer colored glyph (emoji).
        /// </summary>
        ColrLayer
    }
}
