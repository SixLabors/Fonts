// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts;

/// <summary>
/// Encapsulates measured metrics for a single laid-out text line.
/// </summary>
public readonly struct LineMetrics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LineMetrics"/> struct.
    /// </summary>
    /// <param name="ascender">The ascender line position within the line box.</param>
    /// <param name="baseline">The baseline position within the line box.</param>
    /// <param name="descender">The descender line position within the line box.</param>
    /// <param name="lineHeight">The total line-box size for this line.</param>
    /// <param name="start">The logical line box start position in pixel units.</param>
    /// <param name="extent">The logical line box extent in pixel units.</param>
    /// <param name="stringIndex">The UTF-16 index in the original text where this line begins.</param>
    /// <param name="graphemeIndex">The grapheme index in the original text where this line begins.</param>
    /// <param name="graphemeCount">The number of graphemes in the line.</param>
    /// <param name="graphemeOffset">The offset of this line's first grapheme metrics entry.</param>
    internal LineMetrics(
        float ascender,
        float baseline,
        float descender,
        float lineHeight,
        Vector2 start,
        Vector2 extent,
        int stringIndex,
        int graphemeIndex,
        int graphemeCount,
        int graphemeOffset)
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
        this.GraphemeOffset = graphemeOffset;
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
    /// Gets the logical line box start position in pixel units.
    /// </summary>
    public Vector2 Start { get; }

    /// <summary>
    /// Gets the logical line box extent in pixel units.
    /// </summary>
    public Vector2 Extent { get; }

    /// <summary>
    /// Gets the zero-based UTF-16 code unit index in the original text.
    /// </summary>
    public int StringIndex { get; }

    /// <summary>
    /// Gets the zero-based grapheme index in the original text.
    /// </summary>
    public int GraphemeIndex { get; }

    /// <summary>
    /// Gets the number of graphemes in the line.
    /// </summary>
    public int GraphemeCount { get; }

    /// <summary>
    /// Gets the offset of this line's first entry in the flattened grapheme metrics buffer.
    /// </summary>
    internal int GraphemeOffset { get; }
}
