// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts;

/// <summary>
/// Represents a font-specific glyph at the point size used for layout and rendering.
/// </summary>
public readonly struct Glyph
{
    private readonly float pointSize;

    internal Glyph(FontGlyphMetrics glyphMetrics, float pointSize)
    {
        this.GlyphMetrics = glyphMetrics;
        this.pointSize = pointSize;
    }

    /// <summary>
    /// Gets the font metrics for this glyph.
    /// </summary>
    public FontGlyphMetrics GlyphMetrics { get; }

    /// <summary>
    /// Calculates the rendered glyph bounds for the specified layout mode and origin.
    /// </summary>
    /// <param name="mode">The glyph layout mode to measure with.</param>
    /// <param name="glyphOrigin">The glyph origin to calculate the bounds from.</param>
    /// <param name="dpi">The DPI to measure the glyph at.</param>
    /// <returns>The rendered glyph bounds.</returns>
    public FontRectangle BoundingBox(GlyphLayoutMode mode, Vector2 glyphOrigin, float dpi)
        => this.GlyphMetrics.GetBoundingBox(mode, glyphOrigin, this.pointSize * dpi);

    /// <summary>
    /// Renders the glyph to the render surface.
    /// </summary>
    /// <param name="surface">The target render surface.</param>
    /// <param name="graphemeIndex">The index of the grapheme this glyph is part of.</param>
    /// <param name="glyphOrigin">The origin used to render the glyph outline.</param>
    /// <param name="decorationOrigin">The origin used to render text decorations.</param>
    /// <param name="mode">The glyph layout mode to render using.</param>
    /// <param name="options">The options to render using.</param>
    internal void RenderTo(
        IGlyphRenderer surface,
        int graphemeIndex,
        Vector2 glyphOrigin,
        Vector2 decorationOrigin,
        GlyphLayoutMode mode,
        TextOptions options)
        => this.GlyphMetrics.RenderTo(surface, graphemeIndex, glyphOrigin, decorationOrigin, mode, options);
}
