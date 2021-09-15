// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    /// <summary>
    /// The CursivePosFormat1 subtable begins with a format identifier (posFormat) and an offset to a Coverage table (coverageOffset),
    /// which lists all the glyphs that define cursive attachment data. In addition, the subtable contains one EntryExitRecord for each glyph listed in the Coverage table,
    /// a count of those records (entryExitCount), and an array of those records in the same order as the Coverage Index (entryExitRecords).
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#cursive-attachment-positioning-format1-cursive-attachment"/>
    /// </summary>
    internal sealed class LookupType3SubTable
    {
        private LookupType3SubTable()
        {
        }

        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort posFormat = reader.ReadUInt16();

            return posFormat switch
            {
                1 => LookupType3Format1SubTable.Load(reader, offset),
                _ => throw new InvalidFontFileException(
                    $"Invalid value for 'posFormat' {posFormat}. Should be '1'.")
            };
        }

        internal sealed class LookupType3Format1SubTable : LookupSubTable
        {
            private readonly CoverageTable coverageTable;
            private readonly EntryExitRecord[] entryExitRecords;

            public LookupType3Format1SubTable(CoverageTable coverageTable, EntryExitRecord[] entryExitRecords)
            {
                this.coverageTable = coverageTable;
                this.entryExitRecords = entryExitRecords;
            }

            public static LookupType3Format1SubTable Load(BigEndianBinaryReader reader, long offset)
            {
                // Cursive Attachment Positioning Format1.
                // +--------------------+---------------------------------+------------------------------------------------------+
                // | Type               |  Name                           | Description                                          |
                // +====================+=================================+======================================================+
                // | uint16             | posFormat                       | Format identifier: format = 1                        |
                // +--------------------+---------------------------------+------------------------------------------------------+
                // | Offset16           | coverageOffset                  | Offset to Coverage table,                            |
                // |                    |                                 | from beginning of CursivePos subtable.               |
                // +--------------------+---------------------------------+------------------------------------------------------+
                // | uint16             | entryExitCount                  | Number of EntryExit records                          |
                // +--------------------+---------------------------------+------------------------------------------------------+
                // | EntryExitRecord    | entryExitRecord[entryExitCount] | Array of EntryExit records, in Coverage index order. |
                // +--------------------+---------------------------------+------------------------------------------------------+
                ushort coverageOffset = reader.ReadOffset16();
                ushort entryExitCount = reader.ReadUInt16();
                var entryExitRecords = new EntryExitRecord[entryExitCount];
                for (int i = 0; i < entryExitCount; i++)
                {
                    entryExitRecords[i] = new EntryExitRecord(reader);
                }

                var coverageTable = CoverageTable.Load(reader, offset + coverageOffset);

                return new LookupType3Format1SubTable(coverageTable, entryExitRecords);
            }

            public override bool TryUpdatePosition(IFontMetrics fontMetrics, GPosTable table, GlyphPositioningCollection collection, ushort index, int count) => throw new System.NotImplementedException();
        }
    }
}
