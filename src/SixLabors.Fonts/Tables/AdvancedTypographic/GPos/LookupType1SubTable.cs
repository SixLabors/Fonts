// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    internal sealed class LookupType1SubTable
    {
        private LookupType1SubTable()
        {
        }

        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort substFormat = reader.ReadUInt16();

            return substFormat switch
            {
                1 => LookupType1Format1SubTable.Load(reader, offset),

                // 2 => LookupType1Format2SubTable.Load(reader, offset),
                _ => throw new InvalidFontFileException($"Invalid value for 'substFormat' {substFormat}. Should be '1' or '2'.")
            };
        }
    }

    internal sealed class LookupType1Format1SubTable : LookupSubTable
    {
        private readonly ValueRecord valueRecord;
        private readonly CoverageTable coverageTable;

        private LookupType1Format1SubTable(ValueRecord valueRecord, CoverageTable coverageTable)
        {
            this.valueRecord = valueRecord;
            this.coverageTable = coverageTable;
        }

        public static LookupType1Format1SubTable Load(BigEndianBinaryReader reader, long offset)
        {
            // SinglePosFormat1
            // +-------------+----------------+-----------------------------------------------+
            // | Type        | Name           | Description                                   |
            // +=============+================+===============================================+
            // | uint16      | posFormat      | Format identifier: format = 1                 |
            // +-------------+----------------+-----------------------------------------------+
            // | Offset16    | coverageOffset | Offset to Coverage table, from beginning      |
            // |             |                | of SinglePos subtable.                        |
            // +-------------+----------------+-----------------------------------------------+
            // | uint16      | valueFormat    | Defines the types of data in the ValueRecord. |
            // +-------------+----------------+-----------------------------------------------+
            // | ValueRecord | valueRecord    | Defines positioning value(s) — applied to     |
            // |             |                | all glyphs in the Coverage table.             |
            // +-------------+----------------+-----------------------------------------------+
            ushort coverageOffset = reader.ReadOffset16();
            ValueFormat valueFormat = reader.ReadUInt16<ValueFormat>();
            var valueRecord = new ValueRecord(reader, valueFormat);

            var coverageTable = CoverageTable.Load(reader, offset + coverageOffset);

            return new LookupType1Format1SubTable(valueRecord, coverageTable);
        }

        public override bool TryUpdatePosition(
            IFontMetrics fontMetrics,
            GPosTable table,
            GlyphPositioningCollection collection,
            ushort index,
            int count)
        {
            for (ushort i = 0; i < count; i++)
            {
                int glyphId = collection.GetGlyphIds(i + index)[0];
                if (glyphId < 0)
                {
                    return false;
                }

                int coverage = this.coverageTable.CoverageIndexOf((ushort)glyphId);
                if (coverage > -1)
                {
                    ValueRecord record = this.valueRecord;
                    collection.Offset(fontMetrics, i, (ushort)glyphId, record.XPlacement, record.YPlacement);
                    collection.Offset(fontMetrics, i, (ushort)glyphId, record.XAdvance, record.YAdvance);
                    return true;
                }
            }

            return false;
        }
    }
}
