// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GSub;

/// <summary>
/// A Chained Contexts Substitution subtable describes glyph substitutions in context
/// with an ability to look back and/or look ahead in the sequence of glyphs.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-6-chained-contexts-substitution-subtable"/>
/// </summary>
internal static class LookupType6SubTable
{
    /// <summary>
    /// Loads the chaining context substitution lookup subtable from the given offset.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the substitution subtable.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    /// <returns>The loaded <see cref="LookupSubTable"/>.</returns>
    public static LookupSubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        reader.Seek(offset, SeekOrigin.Begin);
        ushort substFormat = reader.ReadUInt16();

        return substFormat switch
        {
            1 => LookupType6Format1SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            2 => LookupType6Format2SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            3 => LookupType6Format3SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            _ => new NotImplementedSubTable(),
        };
    }
}

/// <summary>
/// Implements chaining context substitution format 1 (simple glyph contexts).
/// Rules include backtrack, input, and lookahead glyph sequences for matching.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#61-chained-contexts-substitution-format-1-simple-glyph-contexts"/>
/// </summary>
internal sealed class LookupType6Format1SubTable : LookupSubTable
{
    /// <summary>
    /// The coverage table that defines the set of first input glyph IDs.
    /// </summary>
    private readonly CoverageTable coverageTable;

    /// <summary>
    /// The array of chained sequence rule set tables, ordered by coverage index.
    /// </summary>
    private readonly ChainedSequenceRuleSetTable[] seqRuleSetTables;

    /// <summary>
    /// Initializes a new instance of the <see cref="LookupType6Format1SubTable"/> class.
    /// </summary>
    /// <param name="coverageTable">The coverage table defining first input glyphs.</param>
    /// <param name="seqRuleSetTables">The array of chained sequence rule set tables.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    private LookupType6Format1SubTable(
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
    /// Loads the chaining context substitution format 1 subtable from the given offset.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the substitution subtable.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    /// <returns>The loaded <see cref="LookupType6Format1SubTable"/>.</returns>
    public static LookupType6Format1SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        ChainedSequenceRuleSetTable[] seqRuleSets = TableLoadingUtils.LoadChainedSequenceContextFormat1(reader, offset, out CoverageTable coverageTable);
        return new LookupType6Format1SubTable(coverageTable, seqRuleSets, lookupFlags, markFilteringSet);
    }

    /// <inheritdoc />
    public override bool TrySubstitution(
        FontMetrics fontMetrics,
        GSubTable table,
        GlyphSubstitutionCollection collection,
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

        // Apply ruleset for the given glyph id.
        SkippingGlyphIterator iterator = new(fontMetrics, collection, index, this.LookupFlags, this.MarkFilteringSet);
        ChainedSequenceRuleSetTable seqRuleSet = this.seqRuleSetTables[offset];
        ChainedSequenceRuleTable[] rules = seqRuleSet.SequenceRuleTables;
        for (int i = 0; i < rules.Length; i++)
        {
            ChainedSequenceRuleTable ruleTable = rules[i];
            if (!AdvancedTypographicUtils.ApplyChainedSequenceRule(iterator, ruleTable))
            {
                continue;
            }

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
/// Implements chaining context substitution format 2 (class-based glyph contexts).
/// Rules use class definitions for backtrack, input, and lookahead sequences.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#62-chained-contexts-substitution-format-2-class-based-glyph-contexts"/>
/// </summary>
internal sealed class LookupType6Format2SubTable : LookupSubTable
{
    /// <summary>
    /// The coverage table that defines the set of first input glyph IDs.
    /// </summary>
    private readonly CoverageTable coverageTable;

    /// <summary>
    /// The class definition table used to classify input glyphs.
    /// </summary>
    private readonly ClassDefinitionTable inputClassDefinitionTable;

    /// <summary>
    /// The class definition table used to classify backtrack glyphs.
    /// </summary>
    private readonly ClassDefinitionTable backtrackClassDefinitionTable;

    /// <summary>
    /// The class definition table used to classify lookahead glyphs.
    /// </summary>
    private readonly ClassDefinitionTable lookaheadClassDefinitionTable;

    /// <summary>
    /// The array of chained class sequence rule set tables, indexed by input class value.
    /// </summary>
    private readonly ChainedClassSequenceRuleSetTable[] sequenceRuleSetTables;

    /// <summary>
    /// Initializes a new instance of the <see cref="LookupType6Format2SubTable"/> class.
    /// </summary>
    /// <param name="sequenceRuleSetTables">The array of chained class sequence rule set tables.</param>
    /// <param name="backtrackClassDefinitionTable">The class definition table for backtrack glyphs.</param>
    /// <param name="inputClassDefinitionTable">The class definition table for input glyphs.</param>
    /// <param name="lookaheadClassDefinitionTable">The class definition table for lookahead glyphs.</param>
    /// <param name="coverageTable">The coverage table defining first input glyphs.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    private LookupType6Format2SubTable(
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
    /// Loads the chaining context substitution format 2 subtable from the given offset.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the substitution subtable.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    /// <returns>The loaded <see cref="LookupType6Format2SubTable"/>.</returns>
    public static LookupType6Format2SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        ChainedClassSequenceRuleSetTable[] seqRuleSets = TableLoadingUtils.LoadChainedSequenceContextFormat2(
            reader,
            offset,
            out CoverageTable coverageTable,
            out ClassDefinitionTable backtrackClassDefTable,
            out ClassDefinitionTable inputClassDefTable,
            out ClassDefinitionTable lookaheadClassDefTable);

        return new LookupType6Format2SubTable(
            seqRuleSets,
            backtrackClassDefTable,
            inputClassDefTable,
            lookaheadClassDefTable,
            coverageTable,
            lookupFlags,
            markFilteringSet);
    }

    /// <inheritdoc />
    public override bool TrySubstitution(
        FontMetrics fontMetrics,
        GSubTable table,
        GlyphSubstitutionCollection collection,
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
        ChainedClassSequenceRuleTable[]? rules = classId >= 0 && classId < this.sequenceRuleSetTables.Length ? this.sequenceRuleSetTables[classId]?.SubRules : null;
        if (rules is null)
        {
            return false;
        }

        // Apply ruleset for the given glyph class id.
        SkippingGlyphIterator iterator = new(fontMetrics, collection, index, this.LookupFlags, this.MarkFilteringSet);
        for (int lookupIndex = 0; lookupIndex < rules.Length; lookupIndex++)
        {
            ChainedClassSequenceRuleTable ruleTable = rules[lookupIndex];

            if (!AdvancedTypographicUtils.ApplyChainedClassSequenceRule(iterator, ruleTable, this.inputClassDefinitionTable, this.backtrackClassDefinitionTable, this.lookaheadClassDefinitionTable))
            {
                continue;
            }

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
/// Implements chaining context substitution format 3 (coverage-based glyph contexts).
/// Rules use separate coverage tables for backtrack, input, and lookahead sequences.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#63-chained-contexts-substitution-format-3-coverage-based-glyph-contexts"/>
/// </summary>
internal sealed class LookupType6Format3SubTable : LookupSubTable
{
    /// <summary>
    /// The array of sequence lookup records that define the substitutions to apply.
    /// </summary>
    private readonly SequenceLookupRecord[] sequenceLookupRecords;

    /// <summary>
    /// The array of coverage tables for the backtrack sequence.
    /// </summary>
    private readonly CoverageTable[] backtrackCoverageTables;

    /// <summary>
    /// The array of coverage tables for the input sequence.
    /// </summary>
    private readonly CoverageTable[] inputCoverageTables;

    /// <summary>
    /// The array of coverage tables for the lookahead sequence.
    /// </summary>
    private readonly CoverageTable[] lookaheadCoverageTables;

    /// <summary>
    /// Initializes a new instance of the <see cref="LookupType6Format3SubTable"/> class.
    /// </summary>
    /// <param name="seqLookupRecords">The array of sequence lookup records.</param>
    /// <param name="backtrackCoverageTables">The coverage tables for the backtrack sequence.</param>
    /// <param name="inputCoverageTables">The coverage tables for the input sequence.</param>
    /// <param name="lookaheadCoverageTables">The coverage tables for the lookahead sequence.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    private LookupType6Format3SubTable(
        SequenceLookupRecord[] seqLookupRecords,
        CoverageTable[] backtrackCoverageTables,
        CoverageTable[] inputCoverageTables,
        CoverageTable[] lookaheadCoverageTables,
        LookupFlags lookupFlags,
        ushort markFilteringSet)
        : base(lookupFlags, markFilteringSet)
    {
        this.sequenceLookupRecords = seqLookupRecords;
        this.backtrackCoverageTables = backtrackCoverageTables;
        this.inputCoverageTables = inputCoverageTables;
        this.lookaheadCoverageTables = lookaheadCoverageTables;
    }

    /// <summary>
    /// Loads the chaining context substitution format 3 subtable from the given offset.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the substitution subtable.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    /// <returns>The loaded <see cref="LookupType6Format3SubTable"/>.</returns>
    public static LookupType6Format3SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        SequenceLookupRecord[] seqLookupRecords = TableLoadingUtils.LoadChainedSequenceContextFormat3(
            reader,
            offset,
            out CoverageTable[] backtrackCoverageTables,
            out CoverageTable[] inputCoverageTables,
            out CoverageTable[] lookaheadCoverageTables);

        return new LookupType6Format3SubTable(
            seqLookupRecords,
            backtrackCoverageTables,
            inputCoverageTables,
            lookaheadCoverageTables,
            lookupFlags,
            markFilteringSet);
    }

    /// <inheritdoc />
    public override bool TrySubstitution(
        FontMetrics fontMetrics,
        GSubTable table,
        GlyphSubstitutionCollection collection,
        Tag feature,
        int index,
        int count)
    {
        ushort glyphId = collection[index].GlyphId;
        if (glyphId == 0)
        {
            return false;
        }

        if (!AdvancedTypographicUtils.CheckAllCoverages(
            fontMetrics,
            this.LookupFlags,
            this.MarkFilteringSet,
            collection,
            index,
            count,
            this.inputCoverageTables,
            this.backtrackCoverageTables,
            this.lookaheadCoverageTables))
        {
            return false;
        }

        // It's a match. Perform substitutions and return true if anything changed.
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
