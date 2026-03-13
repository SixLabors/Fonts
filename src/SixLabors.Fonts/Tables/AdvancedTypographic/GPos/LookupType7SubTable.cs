// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos;

/// <summary>
/// Lookup Type 7: Contextual Positioning Subtables.
/// A Contextual Positioning subtable describes glyph positioning in context so a text-processing client can adjust the position
/// of one or more glyphs within a certain pattern of glyphs.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-7-contextual-positioning-subtables"/>
/// </summary>
internal static class LookupType7SubTable
{
    /// <summary>
    /// Loads the contextual positioning subtable from the specified reader.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the subtable.</param>
    /// <param name="lookupFlags">The lookup qualifiers.</param>
    /// <param name="markFilteringSet">The mark filtering set index.</param>
    /// <returns>The loaded <see cref="LookupSubTable"/>.</returns>
    public static LookupSubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        reader.Seek(offset, SeekOrigin.Begin);
        ushort subTableFormat = reader.ReadUInt16();

        return subTableFormat switch
        {
            1 => LookupType7Format1SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            2 => LookupType7Format2SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            3 => LookupType7Format3SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            _ => new NotImplementedSubTable(),
        };
    }

    /// <summary>
    /// Context Positioning Format 1: simple glyph contexts using individual glyph indices.
    /// </summary>
    internal sealed class LookupType7Format1SubTable : LookupSubTable
    {
        private readonly CoverageTable coverageTable;
        private readonly SequenceRuleSetTable[] seqRuleSetTables;

        /// <summary>
        /// Initializes a new instance of the <see cref="LookupType7Format1SubTable"/> class.
        /// </summary>
        /// <param name="coverageTable">The coverage table.</param>
        /// <param name="seqRuleSetTables">The array of sequence rule set tables.</param>
        /// <param name="lookupFlags">The lookup qualifiers.</param>
        /// <param name="markFilteringSet">The mark filtering set index.</param>
        public LookupType7Format1SubTable(
            CoverageTable coverageTable,
            SequenceRuleSetTable[] seqRuleSetTables,
            LookupFlags lookupFlags,
            ushort markFilteringSet)
            : base(lookupFlags, markFilteringSet)
        {
            this.seqRuleSetTables = seqRuleSetTables;
            this.coverageTable = coverageTable;
        }

        /// <summary>
        /// Loads the Format 1 contextual positioning subtable.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="offset">The offset to the beginning of the subtable.</param>
        /// <param name="lookupFlags">The lookup qualifiers.</param>
        /// <param name="markFilteringSet">The mark filtering set index.</param>
        /// <returns>The loaded <see cref="LookupType7Format1SubTable"/>.</returns>
        public static LookupType7Format1SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
        {
            SequenceRuleSetTable[] seqRuleSets = TableLoadingUtils.LoadSequenceContextFormat1(reader, offset, out CoverageTable coverageTable);

            return new LookupType7Format1SubTable(coverageTable, seqRuleSets, lookupFlags, markFilteringSet);
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

            int offset = this.coverageTable.CoverageIndexOf(glyphId);
            if (offset <= -1)
            {
                return false;
            }

            // TODO: Check this.
            // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#example-7-contextual-substitution-format-1
            SequenceRuleSetTable ruleSetTable = this.seqRuleSetTables[offset];
            SkippingGlyphIterator iterator = new(fontMetrics, collection, index, this.LookupFlags, this.MarkFilteringSet);
            foreach (SequenceRuleTable ruleTable in ruleSetTable.SequenceRuleTables)
            {
                int remaining = count - 1;
                int seqLength = ruleTable.InputSequence.Length;
                if (seqLength > remaining)
                {
                    continue;
                }

                if (!AdvancedTypographicUtils.MatchSequence(iterator, 1, ruleTable.InputSequence))
                {
                    continue;
                }

                // It's a match. Perform position update and return true if anything changed.
                return AdvancedTypographicUtils.ApplyLookupList(
                    fontMetrics,
                    table,
                    feature,
                    this.LookupFlags,
                    this.MarkFilteringSet,
                    ruleTable.SequenceLookupRecords,
                    collection,
                    index,
                    count);
            }

            return false;
        }
    }

    /// <summary>
    /// Context Positioning Format 2: class-based glyph contexts.
    /// </summary>
    internal sealed class LookupType7Format2SubTable : LookupSubTable
    {
        private readonly CoverageTable coverageTable;
        private readonly ClassDefinitionTable classDefinitionTable;
        private readonly ClassSequenceRuleSetTable[] sequenceRuleSetTables;

        /// <summary>
        /// Initializes a new instance of the <see cref="LookupType7Format2SubTable"/> class.
        /// </summary>
        /// <param name="coverageTable">The coverage table.</param>
        /// <param name="classDefinitionTable">The class definition table.</param>
        /// <param name="sequenceRuleSetTables">The array of class sequence rule set tables.</param>
        /// <param name="lookupFlags">The lookup qualifiers.</param>
        /// <param name="markFilteringSet">The mark filtering set index.</param>
        public LookupType7Format2SubTable(
            CoverageTable coverageTable,
            ClassDefinitionTable classDefinitionTable,
            ClassSequenceRuleSetTable[] sequenceRuleSetTables,
            LookupFlags lookupFlags,
            ushort markFilteringSet)
            : base(lookupFlags, markFilteringSet)
        {
            this.coverageTable = coverageTable;
            this.classDefinitionTable = classDefinitionTable;
            this.sequenceRuleSetTables = sequenceRuleSetTables;
        }

        /// <summary>
        /// Loads the Format 2 contextual positioning subtable.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="offset">The offset to the beginning of the subtable.</param>
        /// <param name="lookupFlags">The lookup qualifiers.</param>
        /// <param name="markFilteringSet">The mark filtering set index.</param>
        /// <returns>The loaded <see cref="LookupType7Format2SubTable"/>.</returns>
        public static LookupType7Format2SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
        {
            CoverageTable coverageTable =
                TableLoadingUtils.LoadSequenceContextFormat2(
                    reader,
                    offset,
                    out ClassDefinitionTable classDefTable,
                    out ClassSequenceRuleSetTable[] classSeqRuleSets);

            return new LookupType7Format2SubTable(coverageTable, classDefTable, classSeqRuleSets, lookupFlags, markFilteringSet);
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

            if (this.coverageTable.CoverageIndexOf(glyphId) < 0)
            {
                return false;
            }

            int offset = this.classDefinitionTable.ClassIndexOf(glyphId);
            if (offset < 0)
            {
                return false;
            }

            ClassSequenceRuleSetTable ruleSetTable = this.sequenceRuleSetTables[offset];
            SkippingGlyphIterator iterator = new(fontMetrics, collection, index, this.LookupFlags, this.MarkFilteringSet);
            foreach (ClassSequenceRuleTable ruleTable in ruleSetTable.SequenceRuleTables)
            {
                int remaining = count - 1;
                int seqLength = ruleTable.InputSequence.Length;
                if (seqLength > remaining)
                {
                    continue;
                }

                if (!AdvancedTypographicUtils.MatchClassSequence(iterator, 1, ruleTable.InputSequence, this.classDefinitionTable))
                {
                    continue;
                }

                // It's a match. Perform position update and return true if anything changed.
                return AdvancedTypographicUtils.ApplyLookupList(
                    fontMetrics,
                    table,
                    feature,
                    this.LookupFlags,
                    this.MarkFilteringSet,
                    ruleTable.SequenceLookupRecords,
                    collection,
                    index,
                    count);
            }

            return false;
        }
    }

    /// <summary>
    /// Context Positioning Format 3: coverage-based glyph contexts.
    /// </summary>
    internal sealed class LookupType7Format3SubTable : LookupSubTable
    {
        private readonly CoverageTable[] coverageTables;
        private readonly SequenceLookupRecord[] sequenceLookupRecords;

        /// <summary>
        /// Initializes a new instance of the <see cref="LookupType7Format3SubTable"/> class.
        /// </summary>
        /// <param name="coverageTables">The array of coverage tables, one per glyph in the input sequence.</param>
        /// <param name="sequenceLookupRecords">The array of sequence lookup records.</param>
        /// <param name="lookupFlags">The lookup qualifiers.</param>
        /// <param name="markFilteringSet">The mark filtering set index.</param>
        public LookupType7Format3SubTable(
            CoverageTable[] coverageTables,
            SequenceLookupRecord[] sequenceLookupRecords,
            LookupFlags lookupFlags,
            ushort markFilteringSet)
            : base(lookupFlags, markFilteringSet)
        {
            this.coverageTables = coverageTables;
            this.sequenceLookupRecords = sequenceLookupRecords;
        }

        /// <summary>
        /// Loads the Format 3 contextual positioning subtable.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="offset">The offset to the beginning of the subtable.</param>
        /// <param name="lookupFlags">The lookup qualifiers.</param>
        /// <param name="markFilteringSet">The mark filtering set index.</param>
        /// <returns>The loaded <see cref="LookupType7Format3SubTable"/>.</returns>
        public static LookupType7Format3SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
        {
            SequenceLookupRecord[] seqLookupRecords =
                TableLoadingUtils.LoadSequenceContextFormat3(reader, offset, out CoverageTable[] coverageTables);

            return new LookupType7Format3SubTable(coverageTables, seqLookupRecords, lookupFlags, markFilteringSet);
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

            SkippingGlyphIterator iterator = new(fontMetrics, collection, index, this.LookupFlags, this.MarkFilteringSet);
            if (!AdvancedTypographicUtils.MatchCoverageSequence(iterator, this.coverageTables, index, index + count))
            {
                return false;
            }

            return AdvancedTypographicUtils.ApplyLookupList(
                fontMetrics,
                table,
                feature,
                this.LookupFlags,
                this.MarkFilteringSet,
                this.sequenceLookupRecords,
                collection,
                index,
                count);
        }
    }
}
