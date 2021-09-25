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
    internal static class LookupType8SubTable
    {
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
                ChainedSequenceRuleSetTable[] seqRuleSets = TableLoadingUtils.LoadChainedSequenceContextFormat1(reader, offset, out CoverageTable coverageTable);
                return new LookupType8Format1SubTable(coverageTable, seqRuleSets);
            }

            public override bool TryUpdatePosition(
                IFontMetrics fontMetrics,
                GPosTable table,
                GlyphPositioningCollection collection,
                Tag feature,
                ushort index,
                int count)
            {
                // Implements Chained Contexts Substitution, Format 1:
                // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#61-chained-contexts-substitution-format-1-simple-glyph-contexts
                ushort glyphId = collection[index][0];
                if (glyphId == 0)
                {
                    return false;
                }

                // Search for the current glyph in the Coverage table.
                int offset = this.coverageTable.CoverageIndexOf(glyphId);
                if (offset <= -1)
                {
                    return false;
                }

                if (this.seqRuleSetTables is null || this.seqRuleSetTables.Length is 0)
                {
                    return false;
                }

                ChainedSequenceRuleSetTable seqRuleSet = this.seqRuleSetTables[offset];
                if (seqRuleSet is null)
                {
                    return false;
                }

                // Apply ruleset for the given glyph id.
                ChainedSequenceRuleTable[] rules = seqRuleSet.SequenceRuleTables;
                for (int lookupIndex = 0; lookupIndex < rules.Length; lookupIndex++)
                {
                    ChainedSequenceRuleTable rule = rules[lookupIndex];
                    if (!AdvancedTypographicUtils.ApplyChainedSequenceRule(collection, feature, index, rule))
                    {
                        continue;
                    }

                    bool hasChanged = false;
                    for (int j = 0; j < rule.SequenceLookupRecords.Length; j++)
                    {
                        SequenceLookupRecord sequenceLookupRecord = rule.SequenceLookupRecords[j];
                        LookupTable lookup = table.LookupList.LookupTables[sequenceLookupRecord.LookupListIndex];
                        ushort sequenceIndex = sequenceLookupRecord.SequenceIndex;
                        if (lookup.TryUpdatePosition(fontMetrics, table, collection, feature, (ushort)(index + sequenceIndex), 1))
                        {
                            hasChanged = true;
                        }
                    }

                    return hasChanged;
                }

                return false;
            }
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
                ChainedClassSequenceRuleSetTable[] seqRuleSets = TableLoadingUtils.LoadChainedSequenceContextFormat2(
                    reader,
                    offset,
                    out CoverageTable coverageTable,
                    out ClassDefinitionTable backtrackClassDefTable,
                    out ClassDefinitionTable inputClassDefTable,
                    out ClassDefinitionTable lookaheadClassDefTable);

                return new LookupType8Format2SubTable(seqRuleSets, backtrackClassDefTable, inputClassDefTable, lookaheadClassDefTable, coverageTable);
            }

            public override bool TryUpdatePosition(
                IFontMetrics fontMetrics,
                GPosTable table,
                GlyphPositioningCollection collection,
                Tag feature,
                ushort index,
                int count)
            {
                // Implements Chained Contexts Substitution for Format 2:
                // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#62-chained-contexts-substitution-format-2-class-based-glyph-contexts
                ushort glyphId = collection[index][0];
                if (glyphId == 0)
                {
                    return false;
                }

                // Search for the current glyph in the Coverage table.
                int offset = this.coverageTable.CoverageIndexOf(glyphId);
                if (offset <= -1)
                {
                    return false;
                }

                // Search in the class definition table to find the class value assigned to the currently glyph.
                int classId = this.inputClassDefinitionTable.ClassIndexOf(glyphId);
                ChainedClassSequenceRuleTable[]? rules = classId >= 0 && classId < this.sequenceRuleSetTables.Length ? this.sequenceRuleSetTables[classId].SubRules : null;
                if (rules is null)
                {
                    return false;
                }

                // Apply ruleset for the given glyph class id.
                for (int lookupIndex = 0; lookupIndex < rules.Length; lookupIndex++)
                {
                    ChainedClassSequenceRuleTable rule = rules[lookupIndex];
                    if (!AdvancedTypographicUtils.ApplyChainedClassSequenceRule(collection, index, rule, this.inputClassDefinitionTable, this.backtrackClassDefinitionTable, this.lookaheadClassDefinitionTable))
                    {
                        continue;
                    }

                    // It's a match. Perform position update and return true if anything changed.
                    bool hasChanged = false;
                    for (int j = 0; j < rule.SequenceLookupRecords.Length; j++)
                    {
                        SequenceLookupRecord sequenceLookupRecord = rule.SequenceLookupRecords[j];
                        LookupTable lookup = table.LookupList.LookupTables[sequenceLookupRecord.LookupListIndex];
                        ushort sequenceIndex = sequenceLookupRecord.SequenceIndex;
                        if (lookup.TryUpdatePosition(fontMetrics, table, collection, feature, (ushort)(index + sequenceIndex), 1))
                        {
                            hasChanged = true;
                        }
                    }

                    return hasChanged;
                }

                return false;
            }
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
                SequenceLookupRecord[] seqLookupRecords = TableLoadingUtils.LoadChainedSequenceContextFormat3(
                    reader,
                    offset,
                    out CoverageTable[] backtrackCoverageTables,
                    out CoverageTable[] inputCoverageTables,
                    out CoverageTable[] lookaheadCoverageTables);

                return new LookupType8Format3SubTable(seqLookupRecords, backtrackCoverageTables, inputCoverageTables, lookaheadCoverageTables);
            }

            public override bool TryUpdatePosition(
                IFontMetrics fontMetrics,
                GPosTable table,
                GlyphPositioningCollection collection,
                Tag feature,
                ushort index,
                int count)
            {
                ushort glyphId = collection[index][0];
                if (glyphId == 0)
                {
                    return false;
                }

                if (!AdvancedTypographicUtils.CheckAllCoverages(collection, index, count, this.inputCoverageTables, this.backtrackCoverageTables, this.lookaheadCoverageTables))
                {
                    return false;
                }

                // It's a match. Perform position update and return true if anything changed.
                bool hasChanged = false;
                foreach (SequenceLookupRecord lookupRecord in this.seqLookupRecords)
                {
                    ushort sequenceIndex = lookupRecord.SequenceIndex;
                    ushort lookupIndex = lookupRecord.LookupListIndex;

                    LookupTable lookup = table.LookupList.LookupTables[lookupIndex];
                    if (lookup.TryUpdatePosition(fontMetrics, table, collection, feature, (ushort)(index + sequenceIndex), count - sequenceIndex))
                    {
                        hasChanged = true;
                    }
                }

                return hasChanged;
            }
        }
    }
}
