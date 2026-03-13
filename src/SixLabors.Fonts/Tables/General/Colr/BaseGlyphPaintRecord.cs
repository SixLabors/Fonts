// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Represents a record in the COLR v1 BaseGlyphList that associates a glyph ID with its root paint table offset.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#baseglyphlist-layerlist-and-colrglyphs"/>
/// </summary>
internal readonly struct BaseGlyphPaintRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseGlyphPaintRecord"/> struct.
    /// </summary>
    /// <param name="glyphId">The glyph ID.</param>
    /// <param name="paintOffset">The offset to the root paint table for this glyph, relative to the beginning of the COLR table.</param>
    public BaseGlyphPaintRecord(ushort glyphId, uint paintOffset)
    {
        this.GlyphId = glyphId;
        this.PaintOffset = paintOffset;
    }

    /// <summary>
    /// Gets the glyph ID.
    /// </summary>
    public ushort GlyphId { get; }

    /// <summary>
    /// Gets the offset to the root paint table for this glyph, relative to the beginning of the COLR table.
    /// </summary>
    public uint PaintOffset { get; }
}
