// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Gsub
{
    /// <summary>
    /// A Chained Contexts Substitution subtable describes glyph substitutions in context
    /// with an ability to look back and/or look ahead in the sequence of glyphs.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-6-chained-contexts-substitution-subtable"/>
    /// </summary>
    internal static class LookupType6SubTable
    {
        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort substFormat = reader.ReadUInt16();

            return substFormat switch
            {
                1 => LookupType6Format1SubTable.Load(reader, offset),
                2 => LookupType6Format2SubTable.Load(reader, offset),
                3 => LookupType6Format3SubTable.Load(reader, offset),
                _ => throw new InvalidFontFileException($"Invalid value for 'subTableFormat' {substFormat}. Should be '1', '2', or '3'."),
            };
        }
    }

    internal sealed class LookupType6Format1SubTable : LookupSubTable
    {
        private readonly CoverageTable coverageTable;
        private readonly ChainedSequenceRuleSetTable[] seqRuleSetTables;

        private LookupType6Format1SubTable(CoverageTable coverageTable, ChainedSequenceRuleSetTable[] seqRuleSetTables)
        {
            this.coverageTable = coverageTable;
            this.seqRuleSetTables = seqRuleSetTables;
        }

        public static LookupType6Format1SubTable Load(BigEndianBinaryReader reader, long offset)
        {
            ChainedSequenceRuleSetTable[] seqRuleSets = TableLoadingUtils.LoadChainedSequenceContextFormat1(reader, offset, out CoverageTable coverageTable);
            return new LookupType6Format1SubTable(coverageTable, seqRuleSets);
        }

        public override bool TrySubstitution(GSubTable table, GlyphSubstitutionCollection collection, ushort index, int count)
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

            ChainedSequenceRuleSetTable seqRuleSet = this.seqRuleSetTables[index];
            if (seqRuleSet is null || seqRuleSet.SequenceRuleTables.Length is 0)
            {
                return false;
            }

            // Apply ruleset for the given glyph id.
            ChainedSequenceRuleTable[] rules = seqRuleSet.SequenceRuleTables;
            for (int lookupIndex = 0; lookupIndex < rules.Length; lookupIndex++)
            {
                ChainedSequenceRuleTable rule = rules[lookupIndex];
                if (rule.BacktrackSequence.Length > 0
                    && !AdvancedTypographicUtils.MatchSequence(collection, index, rule.BacktrackSequence.Length, rule.InputSequence))
                {
                    continue;
                }

                if (rule.InputSequence.Length > 0
                    && !AdvancedTypographicUtils.MatchInputSequence(collection, index, rule.InputSequence))
                {
                    continue;
                }

                if (rule.LookaheadSequence.Length > 0
                    && !AdvancedTypographicUtils.MatchSequence(collection, index, 1 + rule.InputSequence.Length, rule.LookaheadSequence))
                {
                    continue;
                }

                LookupTable lookup = table.LookupList.LookupTables[lookupIndex];
                if (lookup.TrySubstitution(table, collection, (ushort)lookupIndex, 1))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class LookupType6Format2SubTable : LookupSubTable
    {
        private readonly CoverageTable coverageTable;
        private readonly ClassDefinitionTable inputClassDefinitionTable;
        private readonly ClassDefinitionTable backtrackClassDefinitionTable;
        private readonly ClassDefinitionTable lookaheadClassDefinitionTable;
        private readonly ChainedClassSequenceRuleSetTable[] sequenceRuleSetTables;

        private LookupType6Format2SubTable(
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

        public static LookupType6Format2SubTable Load(BigEndianBinaryReader reader, long offset)
        {
            ChainedClassSequenceRuleSetTable[] seqRuleSets = TableLoadingUtils.LoadChainedSequenceContextFormat2(
                reader,
                offset,
                out CoverageTable coverageTable,
                out ClassDefinitionTable backtrackClassDefTable,
                out ClassDefinitionTable inputClassDefTable,
                out ClassDefinitionTable lookaheadClassDefTable);

            return new LookupType6Format2SubTable(seqRuleSets, backtrackClassDefTable, inputClassDefTable, lookaheadClassDefTable, coverageTable);
        }

        public override bool TrySubstitution(GSubTable table, GlyphSubstitutionCollection collection, ushort index, int count)
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
                if (rule.BacktrackSequence.Length > 0
                    && !AdvancedTypographicUtils.MatchClassSequence(collection, index, rule.BacktrackSequence.Length, rule.BacktrackSequence, this.backtrackClassDefinitionTable))
                {
                    continue;
                }

                if (rule.InputSequence.Length > 0 &&
                    !AdvancedTypographicUtils.MatchInputSequence(collection, index, rule.InputSequence))
                {
                    continue;
                }

                if (rule.LookaheadSequence.Length > 0
                    && !AdvancedTypographicUtils.MatchClassSequence(collection, index, 1 + rule.InputSequence.Length, rule.LookaheadSequence, this.lookaheadClassDefinitionTable))
                {
                    continue;
                }

                LookupTable lookup = table.LookupList.LookupTables[lookupIndex];
                if (lookup.TrySubstitution(table, collection, (ushort)lookupIndex, 1))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class LookupType6Format3SubTable : LookupSubTable
    {
        private readonly SequenceLookupRecord[] seqLookupRecords;
        private readonly CoverageTable[] backtrackCoverageTables;
        private readonly CoverageTable[] inputCoverageTables;
        private readonly CoverageTable[] lookaheadCoverageTables;

        private LookupType6Format3SubTable(
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

        public static LookupType6Format3SubTable Load(BigEndianBinaryReader reader, long offset)
        {
            SequenceLookupRecord[] seqLookupRecords = TableLoadingUtils.LoadChainedSequenceContextFormat3(
                reader,
                offset,
                out CoverageTable[] backtrackCoverageTables,
                out CoverageTable[] inputCoverageTables,
                out CoverageTable[] lookaheadCoverageTables);

            return new LookupType6Format3SubTable(seqLookupRecords, backtrackCoverageTables, inputCoverageTables, lookaheadCoverageTables);
        }

        public override bool TrySubstitution(GSubTable table, GlyphSubstitutionCollection collection, ushort index, int count)
        {
            ushort glyphId = collection[index][0];
            if (glyphId == 0)
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

            // Check all coverages: if any of them does not match, abort substitution.
            for (int i = 0; i < this.inputCoverageTables.Length; ++i)
            {
                ushort id = collection[index + i][0];
                if (id == 0 || this.inputCoverageTables[i].CoverageIndexOf(id) < 0)
                {
                    return false;
                }
            }

            for (int i = 0; i < this.backtrackCoverageTables.Length; ++i)
            {
                ushort id = collection[index - 1 - i][0];
                if (id == 0 || this.backtrackCoverageTables[i].CoverageIndexOf(id) < 0)
                {
                    return false;
                }
            }

            for (int i = 0; i < this.lookaheadCoverageTables.Length; ++i)
            {
                ushort id = collection[index + inputLength + i][0];
                if (id == 0 || this.lookaheadCoverageTables[i].CoverageIndexOf(id) < 0)
                {
                    return false;
                }
            }

            // It's a match. Perform substitutions and return true if anything changed.
            bool hasChanged = false;
            foreach (SequenceLookupRecord lookupRecord in this.seqLookupRecords)
            {
                ushort sequenceIndex = lookupRecord.SequenceIndex;
                ushort lookupIndex = lookupRecord.LookupListIndex;

                LookupTable lookup = table.LookupList.LookupTables[lookupIndex];
                if (lookup.TrySubstitution(table, collection, (ushort)(index + sequenceIndex), count - sequenceIndex))
                {
                    hasChanged = true;
                }
            }

            return hasChanged;
        }
    }
}
