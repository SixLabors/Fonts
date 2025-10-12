// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

#pragma warning disable SA1201 // Elements should appear in the correct order
namespace SixLabors.Fonts.Tables.General.Colr;

internal readonly struct ClipRecord
{
    public ClipRecord(ushort startGlyphId, ushort endGlyphId, uint clipBoxOffset)
    {
        this.StartGlyphId = startGlyphId;
        this.EndGlyphId = endGlyphId;
        this.ClipBoxOffset = clipBoxOffset;
    }

    public ushort StartGlyphId { get; }

    public ushort EndGlyphId { get; }

    // Offset (from start of COLR table) to a ClipBox (Format1/2) defining the clip region.
    public uint ClipBoxOffset { get; }
}
