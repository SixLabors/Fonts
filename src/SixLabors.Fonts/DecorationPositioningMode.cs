// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Defines how text decorations (underline, overline, strikethrough) are positioned relative to font metrics.
/// </summary>
public enum DecorationPositioningMode
{
    /// <summary>
    /// Uses the primary (base) font's metrics for the entire run or line,
    /// ensuring a consistent decoration position across mixed fonts and scripts.
    /// Matches typical browser behavior.
    /// </summary>
    PrimaryFont = 0,

    /// <summary>
    /// Uses each glyph's own font metrics to position its decoration.
    /// Decoration positions may vary between glyphs and fallback fonts within the same line.
    /// Matches typical Microsoft Word behavior.
    /// </summary>
    GlyphFont = 1,
}
