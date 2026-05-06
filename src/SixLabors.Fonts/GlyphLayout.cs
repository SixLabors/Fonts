// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Represents the layout positions of a glyph entry emitted from a laid-out <see cref="TextBlock"/>.
/// </summary>
internal readonly struct GlyphLayout
{
    internal GlyphLayout(
        Glyph glyph,
        Vector2 advanceOrigin,
        Vector2 glyphOrigin,
        Vector2 decorationOrigin,
        float advanceWidth,
        float advanceHeight,
        GlyphLayoutMode layoutMode,
        int bidiLevel,
        bool isStartOfLine,
        int graphemeIndex,
        int stringIndex)
    {
        this.Glyph = glyph;
        this.CodePoint = glyph.GlyphMetrics.CodePoint;
        this.AdvanceOrigin = advanceOrigin;
        this.GlyphOrigin = glyphOrigin;
        this.DecorationOrigin = decorationOrigin;
        this.AdvanceX = advanceWidth;
        this.AdvanceY = advanceHeight;
        this.LayoutMode = layoutMode;
        this.BidiLevel = bidiLevel;
        this.IsStartOfLine = isStartOfLine;
        this.GraphemeIndex = graphemeIndex;
        this.StringIndex = stringIndex;
    }

    /// <summary>
    /// Gets the font-specific glyph for this laid-out glyph entry.
    /// </summary>
    public Glyph Glyph { get; }

    /// <summary>
    /// Gets the code point represented by this glyph.
    /// </summary>
    public CodePoint CodePoint { get; }

    /// <summary>
    /// Gets the origin of the logical advance box in DPI-normalized layout units.
    /// </summary>
    /// <remarks>
    /// Multiply by the target DPI to convert to device pixels.
    /// </remarks>
    public Vector2 AdvanceOrigin { get; }

    /// <summary>
    /// Gets the origin used to render the glyph outline in DPI-normalized layout units.
    /// </summary>
    /// <remarks>
    /// Multiply by the target DPI to convert to device pixels.
    /// </remarks>
    public Vector2 GlyphOrigin { get; }

    /// <summary>
    /// Gets the origin used to render text decorations in DPI-normalized layout units.
    /// </summary>
    /// <remarks>
    /// Multiply by the target DPI to convert to device pixels.
    /// </remarks>
    public Vector2 DecorationOrigin { get; }

    /// <summary>
    /// Gets the advance in the x direction in DPI-normalized layout units.
    /// </summary>
    /// <remarks>
    /// Multiply by the target DPI to convert to device pixels.
    /// </remarks>
    public float AdvanceX { get; }

    /// <summary>
    /// Gets the advance in the y direction in DPI-normalized layout units.
    /// </summary>
    /// <remarks>
    /// Multiply by the target DPI to convert to device pixels.
    /// </remarks>
    public float AdvanceY { get; }

    /// <summary>
    /// Gets the glyph layout mode.
    /// </summary>
    public GlyphLayoutMode LayoutMode { get; }

    /// <summary>
    /// Gets the resolved bidi embedding level.
    /// </summary>
    internal int BidiLevel { get; }

    /// <summary>
    /// Gets a value indicating whether this glyph is the first glyph on a new line.
    /// </summary>
    public bool IsStartOfLine { get; }

    /// <summary>
    /// Gets the grapheme index of the glyph in the original text.
    /// </summary>
    public int GraphemeIndex { get; }

    /// <summary>
    /// Gets the UTF-16 string index of the glyph in the original text.
    /// </summary>
    public int StringIndex { get; }

    /// <summary>
    /// Gets a value indicating whether the glyph represents a whitespace character.
    /// </summary>
    /// <returns>The <see cref="bool"/>.</returns>
    public bool IsWhiteSpace() => UnicodeUtility.ShouldRenderWhiteSpaceOnly(this.CodePoint);

    /// <summary>
    /// Measures the positioned logical advance rectangle in pixel units.
    /// </summary>
    /// <param name="dpi">The target DPI.</param>
    /// <returns>The measured advance rectangle.</returns>
    internal FontRectangle MeasureAdvance(float dpi)
        => new(
            this.AdvanceOrigin.X * dpi,
            this.AdvanceOrigin.Y * dpi,
            this.AdvanceX * dpi,
            this.AdvanceY * dpi);

    /// <summary>
    /// Measures the rendered glyph bounds in pixel units.
    /// </summary>
    /// <param name="dpi">The target DPI.</param>
    /// <returns>The measured rendered bounds.</returns>
    internal FontRectangle MeasureBounds(float dpi)
    {
        // Same logic as in GlyphMetrics.RenderTo.
        Vector2 glyphOrigin = this.GlyphOrigin * dpi;
        FontRectangle box = this.Glyph.BoundingBox(this.LayoutMode, glyphOrigin, dpi);

        // Whitespace uses the layout advance because it occupies measurable
        // text space even though the renderer suppresses its outline.
        if (this.IsWhiteSpace())
        {
            if (this.LayoutMode == GlyphLayoutMode.Vertical)
            {
                return new FontRectangle(
                    box.X,
                    box.Y,
                    box.Width,
                    this.AdvanceY * dpi);
            }

            if (this.LayoutMode == GlyphLayoutMode.VerticalRotated)
            {
                return new FontRectangle(
                    box.X,
                    box.Y,
                    0,
                    this.AdvanceY * dpi);
            }

            return new FontRectangle(
                box.X,
                box.Y,
                this.AdvanceX * dpi,
                box.Height);
        }

        return box;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        string s = this.IsStartOfLine ? "@ " : string.Empty;
        string ws = this.IsWhiteSpace() ? "!" : string.Empty;
        Vector2 l = this.GlyphOrigin;
        return $"{s}{ws}{this.CodePoint.ToDebuggerDisplay()} {l.X},{l.Y} {this.AdvanceX}x{this.AdvanceY}";
    }
}
