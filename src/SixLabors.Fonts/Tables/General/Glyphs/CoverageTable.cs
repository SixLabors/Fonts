// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    /// <summary>
    /// Each subtable (except an Extension LookupType subtable) in a lookup references a Coverage table (Coverage),
    /// which specifies all the glyphs affected by a substitution or positioning operation described in the subtable.
    /// The GSUB, GPOS, and GDEF tables rely on this notion of coverage.
    /// If a glyph does not appear in a Coverage table, the client can skip that subtable and move
    /// immediately to the next subtable.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-table"/>
    /// </summary>
    internal abstract class CoverageTable
    {
        public abstract int CoverageIndexOf(ushort glyphIndex);

        public abstract IEnumerator<ushort> GetEnumerator();

        public static CoverageTable Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort coverageFormat = reader.ReadUInt16();
            return coverageFormat switch
            {
                1 => CoverageFormat1Table.Load(reader),
                2 => CoverageFormat2Table.Load(reader),
                _ => throw new NotSupportedException(),
            };
        }
    }

    internal sealed class CoverageFormat1Table : CoverageTable
    {
        private readonly ushort[] glyphArray;

        private CoverageFormat1Table(ushort[] glyphArray)
            => this.glyphArray = glyphArray;

        public override int CoverageIndexOf(ushort glyphIndex)
        {
            int n = Array.BinarySearch(this.glyphArray, glyphIndex);
            return n < 0 ? -1 : n;
        }

        public override IEnumerator<ushort> GetEnumerator()
            => ((IEnumerable<ushort>)this.glyphArray).GetEnumerator();

        public static CoverageFormat1Table Load(BigEndianBinaryReader reader)
        {
            // +--------+------------------------+-----------------------------------------+
            // | Type   | Name                   | Description                             |
            // +========+========================+=========================================+
            // | uint16 | coverageFormat         | Format identifier — format = 1          |
            // +--------+------------------------+-----------------------------------------+
            // | uint16 | glyphCount             | Number of glyphs in the glyph array     |
            // +--------+------------------------+-----------------------------------------+
            // | uint16 | glyphArray[glyphCount] | Array of glyph IDs — in numerical order |
            // +--------+------------------------+-----------------------------------------+
            ushort glyphCount = reader.ReadUInt16();
            ushort[] glyphArray = reader.ReadUInt16Array(glyphCount);

            return new CoverageFormat1Table(glyphArray);
        }
    }

    internal sealed class CoverageFormat2Table : CoverageTable
    {
        private readonly ushort[] glyphArray;

        private CoverageFormat2Table(ushort[] glyphArray)
            => this.glyphArray = glyphArray;

        public override int CoverageIndexOf(ushort glyphIndex)
        {
            int n = Array.BinarySearch(this.glyphArray, glyphIndex);
            return n < 0 ? -1 : n;
        }

        public override IEnumerator<ushort> GetEnumerator()
            => ((IEnumerable<ushort>)this.glyphArray).GetEnumerator();

        public static CoverageFormat2Table Load(BigEndianBinaryReader reader)
        {
            // +-------------+--------------------------+--------------------------------------------------+
            // | Type        | Name                     | Description                                      |
            // +=============+==========================+==================================================+
            // | uint16      | coverageFormat           | Format identifier — format = 2                   |
            // +-------------+--------------------------+--------------------------------------------------+
            // | uint16      | rangeCount               | Number of RangeRecords                           |
            // +-------------+--------------------------+--------------------------------------------------+
            // | RangeRecord | rangeRecords[rangeCount] | Array of glyph ranges — ordered by startGlyphID. |
            // +-------------+--------------------------+--------------------------------------------------+
            ushort rangeCount = reader.ReadUInt16();

            var glyphArray = new List<ushort>();
            for (int i = 0; i < rangeCount; i++)
            {
                // +--------+--------------------+-------------------------------------------+
                // | Type   | Name               | Description                               |
                // +========+====================+===========================================+
                // | uint16 | startGlyphID       | First glyph ID in the range               |
                // +--------+--------------------+-------------------------------------------+
                // | uint16 | endGlyphID         | Last glyph ID in the range                |
                // +--------+--------------------+-------------------------------------------+
                // | uint16 | startCoverageIndex | Coverage Index of first glyph ID in range |
                // +--------+--------------------+-------------------------------------------+
                ushort startId = reader.ReadUInt16();
                ushort endId = reader.ReadUInt16();
                ushort startCoverageIndex = reader.ReadUInt16();

                for (ushort j = startId; j <= endId; j++)
                {
                    glyphArray.Add(j);
                }
            }

            return new CoverageFormat2Table(glyphArray.ToArray());
        }
    }
}
