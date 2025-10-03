// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Controls how text decorations (underline/overline/strikethrough) are positioned.
/// </summary>
public enum DecorationPositioningMode
{
    /// <summary>
    /// Word-like: position each decoration using the active glyph's own font metrics
    /// Varies across glyphs/fallback faces within the same line.
    /// </summary>
    PerGlyphFromFont = 0,

    /// <summary>
    /// Browser-like: Use the primary (base) font's metrics for the entire run/line, producing a single,
    /// consistent decoration position across mixed fonts and scripts.
    /// </summary>
    FromPrimaryFont = 1,
}
