// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Gsub
{
    /// <summary>
    /// A Contextual Substitution subtable describes glyph substitutions in context that replace one
    /// or more glyphs within a certain pattern of glyphs.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-5-contextual-substitution-subtable"/>
    /// </summary>
    internal static class LookupType5SubTable
    {
        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort subTableFormat = reader.ReadUInt16();

            return subTableFormat switch
            {
                1 => LookupType5Format1SubTable.Load(reader, offset),
                2 => LookupType5Format2SubTable.Load(reader, offset),
                3 => LookupType5Format3SubTable.Load(reader, offset),
                _ => throw new InvalidFontFileException($"Invalid value for 'subTableFormat' {subTableFormat}. Should be '1', '2', or '3'."),
            };
        }
    }

    internal sealed class LookupType5Format1SubTable : LookupSubTable
    {
        private readonly CoverageTable coverageTable;
        private readonly SequenceRuleSetTable[] seqRuleSetTables;

        private LookupType5Format1SubTable(CoverageTable coverageTable, SequenceRuleSetTable[] seqRuleSetTables)
        {
            this.coverageTable = coverageTable;
            this.seqRuleSetTables = seqRuleSetTables;
        }

        public static LookupType5Format1SubTable Load(BigEndianBinaryReader reader, long offset)
        {
            SequenceRuleSetTable[] seqRuleSets = TableLoadingUtils.LoadSequenceContextFormat1(reader, offset, out CoverageTable coverageTable);

            return new LookupType5Format1SubTable(coverageTable, seqRuleSets);
        }

        public override bool TrySubstitution(GSubTable table, GlyphSubstitutionCollection collection, ushort index, int count)
        {
            ushort glyphId = collection[index][0];
            if (glyphId == 0)
            {
                return false;
            }

            int offset = this.coverageTable.CoverageIndexOf(glyphId);
            if (offset <= -1)
            {
                return false;
            }

            // TODO: Check this.
            // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#example-7-contextual-substitution-format-1
            SequenceRuleSetTable ruleSetTable = this.seqRuleSetTables[offset];
            foreach (SequenceRuleTable ruleTable in ruleSetTable.SequenceRuleTables)
            {
                int remaining = count - 1;
                int seqLength = ruleTable.InputSequence.Length;
                if (seqLength > remaining)
                {
                    continue;
                }

                bool allMatched = AdvancedTypographicUtils.MatchInputSequence(collection, index, ruleTable.InputSequence);
                if (!allMatched)
                {
                    continue;
                }

                // It's a match. Perform substitutions and return true if anything changed.
                bool hasChanged = false;
                foreach (SequenceLookupRecord lookupRecord in ruleTable.SequenceLookupRecords)
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

            return false;
        }
    }

    internal sealed class LookupType5Format2SubTable : LookupSubTable
    {
        private readonly CoverageTable coverageTable;
        private readonly ClassDefinitionTable classDefinitionTable;
        private readonly ClassSequenceRuleSetTable[] sequenceRuleSetTables;

        private LookupType5Format2SubTable(
            ClassSequenceRuleSetTable[] sequenceRuleSetTables,
            ClassDefinitionTable classDefinitionTable,
            CoverageTable coverageTable)
        {
            this.sequenceRuleSetTables = sequenceRuleSetTables;
            this.classDefinitionTable = classDefinitionTable;
            this.coverageTable = coverageTable;
        }

        public static LookupType5Format2SubTable Load(BigEndianBinaryReader reader, long offset)
        {
            CoverageTable coverageTable = TableLoadingUtils.LoadSequenceContextFormat2(reader, offset, out ClassDefinitionTable classDefTable, out ClassSequenceRuleSetTable[] classSeqRuleSets);

            return new LookupType5Format2SubTable(classSeqRuleSets, classDefTable, coverageTable);
        }

        public override bool TrySubstitution(GSubTable table, GlyphSubstitutionCollection collection, ushort index, int count)
        {
            ushort glyphId = collection[index][0];
            if (glyphId == 0)
            {
                return false;
            }

            if (this.coverageTable.CoverageIndexOf(glyphId) <= -1)
            {
                return false;
            }

            // TODO: Check this.
            // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#52-context-substitution-format-2-class-based-glyph-contexts
            int offset = this.classDefinitionTable.ClassIndexOf(glyphId);
            if (offset < 0)
            {
                return false;
            }

            ClassSequenceRuleSetTable ruleSetTable = this.sequenceRuleSetTables[offset];
            foreach (ClassSequenceRuleTable ruleTable in ruleSetTable.SequenceRuleTables)
            {
                int remaining = count - 1;
                int seqLength = ruleTable.InputSequence.Length;
                if (seqLength > remaining)
                {
                    continue;
                }

                bool allMatched = AdvancedTypographicUtils.MatchInputSequence(collection, index, ruleTable.InputSequence);
                if (!allMatched)
                {
                    continue;
                }

                // It's a match. Perform substitutions and return true if anything changed.
                bool hasChanged = false;
                foreach (SequenceLookupRecord lookupRecord in ruleTable.SequenceLookupRecords)
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

            return false;
        }
    }

    internal sealed class LookupType5Format3SubTable : LookupSubTable
    {
        private readonly CoverageTable[] coverageTables;
        private readonly SequenceLookupRecord[] sequenceLookupRecords;

        private LookupType5Format3SubTable(CoverageTable[] coverageTables, SequenceLookupRecord[] sequenceLookupRecords)
        {
            this.coverageTables = coverageTables;
            this.sequenceLookupRecords = sequenceLookupRecords;
        }

        public static LookupType5Format3SubTable Load(BigEndianBinaryReader reader, long offset)
        {
            SequenceLookupRecord[] seqLookupRecords = TableLoadingUtils.LoadSequenceContextFormat3(reader, offset, out CoverageTable[] coverageTables);

            return new LookupType5Format3SubTable(coverageTables, seqLookupRecords);
        }

        public override bool TrySubstitution(GSubTable table, GlyphSubstitutionCollection collection, ushort index, int count)
        {
            ushort glyphId = collection[index][0];
            if (glyphId == 0)
            {
                return false;
            }

            // TODO: Check this
            // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#53-context-substitution-format-3-coverage-based-glyph-contexts
            foreach (CoverageTable coverageTable in this.coverageTables)
            {
                int offset = coverageTable.CoverageIndexOf(glyphId);
                if (offset <= -1)
                {
                    continue;
                }

                // It's a match. Perform substitutions and return true if anything changed.
                bool hasChanged = false;
                foreach (SequenceLookupRecord lookupRecord in this.sequenceLookupRecords)
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

            return false;
        }
    }
}
