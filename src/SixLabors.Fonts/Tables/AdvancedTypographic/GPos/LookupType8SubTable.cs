// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos;

/// <summary>
/// LookupType 8: Chained Contexts Positioning Subtable.
/// A Chained Contexts Positioning subtable describes glyph positioning in context with an ability to look back and/or look ahead in the sequence of glyphs.
/// The design of the Chained Contexts Positioning subtable is parallel to that of the Contextual Positioning subtable, including the availability of three formats.
/// Each format can describe one or more chained backtrack, input, and lookahead sequence combinations, and one or more positioning adjustments for glyphs in each input sequence.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookuptype-8-chained-contexts-positioning-subtable"/>
/// </summary>
internal static class LookupType8SubTable
{
    /// <summary>
    /// Loads the chaining context positioning subtable from the specified reader.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the subtable.</param>
    /// <param name="lookupFlags">The lookup qualifiers.</param>
    /// <param name="markFilteringSet">The mark filtering set index.</param>
    /// <returns>The loaded <see cref="LookupSubTable"/>.</returns>
    public static LookupSubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        reader.Seek(offset, SeekOrigin.Begin);
        ushort substFormat = reader.ReadUInt16();

        return substFormat switch
        {
            1 => LookupType8Format1SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            2 => LookupType8Format2SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            3 => LookupType8Format3SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            _ => new NotImplementedSubTable(),
        };
    }

    /// <summary>
    /// Chained Context Positioning Format 1: simple glyph contexts.
    /// </summary>
    internal sealed class LookupType8Format1SubTable : LookupSubTable
    {
        private readonly CoverageTable coverageTable;
        private readonly ChainedSequenceRuleSetTable[] seqRuleSetTables;

        /// <summary>
        /// Initializes a new instance of the <see cref="LookupType8Format1SubTable"/> class.
        /// </summary>
        /// <param name="coverageTable">The coverage table.</param>
        /// <param name="seqRuleSetTables">The array of chained sequence rule set tables.</param>
        /// <param name="lookupFlags">The lookup qualifiers.</param>
        /// <param name="markFilteringSet">The mark filtering set index.</param>
        private LookupType8Format1SubTable(
            CoverageTable coverageTable,
            ChainedSequenceRuleSetTable[] seqRuleSetTables,
            LookupFlags lookupFlags,
            ushort markFilteringSet)
            : base(lookupFlags, markFilteringSet)
        {
            this.coverageTable = coverageTable;
            this.seqRuleSetTables = seqRuleSetTables;
        }

        /// <summary>
        /// Loads the Format 1 chained context positioning subtable.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="offset">The offset to the beginning of the subtable.</param>
        /// <param name="lookupFlags">The lookup qualifiers.</param>
        /// <param name="markFilteringSet">The mark filtering set index.</param>
        /// <returns>The loaded <see cref="LookupType8Format1SubTable"/>.</returns>
        public static LookupType8Format1SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
        {
            ChainedSequenceRuleSetTable[] seqRuleSets =
                TableLoadingUtils.LoadChainedSequenceContextFormat1(reader, offset, out CoverageTable coverageTable);

            return new LookupType8Format1SubTable(coverageTable, seqRuleSets, lookupFlags, markFilteringSet);
        }

        /// <inheritdoc/>
        public override bool TryUpdatePosition(
            FontMetrics fontMetrics,
            GPosTable table,
            GlyphPositioningCollection collection,
            Tag feature,
            int index,
            int count)
        {
            // Implements Chained Contexts Substitution, Format 1:
            // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#61-chained-contexts-substitution-format-1-simple-glyph-contexts
            ushort glyphId = collection[index].GlyphId;
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
            SkippingGlyphIterator iterator = new(fontMetrics, collection, index, this.LookupFlags, this.MarkFilteringSet);
            for (int lookupIndex = 0; lookupIndex < rules.Length; lookupIndex++)
            {
                ChainedSequenceRuleTable rule = rules[lookupIndex];
                if (!AdvancedTypographicUtils.ApplyChainedSequenceRule(iterator, rule))
                {
                    continue;
                }

                bool hasChanged = false;
                for (int j = 0; j < rule.SequenceLookupRecords.Length; j++)
                {
                    SequenceLookupRecord sequenceLookupRecord = rule.SequenceLookupRecords[j];
                    LookupTable lookup = table.LookupList.LookupTables[sequenceLookupRecord.LookupListIndex];
                    ushort sequenceIndex = sequenceLookupRecord.SequenceIndex;
                    if (lookup.TryUpdatePosition(fontMetrics, table, collection, feature, index + sequenceIndex, 1))
                    {
                        hasChanged = true;
                    }
                }

                return hasChanged;
            }

            return false;
        }
    }

    /// <summary>
    /// Chained Context Positioning Format 2: class-based glyph contexts.
    /// </summary>
    internal sealed class LookupType8Format2SubTable : LookupSubTable
    {
        private readonly CoverageTable coverageTable;
        private readonly ClassDefinitionTable inputClassDefinitionTable;
        private readonly ClassDefinitionTable backtrackClassDefinitionTable;
        private readonly ClassDefinitionTable lookaheadClassDefinitionTable;
        private readonly ChainedClassSequenceRuleSetTable[] sequenceRuleSetTables;

        /// <summary>
        /// Initializes a new instance of the <see cref="LookupType8Format2SubTable"/> class.
        /// </summary>
        /// <param name="sequenceRuleSetTables">The array of chained class sequence rule set tables.</param>
        /// <param name="backtrackClassDefinitionTable">The backtrack class definition table.</param>
        /// <param name="inputClassDefinitionTable">The input class definition table.</param>
        /// <param name="lookaheadClassDefinitionTable">The lookahead class definition table.</param>
        /// <param name="coverageTable">The coverage table.</param>
        /// <param name="lookupFlags">The lookup qualifiers.</param>
        /// <param name="markFilteringSet">The mark filtering set index.</param>
        private LookupType8Format2SubTable(
            ChainedClassSequenceRuleSetTable[] sequenceRuleSetTables,
            ClassDefinitionTable backtrackClassDefinitionTable,
            ClassDefinitionTable inputClassDefinitionTable,
            ClassDefinitionTable lookaheadClassDefinitionTable,
            CoverageTable coverageTable,
            LookupFlags lookupFlags,
            ushort markFilteringSet)
            : base(lookupFlags, markFilteringSet)
        {
            this.sequenceRuleSetTables = sequenceRuleSetTables;
            this.backtrackClassDefinitionTable = backtrackClassDefinitionTable;
            this.inputClassDefinitionTable = inputClassDefinitionTable;
            this.lookaheadClassDefinitionTable = lookaheadClassDefinitionTable;
            this.coverageTable = coverageTable;
        }

        /// <summary>
        /// Loads the Format 2 chained context positioning subtable.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="offset">The offset to the beginning of the subtable.</param>
        /// <param name="lookupFlags">The lookup qualifiers.</param>
        /// <param name="markFilteringSet">The mark filtering set index.</param>
        /// <returns>The loaded <see cref="LookupType8Format2SubTable"/>.</returns>
        public static LookupType8Format2SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
        {
            ChainedClassSequenceRuleSetTable[] seqRuleSets = TableLoadingUtils.LoadChainedSequenceContextFormat2(
                reader,
                offset,
                out CoverageTable coverageTable,
                out ClassDefinitionTable backtrackClassDefTable,
                out ClassDefinitionTable inputClassDefTable,
                out ClassDefinitionTable lookaheadClassDefTable);

            return new LookupType8Format2SubTable(
                seqRuleSets,
                backtrackClassDefTable,
                inputClassDefTable,
                lookaheadClassDefTable,
                coverageTable,
                lookupFlags,
                markFilteringSet);
        }

        /// <inheritdoc/>
        public override bool TryUpdatePosition(
            FontMetrics fontMetrics,
            GPosTable table,
            GlyphPositioningCollection collection,
            Tag feature,
            int index,
            int count)
        {
            // Implements Chained Contexts Substitution for Format 2:
            // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#62-chained-contexts-substitution-format-2-class-based-glyph-contexts
            ushort glyphId = collection[index].GlyphId;
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
            SkippingGlyphIterator iterator = new(fontMetrics, collection, index, this.LookupFlags, this.MarkFilteringSet);
            for (int lookupIndex = 0; lookupIndex < rules.Length; lookupIndex++)
            {
                ChainedClassSequenceRuleTable rule = rules[lookupIndex];

                if (!AdvancedTypographicUtils.ApplyChainedClassSequenceRule(iterator, rule, this.inputClassDefinitionTable, this.backtrackClassDefinitionTable, this.lookaheadClassDefinitionTable))
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
                    if (lookup.TryUpdatePosition(fontMetrics, table, collection, feature, index + sequenceIndex, 1))
                    {
                        hasChanged = true;
                    }
                }

                return hasChanged;
            }

            return false;
        }
    }

    /// <summary>
    /// Chained Context Positioning Format 3: coverage-based glyph contexts.
    /// </summary>
    internal sealed class LookupType8Format3SubTable : LookupSubTable
    {
        private readonly SequenceLookupRecord[] seqLookupRecords;
        private readonly CoverageTable[] backtrackCoverageTables;
        private readonly CoverageTable[] inputCoverageTables;
        private readonly CoverageTable[] lookaheadCoverageTables;

        /// <summary>
        /// Initializes a new instance of the <see cref="LookupType8Format3SubTable"/> class.
        /// </summary>
        /// <param name="seqLookupRecords">The array of sequence lookup records.</param>
        /// <param name="backtrackCoverageTables">The array of backtrack coverage tables.</param>
        /// <param name="inputCoverageTables">The array of input coverage tables.</param>
        /// <param name="lookaheadCoverageTables">The array of lookahead coverage tables.</param>
        /// <param name="lookupFlags">The lookup qualifiers.</param>
        /// <param name="markFilteringSet">The mark filtering set index.</param>
        private LookupType8Format3SubTable(
            SequenceLookupRecord[] seqLookupRecords,
            CoverageTable[] backtrackCoverageTables,
            CoverageTable[] inputCoverageTables,
            CoverageTable[] lookaheadCoverageTables,
            LookupFlags lookupFlags,
            ushort markFilteringSet)
            : base(lookupFlags, markFilteringSet)
        {
            this.seqLookupRecords = seqLookupRecords;
            this.backtrackCoverageTables = backtrackCoverageTables;
            this.inputCoverageTables = inputCoverageTables;
            this.lookaheadCoverageTables = lookaheadCoverageTables;
        }

        /// <summary>
        /// Loads the Format 3 chained context positioning subtable.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="offset">The offset to the beginning of the subtable.</param>
        /// <param name="lookupFlags">The lookup qualifiers.</param>
        /// <param name="markFilteringSet">The mark filtering set index.</param>
        /// <returns>The loaded <see cref="LookupType8Format3SubTable"/>.</returns>
        public static LookupType8Format3SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
        {
            SequenceLookupRecord[] seqLookupRecords = TableLoadingUtils.LoadChainedSequenceContextFormat3(
                reader,
                offset,
                out CoverageTable[] backtrackCoverageTables,
                out CoverageTable[] inputCoverageTables,
                out CoverageTable[] lookaheadCoverageTables);

            return new LookupType8Format3SubTable(
                seqLookupRecords,
                backtrackCoverageTables,
                inputCoverageTables,
                lookaheadCoverageTables,
                lookupFlags,
                markFilteringSet);
        }

        /// <inheritdoc/>
        public override bool TryUpdatePosition(
            FontMetrics fontMetrics,
            GPosTable table,
            GlyphPositioningCollection collection,
            Tag feature,
            int index,
            int count)
        {
            ushort glyphId = collection[index].GlyphId;
            if (glyphId == 0)
            {
                return false;
            }

            if (!AdvancedTypographicUtils.CheckAllCoverages(fontMetrics, this.LookupFlags, this.MarkFilteringSet, collection, index, count, this.inputCoverageTables, this.backtrackCoverageTables, this.lookaheadCoverageTables))
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
                if (lookup.TryUpdatePosition(fontMetrics, table, collection, feature, index + sequenceIndex, count - sequenceIndex))
                {
                    hasChanged = true;
                }
            }

            return hasChanged;
        }
    }
}
