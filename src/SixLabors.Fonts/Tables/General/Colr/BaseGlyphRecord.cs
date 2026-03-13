// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Represents a COLR v0 BaseGlyph record that maps a glyph ID to a range of layer records.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#baseglyph-and-layer-records"/>
/// </summary>
internal readonly struct BaseGlyphRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseGlyphRecord"/> struct.
    /// </summary>
    /// <param name="glyphId">The glyph ID of the base glyph.</param>
    /// <param name="firstLayerIndex">The index of the first layer record for this glyph.</param>
    /// <param name="layerCount">The number of layer records for this glyph.</param>
    public BaseGlyphRecord(ushort glyphId, ushort firstLayerIndex, ushort layerCount)
    {
        this.GlyphId = glyphId;
        this.FirstLayerIndex = firstLayerIndex;
        this.LayerCount = layerCount;
    }

    /// <summary>
    /// Gets the glyph ID of the base glyph.
    /// </summary>
    public ushort GlyphId { get; }

    /// <summary>
    /// Gets the index of the first layer record for this glyph in the layer records array.
    /// </summary>
    public ushort FirstLayerIndex { get; }

    /// <summary>
    /// Gets the number of contiguous layer records for this glyph.
    /// </summary>
    public ushort LayerCount { get; }
}
