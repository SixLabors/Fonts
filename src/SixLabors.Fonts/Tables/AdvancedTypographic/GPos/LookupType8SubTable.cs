// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    /// <summary>
    /// LookupType 8: Chained Contexts Positioning Subtable.
    /// A Chained Contexts Positioning subtable describes glyph positioning in context with an ability to look back and/or look ahead in the sequence of glyphs.
    /// The design of the Chained Contexts Positioning subtable is parallel to that of the Contextual Positioning subtable, including the availability of three formats.
    /// Each format can describe one or more chained backtrack, input, and lookahead sequence combinations, and one or more positioning adjustments for glyphs in each input sequence.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookuptype-8-chained-contexts-positioning-subtable"/>
    /// </summary>
    internal sealed class LookupType8SubTable
    {
        internal LookupType8SubTable()
        {
        }

        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort substFormat = reader.ReadUInt16();

            return substFormat switch
            {
                1 => LookupType8Format1SubTable.Load(reader, offset),
                2 => LookupType8Format2SubTable.Load(reader, offset),
                3 => LookupType8Format3SubTable.Load(reader, offset),
                _ => throw new InvalidFontFileException($"Invalid value for 'subTableFormat' {substFormat}. Should be '1', '2', or '3'."),
            };
        }

        internal sealed class LookupType8Format1SubTable : LookupSubTable
        {
            private readonly CoverageTable coverageTable;
            private readonly ChainedSequenceRuleSetTable[] seqRuleSetTables;

            private LookupType8Format1SubTable(CoverageTable coverageTable, ChainedSequenceRuleSetTable[] seqRuleSetTables)
            {
                this.coverageTable = coverageTable;
                this.seqRuleSetTables = seqRuleSetTables;
            }

            public static LookupType8Format1SubTable Load(BigEndianBinaryReader reader, long offset)
            {
                ChainedSequenceRuleSetTable[] seqRuleSets = AdvancedTypographicUtils.LoadChainedSequenceContextFormat1(reader, offset, out CoverageTable coverageTable);
                return new LookupType8Format1SubTable(coverageTable, seqRuleSets);
            }

            public override bool TryUpdatePosition(IFontMetrics fontMetrics, GPosTable table, GlyphPositioningCollection collection, ushort index, int count)
                => throw new System.NotImplementedException();
        }

        internal sealed class LookupType8Format2SubTable : LookupSubTable
        {
            private readonly CoverageTable coverageTable;
            private readonly ClassDefinitionTable inputClassDefinitionTable;
            private readonly ClassDefinitionTable backtrackClassDefinitionTable;
            private readonly ClassDefinitionTable lookaheadClassDefinitionTable;
            private readonly ChainedClassSequenceRuleSetTable[] sequenceRuleSetTables;

            private LookupType8Format2SubTable(
                ChainedClassSequenceRuleSetTable[] sequenceRuleSetTables,
                ClassDefinitionTable backtrackClassDefinitionTable,
                ClassDefinitionTable inputClassDefinitionTable,
                ClassDefinitionTable lookaheadClassDefinitionTable,
                CoverageTable coverageTable)
            {
                this.sequenceRuleSetTables = sequenceRuleSetTables;
                this.backtrackClassDefinitionTable = backtrackClassDefinitionTable;
                this.inputClassDefinitionTable = inputClassDefinitionTable;
                this.lookaheadClassDefinitionTable = lookaheadClassDefinitionTable;
                this.coverageTable = coverageTable;
            }

            public static LookupType8Format2SubTable Load(BigEndianBinaryReader reader, long offset)
            {
                ChainedClassSequenceRuleSetTable[] seqRuleSets = AdvancedTypographicUtils.LoadChainedSequenceContextFormat2(
                    reader,
                    offset,
                    out CoverageTable coverageTable,
                    out ClassDefinitionTable backtrackClassDefTable,
                    out ClassDefinitionTable inputClassDefTable,
                    out ClassDefinitionTable lookaheadClassDefTable);

                return new LookupType8Format2SubTable(seqRuleSets, backtrackClassDefTable, inputClassDefTable, lookaheadClassDefTable, coverageTable);
            }

            public override bool TryUpdatePosition(IFontMetrics fontMetrics, GPosTable table, GlyphPositioningCollection collection, ushort index, int count)
                => throw new System.NotImplementedException();
        }

        internal sealed class LookupType8Format3SubTable : LookupSubTable
        {
            private readonly SequenceLookupRecord[] seqLookupRecords;
            private readonly CoverageTable[] backtrackCoverageTables;
            private readonly CoverageTable[] inputCoverageTables;
            private readonly CoverageTable[] lookaheadCoverageTables;

            private LookupType8Format3SubTable(
                SequenceLookupRecord[] seqLookupRecords,
                CoverageTable[] backtrackCoverageTables,
                CoverageTable[] inputCoverageTables,
                CoverageTable[] lookaheadCoverageTables)
            {
                this.seqLookupRecords = seqLookupRecords;
                this.backtrackCoverageTables = backtrackCoverageTables;
                this.inputCoverageTables = inputCoverageTables;
                this.lookaheadCoverageTables = lookaheadCoverageTables;
            }

            public static LookupType8Format3SubTable Load(BigEndianBinaryReader reader, long offset)
            {
                SequenceLookupRecord[] seqLookupRecords = AdvancedTypographicUtils.LoadChainedSequenceContextFormat3(
                    reader,
                    offset,
                    out CoverageTable[] backtrackCoverageTables,
                    out CoverageTable[] inputCoverageTables,
                    out CoverageTable[] lookaheadCoverageTables);

                return new LookupType8Format3SubTable(seqLookupRecords, backtrackCoverageTables, inputCoverageTables, lookaheadCoverageTables);
            }

            public override bool TryUpdatePosition(IFontMetrics fontMetrics, GPosTable table, GlyphPositioningCollection collection, ushort index, int count)
            {
                int glyphId = collection[index][0].GlyphId;
                if (glyphId < 0)
                {
                    return false;
                }

                int inputLength = this.inputCoverageTables.Length;

                // Check that there are enough context glyphs.
                if (index < this.backtrackCoverageTables.Length
                    || inputLength + this.lookaheadCoverageTables.Length > count)
                {
                    return false;
                }

                // Check all coverages: if any of them does not match, abort update.
                for (int i = 0; i < this.inputCoverageTables.Length; ++i)
                {
                    int id = collection[index + i][0].GlyphId;
                    if (id < 0 || this.inputCoverageTables[i].CoverageIndexOf((ushort)id) < 0)
                    {
                        return false;
                    }
                }

                for (int i = 0; i < this.backtrackCoverageTables.Length; ++i)
                {
                    int id = collection[index - 1 - i][0].GlyphId;
                    if (id < 0 || this.backtrackCoverageTables[i].CoverageIndexOf((ushort)id) < 0)
                    {
                        return false;
                    }
                }

                for (int i = 0; i < this.lookaheadCoverageTables.Length; ++i)
                {
                    int id = collection[index + inputLength + i][0].GlyphId;
                    if (id < 0 || this.lookaheadCoverageTables[i].CoverageIndexOf((ushort)id) < 0)
                    {
                        return false;
                    }
                }

                // It's a match. Perform position update and return true if anything changed.
                bool hasChanged = false;
                foreach (SequenceLookupRecord lookupRecord in this.seqLookupRecords)
                {
                    ushort sequenceIndex = lookupRecord.SequenceIndex;
                    ushort lookupIndex = lookupRecord.LookupListIndex;

                    LookupTable lookup = table.LookupList.LookupTables[lookupIndex];
                    if (lookup.TryUpdatePosition(fontMetrics, table, collection, (ushort)(index + sequenceIndex), count - sequenceIndex))
                    {
                        hasChanged = true;
                    }
                }

                return hasChanged;
            }
        }
    }
}
