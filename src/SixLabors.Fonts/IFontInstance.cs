// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.Numerics;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents a font instance, which is a set of glyphs under a specific style (regular, italic, bold etc)
    /// </summary>
    public interface IFontInstance
    {
        /// <summary>
        /// Gets the basic descripton of the font instance type.
        /// </summary>
        FontDescription Description { get; }

        /// <summary>
        /// Gets the EM size of the font
        /// </summary>
        ushort EmSize { get; }

        /// <summary>
        /// Gets the line height
        /// </summary>
        int LineHeight { get; }

        /// <summary>
        /// Gets the ascender
        /// </summary>
        short Ascender { get; }

        /// <summary>
        /// Gets the descender
        /// </summary>
        short Descender { get; }

        /// <summary>
        /// Gets the line gap
        /// </summary>
        short LineGap { get; }

        /// <summary>
        /// Gets a specific glyph
        /// </summary>
        /// <param name="codePoint">the code point to get the glyph from</param>
        /// <returns>The glyph to find.</returns>
        GlyphInstance GetGlyph(int codePoint);

        /// <summary>
        /// Get the kerning offset that should be applied between 2 glyphs.
        /// </summary>
        /// <param name="glyph">the new glyph</param>
        /// <param name="previousGlyph">the previous glyph in the rendered font</param>
        /// <returns>Returns the offset that should be offset between the 2 glyphs</returns>
        Vector2 GetOffset(GlyphInstance glyph, GlyphInstance previousGlyph);
    }
}
