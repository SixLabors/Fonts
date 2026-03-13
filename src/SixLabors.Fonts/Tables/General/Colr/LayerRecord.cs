// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Represents a COLR v0 layer record that pairs a glyph ID with a CPAL palette entry index.
/// Each layer renders the glyph outline in the specified color.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#baseglyph-and-layer-records"/>
/// </summary>
internal readonly struct LayerRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LayerRecord"/> struct.
    /// </summary>
    /// <param name="glyphId">The glyph ID for this layer.</param>
    /// <param name="paletteIndex">The index into the CPAL palette for this layer's color.</param>
    public LayerRecord(ushort glyphId, ushort paletteIndex)
    {
        this.GlyphId = glyphId;
        this.PaletteIndex = paletteIndex;
    }

    /// <summary>
    /// Gets the glyph ID for this layer.
    /// </summary>
    public ushort GlyphId { get; }

    /// <summary>
    /// Gets the index into the CPAL palette for this layer's color.
    /// A value of 0xFFFF indicates the foreground color.
    /// </summary>
    public ushort PaletteIndex { get; }
}
