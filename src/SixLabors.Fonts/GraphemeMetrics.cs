// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Represents one coalesced grapheme in final layout order.
/// </summary>
public readonly struct GraphemeMetrics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphemeMetrics"/> struct.
    /// </summary>
    /// <param name="advance">The positioned logical advance rectangle for the grapheme in pixel units.</param>
    /// <param name="bounds">The rendered glyph bounds for the grapheme in pixel units.</param>
    /// <param name="renderableBounds">The union of the positioned logical advance bounds and rendered glyph bounds in pixel units.</param>
    /// <param name="graphemeIndex">The grapheme index in the original text.</param>
    /// <param name="stringIndex">The UTF-16 index in the original text where the grapheme begins.</param>
    /// <param name="bidiLevel">The resolved bidi embedding level.</param>
    /// <param name="isLineBreak">Whether the grapheme represents a line break.</param>
    internal GraphemeMetrics(
        FontRectangle advance,
        FontRectangle bounds,
        FontRectangle renderableBounds,
        int graphemeIndex,
        int stringIndex,
        int bidiLevel,
        bool isLineBreak)
    {
        this.Advance = advance;
        this.Bounds = bounds;
        this.RenderableBounds = renderableBounds;
        this.GraphemeIndex = graphemeIndex;
        this.StringIndex = stringIndex;
        this.BidiLevel = bidiLevel;
        this.IsLineBreak = isLineBreak;
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
    /// Gets the zero-based grapheme index in the original text.
    /// </summary>
    public int GraphemeIndex { get; }

    /// <summary>
    /// Gets the zero-based UTF-16 code unit index in the original text.
    /// </summary>
    public int StringIndex { get; }

    /// <summary>
    /// Gets the resolved bidi embedding level.
    /// </summary>
    internal int BidiLevel { get; }

    /// <summary>
    /// Gets a value indicating whether this grapheme represents a line break.
    /// </summary>
    public bool IsLineBreak { get; }
}
