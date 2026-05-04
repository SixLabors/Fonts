// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Encapsulates measured metrics for a single laid-out text line.
/// </summary>
/// <remarks>
/// <para>This type is layout-mode agnostic:</para>
/// <list type="bullet">
/// <item><description>Horizontal layouts: <see cref="Start"/> is the X start position and <see cref="Extent"/> is the width.</description></item>
/// <item><description>Vertical layouts: <see cref="Start"/> is the Y start position and <see cref="Extent"/> is the height.</description></item>
/// </list>
/// </remarks>
public readonly struct LineMetrics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LineMetrics"/> struct.
    /// </summary>
    /// <param name="ascender">Ascender line position within the line box.</param>
    /// <param name="baseline">Baseline position within the line box.</param>
    /// <param name="descender">Descender line position within the line box.</param>
    /// <param name="lineHeight">Total line-box size (includes effective line spacing).</param>
    /// <param name="start">Line start position in the primary layout flow direction after alignment.</param>
    /// <param name="extent">Line extent in the primary layout flow direction.</param>
    /// <param name="stringIndex">The UTF-16 index in the original text where this line begins.</param>
    /// <param name="graphemeIndex">The grapheme index in the original text where this line begins.</param>
    /// <param name="graphemeCount">The number of graphemes in the line.</param>
    /// <param name="glyphIndex">The index of the first glyph entry in the measured character collections.</param>
    /// <param name="glyphCount">The number of glyph entries in the measured character collections for this line.</param>
    public LineMetrics(
        float ascender,
        float baseline,
        float descender,
        float lineHeight,
        float start,
        float extent,
        int stringIndex,
        int graphemeIndex,
        int graphemeCount,
        int glyphIndex,
        int glyphCount)
    {
        this.Ascender = ascender;
        this.Baseline = baseline;
        this.Descender = descender;
        this.LineHeight = lineHeight;
        this.Start = start;
        this.Extent = extent;
        this.StringIndex = stringIndex;
        this.GraphemeIndex = graphemeIndex;
        this.GraphemeCount = graphemeCount;
        this.GlyphIndex = glyphIndex;
        this.GlyphCount = glyphCount;
    }

    /// <summary>
    /// Gets the ascender line position within the line box.
    /// </summary>
    /// <remarks>
    /// This is a position value (not a baseline-relative distance).
    /// Use this value to draw the ascender guide line relative to the current line origin.
    /// </remarks>
    public float Ascender { get; }

    /// <summary>
    /// Gets the baseline position within the line box.
    /// </summary>
    /// <remarks>
    /// Use this value as the guide-line position for drawing a baseline relative to the current line origin.
    /// </remarks>
    public float Baseline { get; }

    /// <summary>
    /// Gets the descender line position within the line box.
    /// </summary>
    /// <remarks>
    /// This is a position value (not a baseline-relative distance).
    /// Use this value to draw the descender guide line relative to the current line origin.
    /// </remarks>
    public float Descender { get; }

    /// <summary>
    /// Gets the total line-box size for this line.
    /// </summary>
    public float LineHeight { get; }

    /// <summary>
    /// Gets the line start position in the primary layout flow direction.
    /// </summary>
    public float Start { get; }

    /// <summary>
    /// Gets the line extent in the primary layout flow direction.
    /// </summary>
    public float Extent { get; }

    /// <summary>
    /// Gets the UTF-16 index in the original text where this line begins.
    /// </summary>
    public int StringIndex { get; }

    /// <summary>
    /// Gets the grapheme index in the original text where this line begins.
    /// </summary>
    public int GraphemeIndex { get; }

    /// <summary>
    /// Gets the number of graphemes in the line.
    /// </summary>
    public int GraphemeCount { get; }

    /// <summary>
    /// Gets the index of the first glyph entry in the measured character collections.
    /// </summary>
    public int GlyphIndex { get; }

    /// <summary>
    /// Gets the number of glyph entries in the measured character collections for this line.
    /// </summary>
    public int GlyphCount { get; }
}
