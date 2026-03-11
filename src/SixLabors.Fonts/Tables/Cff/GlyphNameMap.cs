// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Maps a glyph index to its PostScript glyph name from the CFF charset.
/// </summary>
internal readonly struct GlyphNameMap
{
    /// <summary>
    /// The glyph index within the font.
    /// </summary>
    public readonly ushort GlyphIndex;

    /// <summary>
    /// The PostScript glyph name.
    /// </summary>
    public readonly string GlyphName;

    public GlyphNameMap(ushort glyphIndex, string glyphName)
    {
        this.GlyphIndex = glyphIndex;
        this.GlyphName = glyphName;
    }
}
