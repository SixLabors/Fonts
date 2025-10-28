// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.CMap;

internal sealed class Format4SubTable : CMapSubTable
{
    public Format4SubTable(ushort language, PlatformIDs platform, ushort encoding, Segment[] segments, ushort[] glyphIds)
        : base(platform, encoding, 4)
    {
        this.Language = language;
        this.Segments = segments;
        this.GlyphIds = glyphIds;
    }

    public Segment[] Segments { get; }

    public ushort[] GlyphIds { get; }

    public ushort Language { get; }

    public override bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId)
    {
        int charAsInt = codePoint.Value;

        for (int i = 0; i < this.Segments.Length; i++)
        {
            ref Segment seg = ref this.Segments[i];

            if (seg.End >= charAsInt && seg.Start <= charAsInt)
            {
                if (seg.Offset == 0)
                {
                    glyphId = (ushort)((charAsInt + seg.Delta) & ushort.MaxValue);
                    return true;
                }

                long offset = (seg.Offset / 2) + (charAsInt - seg.Start);
                glyphId = this.GlyphIds[offset - this.Segments.Length + seg.Index];

                return true;
            }
        }

        glyphId = 0;
        return false;
    }

    public override bool TryGetCodePoint(ushort glyphId, out CodePoint codePoint)
    {
        for (int i = 0; i < this.Segments.Length; i++)
        {
            ref Segment seg = ref this.Segments[i];

            if (seg.Offset == 0)
            {
                // Reverse the delta-based calculation
                // Forward was: glyphId = (charAsInt + seg.Delta) & 0xFFFF
                // Reverse should apply the inverse logic with the same wrap:
                int candidate = (glyphId - seg.Delta) & ushort.MaxValue;

                if (candidate >= seg.Start && candidate <= seg.End)
                {
                    codePoint = new CodePoint(candidate);
                    return true;
                }
            }
            else
            {
                // Reverse the offset-based calculation:
                // Forward logic:
                //   offset = (seg.Offset / 2) + (charAsInt - seg.Start)
                //   glyphId = GlyphIds[offset - Segments.Length + seg.Index]

                // To reverse, iterate over possible codepoints in the segment and find the matching glyphId.
                for (long j = 0; j <= (seg.End - seg.Start); j++)
                {
                    long offset = (seg.Offset / 2) + j;
                    if (this.GlyphIds[offset - this.Segments.Length + seg.Index] == glyphId)
                    {
                        codePoint = new CodePoint((int)(seg.Start + j));
                        return true;
                    }
                }
            }
        }

        codePoint = default;
        return false;
    }

    public override IEnumerable<int> GetAvailableCodePoints()
        => this.Segments.SelectMany(segment => Enumerable.Range(segment.Start, segment.End - segment.Start + 1));

    public static IEnumerable<Format4SubTable> Load(IEnumerable<EncodingRecord> encodings, BigEndianBinaryReader reader)
    {
        // 'cmap' Subtable Format 4:
        // Type   | Name                       | Description
        // -------|----------------------------|------------------------------------------------------------------------
        // uint16 | format                     | Format number is set to 4.
        // uint16 | length                     | This is the length in bytes of the subtable.
        // uint16 | language                   | Please see "Note on the language field in 'cmap' subtables" in this document.
        // uint16 | segCountX2                 | 2 x segCount.
        // uint16 | searchRange                | 2 x (2**floor(log2(segCount)))
        // uint16 | entrySelector              | log2(searchRange/2)
        // uint16 | rangeShift                 | 2 x segCount - searchRange
        // uint16 | endCount[segCount]         | End characterCode for each segment, last=0xFFFF.
        // uint16 | reservedPad                | Set to 0.
        // uint16 | startCount[segCount]       | Start character code for each segment.
        // int16  | idDelta[segCount]           | Delta for all character codes in segment.
        // uint16 | idRangeOffset[segCount]    | Offsets into glyphIdArray or 0
        // uint16 | glyphIdArray[ ]            | Glyph index array (arbitrary length)
        // format has already been read by this point skip it
        ushort length = reader.ReadUInt16();
        ushort language = reader.ReadUInt16();
        ushort segCountX2 = reader.ReadUInt16();
        ushort searchRange = reader.ReadUInt16();
        ushort entrySelector = reader.ReadUInt16();
        ushort rangeShift = reader.ReadUInt16();
        int segCount = segCountX2 / 2;

        using Buffer<ushort> endCountBuffer = new(segCount);
        Span<ushort> endCounts = endCountBuffer.GetSpan();
        reader.ReadUInt16Array(endCounts);

        ushort reserved = reader.ReadUInt16();

        using Buffer<ushort> startCountsBuffer = new(segCount);
        Span<ushort> startCounts = startCountsBuffer.GetSpan();
        reader.ReadUInt16Array(startCounts);

        using Buffer<short> idDeltaBuffer = new(segCount);
        Span<short> idDelta = idDeltaBuffer.GetSpan();
        reader.ReadInt16Array(idDelta);

        using Buffer<ushort> idRangeOffsetBuffer = new(segCount);
        Span<ushort> idRangeOffset = idRangeOffsetBuffer.GetSpan();
        reader.ReadUInt16Array(idRangeOffset);

        // table length thus far
        int headerLength = 16 + (segCount * 8);
        int glyphIdCount = (length - headerLength) / 2;

        ushort[] glyphIds = reader.ReadUInt16Array(glyphIdCount);

        Segment[] segments = Segment.Create(endCounts, startCounts, idDelta, idRangeOffset);

        List<Format4SubTable> table = [];
        foreach (EncodingRecord encoding in encodings)
        {
            table.Add(new Format4SubTable(language, encoding.PlatformID, encoding.EncodingID, segments, glyphIds));
        }

        return table;
    }

    internal readonly struct Segment
    {
        public Segment(ushort index, ushort end, ushort start, short delta, ushort offset)
        {
            this.Index = index;
            this.End = end;
            this.Start = start;
            this.Delta = delta;
            this.Offset = offset;
        }

        public ushort Index { get; }

        public short Delta { get; }

        public ushort End { get; }

        public ushort Offset { get; }

        public ushort Start { get; }

        public static Segment[] Create(ReadOnlySpan<ushort> endCounts, ReadOnlySpan<ushort> startCode, ReadOnlySpan<short> idDelta, ReadOnlySpan<ushort> idRangeOffset)
        {
            int count = endCounts.Length;
            Segment[] segments = new Segment[count];
            for (ushort i = 0; i < count; i++)
            {
                ushort start = startCode[i];
                ushort end = endCounts[i];
                short delta = idDelta[i];
                ushort offset = idRangeOffset[i];
                segments[i] = new Segment(i, end, start, delta, offset);
            }

            return segments;
        }
    }
}
