// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Represents overrides for the geometry of a single text decoration line, allowing a
/// <see cref="TextRun"/> to replace the values that would otherwise be derived from the font metrics.
/// </summary>
/// <remarks>
/// Values are expressed in device pixels. Any property left <see langword="null"/> falls back to the
/// font's metric-derived value. Further overrides (for example a position override) can be added as
/// new nullable properties without changing the <see cref="TextRun.GetDecorationOptions(TextDecorations)"/>
/// contract.
/// </remarks>
public class TextDecorationOptions
{
    /// <summary>
    /// Gets or sets the thickness of the decoration line, in device pixels, or <see langword="null"/>
    /// to use the font's metric-derived thickness.
    /// </summary>
    public float? Thickness { get; set; }
}
