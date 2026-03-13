// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Represents a COLR v1 ClipRecord that defines a clip region for a range of glyph IDs.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#cliplist-table"/>
/// </summary>
internal readonly struct ClipRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClipRecord"/> struct.
    /// </summary>
    /// <param name="startGlyphId">The first glyph ID in the range covered by this clip record.</param>
    /// <param name="endGlyphId">The last glyph ID in the range covered by this clip record.</param>
    /// <param name="clipBoxOffset">The offset from the start of the COLR table to the ClipBox subtable.</param>
    public ClipRecord(ushort startGlyphId, ushort endGlyphId, uint clipBoxOffset)
    {
        this.StartGlyphId = startGlyphId;
        this.EndGlyphId = endGlyphId;
        this.ClipBoxOffset = clipBoxOffset;
    }

    /// <summary>
    /// Gets the first glyph ID in the range covered by this clip record.
    /// </summary>
    public ushort StartGlyphId { get; }

    /// <summary>
    /// Gets the last glyph ID in the range covered by this clip record.
    /// </summary>
    public ushort EndGlyphId { get; }

    /// <summary>
    /// Gets the offset from the start of the COLR table to a ClipBox subtable (Format 1 or 2) defining the clip region.
    /// </summary>
    public uint ClipBoxOffset { get; }
}
