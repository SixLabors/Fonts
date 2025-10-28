// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.CMap;

internal sealed class Format12SubTable : CMapSubTable
{
    public Format12SubTable(uint language, PlatformIDs platform, ushort encoding, SequentialMapGroup[] groups)
        : base(platform, encoding, 4)
    {
        this.Language = language;
        this.SequentialMapGroups = groups;
    }

    public SequentialMapGroup[] SequentialMapGroups { get; }

    public uint Language { get; }

    public override bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId)
    {
        int charAsInt = codePoint.Value;

        for (int i = 0; i < this.SequentialMapGroups.Length; i++)
        {
            ref SequentialMapGroup seg = ref this.SequentialMapGroups[i];

            if (charAsInt >= seg.StartCodePoint && charAsInt <= seg.EndCodePoint)
            {
                glyphId = (ushort)(charAsInt - seg.StartCodePoint + seg.StartGlyphId);
                return true;
            }
        }

        glyphId = 0;
        return false;
    }

    public override bool TryGetCodePoint(ushort glyphId, out CodePoint codePoint)
    {
        for (int i = 0; i < this.SequentialMapGroups.Length; i++)
        {
            ref SequentialMapGroup seg = ref this.SequentialMapGroups[i];
            if (glyphId >= seg.StartGlyphId && glyphId <= seg.StartGlyphId + seg.EndCodePoint - seg.StartCodePoint)
            {
                // Reverse the calculation:
                // Forward: glyphId = (codePoint - StartCodePoint) + StartGlyphId
                // Reverse: codePoint = (glyphId - StartGlyphId) + StartCodePoint
                codePoint = new CodePoint(glyphId - seg.StartGlyphId + seg.StartCodePoint);
                return true;
            }
        }

        codePoint = default;
        return false;
    }

    public override IEnumerable<int> GetAvailableCodePoints()
        => this.SequentialMapGroups.SelectMany(segment =>
        {
            int start = (int)segment.StartCodePoint;
            int end = (int)segment.EndCodePoint;
            return Enumerable.Range(start, end - start + 1);
        });

    public static IEnumerable<Format12SubTable> Load(IEnumerable<EncodingRecord> encodings, BigEndianBinaryReader reader)
    {
        // 'cmap' Subtable Format 4:
        // Type               | Name              | Description
        // -------------------|-------------------|------------------------------------------------------------------------
        // uint16             | format            | Subtable format; set to 12.
        // uint16             | reserved          | Reserved; set to 0
        // uint32             | length            | Byte length of this subtable(including the header)
        // uint32             | language          | For requirements on use of the language field, see "Use of the language field in 'cmap' subtables" in this document.
        // uint32             | numGroups         | Number of groupings which follow
        // SequentialMapGroup | groups[numGroups] | Array of SequentialMapGroup records.

        // format has already been read by this point skip it
        ushort reserved = reader.ReadUInt16();
        uint length = reader.ReadUInt32();
        uint language = reader.ReadUInt32();
        uint numGroups = reader.ReadUInt32();

        SequentialMapGroup[] groups = new SequentialMapGroup[numGroups];
        for (int i = 0; i < numGroups; i++)
        {
            groups[i] = SequentialMapGroup.Load(reader);
        }

        foreach (EncodingRecord encoding in encodings)
        {
            yield return new Format12SubTable(language, encoding.PlatformID, encoding.EncodingID, groups);
        }
    }

    internal readonly struct SequentialMapGroup
    {
        public readonly uint StartCodePoint;
        public readonly uint EndCodePoint;
        public readonly uint StartGlyphId;

        public SequentialMapGroup(uint startCodePoint, uint endCodePoint, uint startGlyph)
        {
            this.StartCodePoint = startCodePoint;
            this.EndCodePoint = endCodePoint;
            this.StartGlyphId = startGlyph;
        }

        public static SequentialMapGroup Load(BigEndianBinaryReader reader)
        {
            uint startCodePoint = reader.ReadUInt32();
            uint endCodePoint = reader.ReadUInt32();
            uint startGlyph = reader.ReadUInt32();
            return new SequentialMapGroup(startCodePoint, endCodePoint, startGlyph);
        }
    }
}
