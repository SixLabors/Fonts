// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Represents the positioned metrics for one Unicode word-boundary segment.
/// </summary>
public readonly struct WordMetrics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WordMetrics"/> struct.
    /// </summary>
    /// <param name="advance">The positioned logical advance rectangle for the word-boundary segment in pixel units.</param>
    /// <param name="bounds">The rendered glyph bounds for the word-boundary segment in pixel units.</param>
    /// <param name="renderableBounds">The union of the positioned logical advance bounds and rendered glyph bounds in pixel units.</param>
    /// <param name="graphemeStart">The inclusive grapheme insertion index where the word-boundary segment starts.</param>
    /// <param name="graphemeEnd">The exclusive grapheme insertion index where the word-boundary segment ends.</param>
    /// <param name="stringStart">The inclusive UTF-16 index where the word-boundary segment starts.</param>
    /// <param name="stringEnd">The exclusive UTF-16 index where the word-boundary segment ends.</param>
    internal WordMetrics(
        FontRectangle advance,
        FontRectangle bounds,
        FontRectangle renderableBounds,
        int graphemeStart,
        int graphemeEnd,
        int stringStart,
        int stringEnd)
    {
        this.Advance = advance;
        this.Bounds = bounds;
        this.RenderableBounds = renderableBounds;
        this.GraphemeStart = graphemeStart;
        this.GraphemeEnd = graphemeEnd;
        this.StringStart = stringStart;
        this.StringEnd = stringEnd;
    }

    /// <summary>
    /// Gets the positioned logical advance rectangle for the word-boundary segment in pixel units.
    /// </summary>
    public FontRectangle Advance { get; }

    /// <summary>
    /// Gets the rendered glyph bounds for the word-boundary segment in pixel units.
    /// </summary>
    public FontRectangle Bounds { get; }

    /// <summary>
    /// Gets the union of the positioned logical advance bounds and rendered glyph bounds in pixel units.
    /// </summary>
    public FontRectangle RenderableBounds { get; }

    /// <summary>
    /// Gets the inclusive grapheme insertion index where the word-boundary segment starts.
    /// </summary>
    public int GraphemeStart { get; }

    /// <summary>
    /// Gets the exclusive grapheme insertion index where the word-boundary segment ends.
    /// </summary>
    public int GraphemeEnd { get; }

    /// <summary>
    /// Gets the inclusive UTF-16 index where the word-boundary segment starts.
    /// </summary>
    public int StringStart { get; }

    /// <summary>
    /// Gets the exclusive UTF-16 index where the word-boundary segment ends.
    /// </summary>
    public int StringEnd { get; }
}
