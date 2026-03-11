// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Represents a parsed CFF font containing the top-level dictionary and glyph data.
/// </summary>
internal class CffFont
{
    public CffFont(string name, CffTopDictionary metrics, CffGlyphData[] glyphs)
    {
        this.FontName = name;
        this.Metrics = metrics;
        this.Glyphs = glyphs;
    }

    /// <summary>
    /// Gets or sets the PostScript font name.
    /// </summary>
    public string FontName { get; set; }

    /// <summary>
    /// Gets or sets the Top DICT data containing font-wide metrics and properties.
    /// </summary>
    public CffTopDictionary Metrics { get; set; }

    /// <summary>
    /// Gets the array of glyph data parsed from the CharStrings INDEX.
    /// </summary>
    public CffGlyphData[] Glyphs { get; }
}
