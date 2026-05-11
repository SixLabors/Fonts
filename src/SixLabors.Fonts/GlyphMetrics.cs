// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Represents one laid-out glyph entry in final layout order.
/// </summary>
public readonly struct GlyphMetrics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphMetrics"/> struct.
    /// </summary>
    /// <param name="codePoint">The Unicode code point represented by the glyph entry.</param>
    /// <param name="advance">The positioned logical advance rectangle for the glyph entry in pixel units.</param>
    /// <param name="bounds">The rendered rectangle for the glyph entry in pixel units.</param>
    /// <param name="renderableBounds">The union of the positioned logical advance rectangle and rendered rectangle in pixel units.</param>
    /// <param name="font">The font used to render the glyph entry.</param>
    /// <param name="graphemeIndex">The grapheme index in the original text.</param>
    /// <param name="stringIndex">The UTF-16 index in the original text where the glyph entry begins.</param>
    internal GlyphMetrics(
        CodePoint codePoint,
        in FontRectangle advance,
        in FontRectangle bounds,
        in FontRectangle renderableBounds,
        Font font,
        int graphemeIndex,
        int stringIndex)
    {
        this.CodePoint = codePoint;
        this.Advance = advance;
        this.Bounds = bounds;
        this.RenderableBounds = renderableBounds;
        this.Font = font;
        this.GraphemeIndex = graphemeIndex;
        this.StringIndex = stringIndex;
    }

    /// <summary>
    /// Gets the Unicode code point represented by the glyph entry.
    /// </summary>
    public CodePoint CodePoint { get; }

    /// <summary>
    /// Gets the positioned logical advance rectangle for the glyph entry in pixel units.
    /// </summary>
    public FontRectangle Advance { get; }

    /// <summary>
    /// Gets the rendered rectangle for the glyph entry in pixel units.
    /// </summary>
    public FontRectangle Bounds { get; }

    /// <summary>
    /// Gets the union of the positioned logical advance rectangle and rendered rectangle in pixel units.
    /// </summary>
    public FontRectangle RenderableBounds { get; }

    /// <summary>
    /// Gets the font used to render the glyph entry.
    /// </summary>
    public Font Font { get; }

    /// <summary>
    /// Gets the zero-based grapheme index in the original text.
    /// </summary>
    public int GraphemeIndex { get; }

    /// <summary>
    /// Gets the zero-based UTF-16 code unit index in the original text.
    /// </summary>
    public int StringIndex { get; }

    /// <inheritdoc/>
    public override string ToString()
        => $"CodePoint: {this.CodePoint}, Advance: {this.Advance}, Bounds: {this.Bounds}, RenderableBounds: {this.RenderableBounds}.";
}
