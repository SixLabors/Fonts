// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

[DebuggerDisplay("StartGlyphId: {StartGlyphId}, EndGlyphId: {EndGlyphId}, Class: {Class}")]
internal readonly struct ClassRangeRecord
{
    public ClassRangeRecord(ushort startGlyphId, ushort endGlyphId, ushort glyphClass)
    {
        this.StartGlyphId = startGlyphId;
        this.EndGlyphId = endGlyphId;
        this.Class = glyphClass;
    }

    public ushort StartGlyphId { get; }

    public ushort EndGlyphId { get; }

    public ushort Class { get; }
}
