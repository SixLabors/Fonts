// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Svg;

internal readonly struct SvgDocumentIndexEntry
{
    public SvgDocumentIndexEntry(ushort startGlyphId, ushort endGlyphId, uint svgDocOffset, uint svgDocLength)
    {
        this.StartGlyphId = startGlyphId;
        this.EndGlyphId = endGlyphId;
        this.SvgDocOffset = svgDocOffset;
        this.SvgDocLength = svgDocLength;
    }

    public ushort StartGlyphId { get; }

    public ushort EndGlyphId { get; }

    public uint SvgDocOffset { get; }

    public uint SvgDocLength { get; }
}
