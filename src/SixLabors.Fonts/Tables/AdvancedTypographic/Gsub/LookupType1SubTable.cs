// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Gsub
{
    /// <summary>
    /// Single substitution (SingleSubst) subtables tell a client to replace a single glyph with another glyph.
    /// The subtables can be either of two formats. Both formats require two distinct sets of glyph indices:
    /// one that defines input glyphs (specified in the Coverage table), and one that defines the output glyphs.
    /// Format 1 requires less space than Format 2, but it is less flexible.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-1-single-substitution-subtable"/>
    /// </summary>
    internal static class LookupType1SubTable
    {
        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort substFormat = reader.ReadUInt16();

            return substFormat switch
            {
                1 => LookupType1Format1SubTable.Load(reader, offset),
                2 => LookupType1Format2SubTable.Load(reader, offset),
                _ => throw new InvalidFontFileException($"Invalid value for 'substFormat' {substFormat}. Should be '1' or '2'.")
            };
        }
    }

    internal sealed class LookupType1Format1SubTable : LookupSubTable
    {
        private readonly ushort deltaGlyphId;
        private readonly CoverageTable coverageTable;

        private LookupType1Format1SubTable(ushort deltaGlyphId, CoverageTable coverageTable)
        {
            this.deltaGlyphId = deltaGlyphId;
            this.coverageTable = coverageTable;
        }

        public static LookupType1Format1SubTable Load(BigEndianBinaryReader reader, long offset)
        {
            // SingleSubstFormat1
            // +----------+----------------+----------------------------------------------------------+
            // | Type     | Name           | Description                                              |
            // +==========+================+==========================================================+
            // | uint16   | substFormat    | Format identifier: format = 1                            |
            // +----------+----------------+----------------------------------------------------------+
            // | Offset16 | coverageOffset | Offset to Coverage table, from beginning of substitution |
            // |          |                | subtable                                                 |
            // +----------+----------------+----------------------------------------------------------+
            // | int16    | deltaGlyphID   | Add to original glyph ID to get substitute glyph ID      |
            // +----------+----------------+----------------------------------------------------------+
            ushort coverageOffset = reader.ReadOffset16();
            ushort deltaGlyphId = reader.ReadUInt16();
            var coverageTable = CoverageTable.Load(reader, offset + coverageOffset);

            return new LookupType1Format1SubTable(deltaGlyphId, coverageTable);
        }

        public override bool TrySubstitution(
            FontMetrics fontMetrics,
            GSubTable table,
            GlyphSubstitutionCollection collection,
            Tag feature,
            ushort index,
            int count)
        {
            ushort glyphId = collection[index][0];
            if (glyphId == 0)
            {
                return false;
            }

            if (this.coverageTable.CoverageIndexOf(glyphId) > -1)
            {
                collection.Replace(index, (ushort)(glyphId + this.deltaGlyphId));
                return true;
            }

            return false;
        }
    }

    internal sealed class LookupType1Format2SubTable : LookupSubTable
    {
        private readonly CoverageTable coverageTable;
        private readonly ushort[] substituteGlyphs;

        private LookupType1Format2SubTable(ushort[] substituteGlyphs, CoverageTable coverageTable)
        {
            this.substituteGlyphs = substituteGlyphs;
            this.coverageTable = coverageTable;
        }

        public static LookupType1Format2SubTable Load(BigEndianBinaryReader reader, long offset)
        {
            // SingleSubstFormat2
            // +----------+--------------------------------+-----------------------------------------------------------+
            // | Type     | Name                           | Description                                               |
            // +==========+================================+===========================================================+
            // | uint16   | substFormat                    | Format identifier: format = 2                             |
            // +----------+--------------------------------+-----------------------------------------------------------+
            // | Offset16 | coverageOffset                 | Offset to Coverage table, from beginning of substitution  |
            // |          |                                | subtable                                                  |
            // +----------+--------------------------------+-----------------------------------------------------------+
            // | uint16   | glyphCount                     | Number of glyph IDs in the substituteGlyphIDs array       |
            // +----------+--------------------------------+-----------------------------------------------------------+
            // | uint16   | substituteGlyphIDs[glyphCount] | Array of substitute glyph IDs — ordered by Coverage index |
            // +----------+--------------------------------+-----------------------------------------------------------+
            ushort coverageOffset = reader.ReadOffset16();
            ushort glyphCount = reader.ReadUInt16();
            ushort[] substituteGlyphIds = reader.ReadUInt16Array(glyphCount);
            var coverageTable = CoverageTable.Load(reader, offset + coverageOffset);

            return new LookupType1Format2SubTable(substituteGlyphIds, coverageTable);
        }

        public override bool TrySubstitution(
            FontMetrics fontMetrics,
            GSubTable table,
            GlyphSubstitutionCollection collection,
            Tag feature,
            ushort index,
            int count)
        {
            ushort glyphId = collection[index][0];
            if (glyphId == 0)
            {
                return false;
            }

            int offset = this.coverageTable.CoverageIndexOf(glyphId);

            if (offset > -1)
            {
                collection.Replace(index, this.substituteGlyphs[offset]);
                return true;
            }

            return false;
        }
    }
}
