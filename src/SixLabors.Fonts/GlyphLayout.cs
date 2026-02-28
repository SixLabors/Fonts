// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// A glyphs layout and location
/// </summary>
internal readonly struct GlyphLayout
{
    internal GlyphLayout(
        Glyph glyph,
        Vector2 boxLocation,
        Vector2 penLocation,
        Vector2 offset,
        float advanceWidth,
        float advanceHeight,
        GlyphLayoutMode layoutMode,
        bool isStartOfLine,
        int graphemeIndex,
        int stringIndex)
    {
        this.Glyph = glyph;
        this.CodePoint = glyph.GlyphMetrics.CodePoint;
        this.BoxLocation = boxLocation;
        this.PenLocation = penLocation;
        this.Offset = offset;
        this.AdvanceX = advanceWidth;
        this.AdvanceY = advanceHeight;
        this.LayoutMode = layoutMode;
        this.IsStartOfLine = isStartOfLine;
        this.GraphemeIndex = graphemeIndex;
        this.StringIndex = stringIndex;
    }

    /// <summary>
    /// Gets the glyph.
    /// </summary>
    public Glyph Glyph { get; }

    /// <summary>
    /// Gets the codepoint represented by this glyph.
    /// </summary>
    public CodePoint CodePoint { get; }

    /// <summary>
    /// Gets the location of the glyph box.
    /// </summary>
    public Vector2 BoxLocation { get; }

    /// <summary>
    /// Gets the location to render the glyph at.
    /// </summary>
    public Vector2 PenLocation { get; }

    /// <summary>
    /// Gets the offset of the glyph vector relative to the top-left position of the glyph advance.
    /// For horizontal layout this will always be <see cref="Vector2.Zero"/>.
    /// </summary>
    public Vector2 Offset { get; }

    /// <summary>
    /// Gets the width.
    /// </summary>
    public float AdvanceX { get; }

    /// <summary>
    /// Gets the height.
    /// </summary>
    public float AdvanceY { get; }

    /// <summary>
    /// Gets the glyph layout mode.
    /// </summary>
    public GlyphLayoutMode LayoutMode { get; }

    /// <summary>
    /// Gets a value indicating whether this glyph is the first glyph on a new line.
    /// </summary>
    public bool IsStartOfLine { get; }

    /// <summary>
    /// Gets grapheme index of glyph in original text.
    /// </summary>
    public int GraphemeIndex { get; }

    /// <summary>
    /// Gets string index of glyph in original text.
    /// </summary>
    public int StringIndex { get; }

    /// <summary>
    /// Gets a value indicating whether the glyph represents a whitespace character.
    /// </summary>
    /// <returns>The <see cref="bool"/>.</returns>
    public bool IsWhiteSpace() => UnicodeUtility.ShouldRenderWhiteSpaceOnly(this.CodePoint);

    internal FontRectangle BoundingBox(float dpi)
    {
        // Same logic as in GlyphMetrics.RenderTo
        Vector2 location = this.PenLocation;
        Vector2 offset = this.Offset;

        location *= dpi;
        offset *= dpi;
        Vector2 renderLocation = location + offset;

        FontRectangle box = this.Glyph.BoundingBox(this.LayoutMode, renderLocation, dpi);

        if (this.IsWhiteSpace())
        {
            // Take the layout advance width/height to account for advance multipliers that can cause
            // the glyph to extend beyond the box. For example '\t'.
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
        Vector2 l = this.PenLocation;
        return $"{s}{ws}{this.CodePoint.ToDebuggerDisplay()} {l.X},{l.Y} {this.AdvanceX}x{this.AdvanceY}";
    }
}
