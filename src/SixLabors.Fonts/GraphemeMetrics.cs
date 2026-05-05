// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Represents one coalesced grapheme in final layout order.
/// </summary>
public readonly struct GraphemeMetrics
{
    internal GraphemeMetrics(
        FontRectangle advance,
        FontRectangle bounds,
        FontRectangle renderableBounds,
        int graphemeIndex,
        int stringIndex)
    {
        this.Advance = advance;
        this.Bounds = bounds;
        this.RenderableBounds = renderableBounds;
        this.GraphemeIndex = graphemeIndex;
        this.StringIndex = stringIndex;
    }

    /// <summary>
    /// Gets the positioned logical advance rectangle for the grapheme in pixel units.
    /// </summary>
    public FontRectangle Advance { get; }

    /// <summary>
    /// Gets the rendered glyph bounds for the grapheme in pixel units.
    /// </summary>
    public FontRectangle Bounds { get; }

    /// <summary>
    /// Gets the union of the positioned logical advance bounds and rendered glyph bounds in pixel units.
    /// </summary>
    public FontRectangle RenderableBounds { get; }

    /// <summary>
    /// Gets the grapheme index in the original text.
    /// </summary>
    public int GraphemeIndex { get; }

    /// <summary>
    /// Gets the UTF-16 index in the original text where the grapheme begins.
    /// </summary>
    public int StringIndex { get; }
}
