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
                SequenceRuleSetTable[] seqRuleSets = TableLoadingUtils.LoadSequenceContextFormat1(reader, offset, out CoverageTable coverageTable);

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
                CoverageTable coverageTable = TableLoadingUtils.LoadSequenceContextFormat2(reader, offset, out ClassDefinitionTable classDefTable, out ClassSequenceRuleSetTable[] classSeqRuleSets);

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
                SequenceLookupRecord[] seqLookupRecords = TableLoadingUtils.LoadSequenceContextFormat3(reader, offset, out CoverageTable[] coverageTables);

                return new LookupType7Format3SubTable(coverageTables, seqLookupRecords);
            }

            public override bool TryUpdatePosition(IFontMetrics fontMetrics, GPosTable table, GlyphPositioningCollection collection, ushort index, int count)
                => throw new System.NotImplementedException();
        }
    }
}
