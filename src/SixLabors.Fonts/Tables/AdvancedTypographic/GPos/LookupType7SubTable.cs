// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using SixLabors.Fonts.Tables.AdvancedTypographic.Gsub;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    /// <summary>
    /// Lookup Type 7: Contextual Positioning Subtables.
    /// A Contextual Positioning subtable describes glyph positioning in context so a text-processing client can adjust the position
    /// of one or more glyphs within a certain pattern of glyphs.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-7-contextual-positioning-subtables"/>
    /// </summary>
    internal sealed class LookupType7SubTable
    {
        internal LookupType7SubTable()
        {
        }

        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort subTableFormat = reader.ReadUInt16();

            return subTableFormat switch
            {
                1 => LookupType7Format1SubTable.Load(reader, offset),
                2 => LookupType7Format2SubTable.Load(reader, offset),
                3 => LookupType7Format3SubTable.Load(reader, offset),
                _ => throw new InvalidFontFileException(
                    $"Invalid value for 'subTableFormat' {subTableFormat}. Should be '1', '2' or 3."),
            };
        }

        internal sealed class LookupType7Format1SubTable : LookupSubTable
        {
            private readonly CoverageTable coverageTable;
            private readonly SequenceRuleSetTable[] seqRuleSetTables;

            public LookupType7Format1SubTable(CoverageTable coverageTable, SequenceRuleSetTable[] seqRuleSetTables)
            {
                this.seqRuleSetTables = seqRuleSetTables;
                this.coverageTable = coverageTable;
            }

            public static LookupType7Format1SubTable Load(BigEndianBinaryReader reader, long offset)
            {
                // Context Positioning Subtable Format 1: Simple Glyph Contexts.
                // +----------+------------------------------------+---------------------------------------------------------------+
                // | Type     | Name                               | Description                                                   |
                // +==========+====================================+===============================================================+
                // | uint16   | format                             | Format identifier: format = 1                                 |
                // +----------+------------------------------------+---------------------------------------------------------------+
                // | Offset16 | coverageOffset                     | Offset to Coverage table, from beginning of                   |
                // |          |                                    | SequenceContextFormat1 table                                  |
                // +----------+------------------------------------+---------------------------------------------------------------+
                // | uint16   | seqRuleSetCount                    | Number of SequenceRuleSet tables                              |
                // +----------+------------------------------------+---------------------------------------------------------------+
                // | Offset16 | seqRuleSetOffsets[seqRuleSetCount] | Array of offsets to SequenceRuleSet tables, from beginning of |
                // |          |                                    | SequenceContextFormat1 table (offsets may be NULL)            |
                // +----------+------------------------------------+---------------------------------------------------------------+
                SequenceRuleSetTable[] seqRuleSets = AdvancedTypographicUtils.LoadSequenceContextFormat1(reader, offset, out CoverageTable coverageTable);

                return new LookupType7Format1SubTable(coverageTable, seqRuleSets);
            }

            public override bool TryUpdatePosition(IFontMetrics fontMetrics, GPosTable table, GlyphPositioningCollection collection, ushort index, int count)
                => throw new System.NotImplementedException();
        }

        internal sealed class LookupType7Format2SubTable : LookupSubTable
        {
            private readonly CoverageTable coverageTable;
            private readonly ClassDefinitionTable classDefinitionTable;
            private readonly ClassSequenceRuleSetTable[] sequenceRuleSetTables;

            public LookupType7Format2SubTable(CoverageTable coverageTable, ClassDefinitionTable classDefinitionTable, ClassSequenceRuleSetTable[] sequenceRuleSetTables)
            {
                this.coverageTable = coverageTable;
                this.classDefinitionTable = classDefinitionTable;
                this.sequenceRuleSetTables = sequenceRuleSetTables;
            }

            public static LookupType7Format2SubTable Load(BigEndianBinaryReader reader, long offset)
            {
                // Context Positioning Subtable Format 2: Class-based Glyph Contexts.
                // +----------+----------------------------------------------+--------------------------------------------------------------------+
                // | Type     | Name                                         | Description                                                        |
                // +==========+==============================================+====================================================================+
                // | uint16   | format                                       | Format identifier: format = 2                                      |
                // +----------+----------------------------------------------+--------------------------------------------------------------------+
                // | Offset16 | coverageOffset                               | Offset to Coverage table, from beginning of                        |
                // |          |                                              | SequenceContextFormat2 table.                                      |
                // +----------+----------------------------------------------+--------------------------------------------------------------------+
                // | Offset16 | classDefOffset                               | Offset to ClassDef table, from beginning of                        |
                // |          |                                              | SequenceContextFormat2 table.                                      |
                // +----------+----------------------------------------------+--------------------------------------------------------------------+
                // | uint16   | classSeqRuleSetCount                         | Number of ClassSequenceRuleSet tables.                             |
                // +----------+----------------------------------------------+--------------------------------------------------------------------+
                // | Offset16 | classSeqRuleSetOffsets[classSeqRuleSetCount] | Array of offsets to ClassSequenceRuleSet tables, from beginning of |
                // |          |                                              | SequenceContextFormat2 table (may be NULL)                         |
                // +----------+----------------------------------------------+--------------------------------------------------------------------+
                CoverageTable coverageTable = AdvancedTypographicUtils.LoadSequenceContextFormat2(reader, offset, out ClassDefinitionTable classDefTable, out ClassSequenceRuleSetTable[] classSeqRuleSets);

                return new LookupType7Format2SubTable(coverageTable, classDefTable, classSeqRuleSets);
            }

            public override bool TryUpdatePosition(IFontMetrics fontMetrics, GPosTable table, GlyphPositioningCollection collection, ushort index, int count)
                => throw new System.NotImplementedException();
        }

        internal sealed class LookupType7Format3SubTable : LookupSubTable
        {
            private readonly CoverageTable[] coverageTables;
            private readonly SequenceLookupRecord[] sequenceLookupRecords;

            public LookupType7Format3SubTable(CoverageTable[] coverageTables, SequenceLookupRecord[] sequenceLookupRecords)
            {
                this.coverageTables = coverageTables;
                this.sequenceLookupRecords = sequenceLookupRecords;
            }

            public static LookupType7Format3SubTable Load(BigEndianBinaryReader reader, long offset)
            {
                // Context Positioning Subtable Format 3: Coverage-based Glyph Contexts.
                // +----------------------+----------------------------------+-------------------------------------------+
                // | Type                 | Name                             | Description                               |
                // +======================+==================================+===========================================+
                // | uint16               | format                           | Format identifier: format = 3             |
                // +----------------------+----------------------------------+-------------------------------------------+
                // | uint16               | glyphCount                       | Number of glyphs in the input sequence.   |
                // +----------------------+----------------------------------+-------------------------------------------+
                // | uint16               | seqLookupCount                   | Number of SequenceLookupRecords.          |
                // +----------------------+----------------------------------+-------------------------------------------+
                // | Offset16             | coverageOffsets[glyphCount]      | Array of offsets to Coverage tables, from |
                // |                      |                                  | beginning of SequenceContextFormat3       |
                // |                      |                                  | subtable.                                 |
                // +----------------------+----------------------------------+-------------------------------------------+
                // | SequenceLookupRecord | seqLookupRecords[seqLookupCount] | Array of SequenceLookupRecords.           |
                // +----------------------+----------------------------------+-------------------------------------------+
                SequenceLookupRecord[] seqLookupRecords = AdvancedTypographicUtils.LoadSequenceContextFormat3(reader, offset, out CoverageTable[] coverageTables);

                return new LookupType7Format3SubTable(coverageTables, seqLookupRecords);
            }

            public override bool TryUpdatePosition(IFontMetrics fontMetrics, GPosTable table, GlyphPositioningCollection collection, ushort index, int count)
                => throw new System.NotImplementedException();
        }
    }
}
