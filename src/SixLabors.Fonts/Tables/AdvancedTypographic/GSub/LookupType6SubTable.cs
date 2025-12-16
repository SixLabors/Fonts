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

internal sealed class LookupType6Format1SubTable : LookupSubTable
{
    private readonly CoverageTable coverageTable;
    private readonly ChainedSequenceRuleSetTable[] seqRuleSetTables;

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

    public static LookupType6Format1SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        ChainedSequenceRuleSetTable[] seqRuleSets = TableLoadingUtils.LoadChainedSequenceContextFormat1(reader, offset, out CoverageTable coverageTable);
        return new LookupType6Format1SubTable(coverageTable, seqRuleSets, lookupFlags, markFilteringSet);
    }

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
            //if (!AdvancedTypographicUtils.ApplyChainedClassSequenceRule(iterator, ruleTable, index + count, this.inputClassDefinitionTable, this.backtrackClassDefinitionTable, this.lookaheadClassDefinitionTable))
            //{
            //    continue;
            //}
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

internal sealed class LookupType6Format3SubTable : LookupSubTable
{
    private readonly SequenceLookupRecord[] sequenceLookupRecords;
    private readonly CoverageTable[] backtrackCoverageTables;
    private readonly CoverageTable[] inputCoverageTables;
    private readonly CoverageTable[] lookaheadCoverageTables;

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
