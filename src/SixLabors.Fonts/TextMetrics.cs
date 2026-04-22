// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Encapsulates the full set of measurement results for laid-out text.
/// </summary>
/// <remarks>
/// <para>
/// This type aggregates every measurement exposed by the granular <see cref="TextMeasurer"/> overloads.
/// Producing one <see cref="TextMetrics"/> instance is cheaper than calling multiple granular overloads
/// back-to-back because the text is shaped and laid out only once.
/// </para>
/// <para>
/// For callers that only require one or two values, the granular overloads on <see cref="TextMeasurer"/>
/// remain the most efficient choice because they avoid materializing the per-character and per-line arrays.
/// </para>
/// </remarks>
public readonly struct TextMetrics
{
    /// <summary>
    /// Represents an empty <see cref="TextMetrics"/> instance with zeroed rectangles and empty collections.
    /// </summary>
    public static readonly TextMetrics Empty = new(
        FontRectangle.Empty,
        FontRectangle.Empty,
        FontRectangle.Empty,
        FontRectangle.Empty,
        0,
        [],
        [],
        [],
        [],
        []);

    internal TextMetrics(
        FontRectangle advance,
        FontRectangle bounds,
        FontRectangle size,
        FontRectangle renderableBounds,
        int lineCount,
        GlyphBounds[] characterAdvances,
        GlyphBounds[] characterSizes,
        GlyphBounds[] characterBounds,
        GlyphBounds[] characterRenderableBounds,
        LineMetrics[] lines)
    {
        this.Advance = advance;
        this.Bounds = bounds;
        this.Size = size;
        this.RenderableBounds = renderableBounds;
        this.LineCount = lineCount;
        this.CharacterAdvances = characterAdvances;
        this.CharacterSizes = characterSizes;
        this.CharacterBounds = characterBounds;
        this.CharacterRenderableBounds = characterRenderableBounds;
        this.Lines = lines;
    }

    /// <summary>
    /// Gets the logical advance rectangle of the text in pixel units.
    /// </summary>
    /// <remarks>
    /// Reflects line-box height and horizontal or vertical text advance from the layout model.
    /// Does not guarantee that all rendered glyph pixels fit within the returned rectangle.
    /// </remarks>
    public FontRectangle Advance { get; }

    /// <summary>
    /// Gets the rendered glyph bounds of the text in pixel units.
    /// </summary>
    /// <remarks>
    /// This is the tight ink bounds enclosing all rendered glyphs and may be smaller or larger
    /// than the logical advance. May have a non-zero origin.
    /// </remarks>
    public FontRectangle Bounds { get; }

    /// <summary>
    /// Gets the normalized rendered size of the text in pixel units with the origin at <c>(0, 0)</c>.
    /// </summary>
    /// <remarks>
    /// Equivalent to <see cref="Bounds"/> with a zeroed origin.
    /// </remarks>
    public FontRectangle Size { get; }

    /// <summary>
    /// Gets the union of the logical advance rectangle (positioned at the text options origin)
    /// and the rendered glyph bounds.
    /// </summary>
    /// <remarks>
    /// Use this rectangle when both typographic advance and rendered glyph overshoot
    /// must fit within the same bounding box.
    /// </remarks>
    public FontRectangle RenderableBounds { get; }

    /// <summary>
    /// Gets the number of laid-out lines in the text.
    /// </summary>
    public int LineCount { get; }

    /// <summary>
    /// Gets the logical advance of each laid-out character in pixel units.
    /// </summary>
    /// <remarks>
    /// Each entry reflects the typographic advance width and height for one character,
    /// with an origin of <c>(0, 0)</c>.
    /// </remarks>
    public IReadOnlyList<GlyphBounds> CharacterAdvances { get; }

    /// <summary>
    /// Gets the normalized rendered size of each laid-out character in pixel units.
    /// </summary>
    /// <remarks>
    /// Each entry is the tight ink bounds of one glyph with the origin normalized to <c>(0, 0)</c>.
    /// </remarks>
    public IReadOnlyList<GlyphBounds> CharacterSizes { get; }

    /// <summary>
    /// Gets the rendered glyph bounds of each laid-out character in pixel units.
    /// </summary>
    /// <remarks>
    /// Each entry reflects the tight ink bounds of one rendered glyph.
    /// </remarks>
    public IReadOnlyList<GlyphBounds> CharacterBounds { get; }

    /// <summary>
    /// Gets the full renderable bounds of each laid-out character in pixel units.
    /// </summary>
    /// <remarks>
    /// Each entry is the union of the logical advance rectangle and the rendered glyph bounds
    /// for the corresponding laid-out character.
    /// </remarks>
    public IReadOnlyList<GlyphBounds> CharacterRenderableBounds { get; }

    /// <summary>
    /// Gets the per-line layout metrics for the text.
    /// </summary>
    public IReadOnlyList<LineMetrics> Lines { get; }
}
