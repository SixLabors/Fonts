// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// Supplies painted glyphs (layers + commands + paints) and canvas metadata for a glyph id.
/// Interpreters (e.g., COLR v1, OT-SVG) implement this interface.
/// </summary>
internal interface IPaintedGlyphSource
{
    /// <summary>
    /// Attempts to get a painted glyph and its canvas metadata.
    /// </summary>
    /// <param name="glyphId">The glyph id.</param>
    /// <param name="glyph">The painted glyph.</param>
    /// <param name="canvas">The canvas metadata.</param>
    /// <returns><see langword="true"/> if the glyph is available; otherwise <see langword="false"/>.</returns>
    bool TryGetPaintedGlyph(ushort glyphId, out PaintedGlyph glyph, out PaintedCanvas canvas);
}
