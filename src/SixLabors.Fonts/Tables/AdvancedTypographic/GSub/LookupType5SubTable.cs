// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GSub;

/// <summary>
/// A Contextual Substitution subtable describes glyph substitutions in context that replace one
/// or more glyphs within a certain pattern of glyphs.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-5-contextual-substitution-subtable"/>
/// </summary>
internal static class LookupType5SubTable
{
    public static LookupSubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        reader.Seek(offset, SeekOrigin.Begin);
        ushort subTableFormat = reader.ReadUInt16();

        return subTableFormat switch
        {
            1 => LookupType5Format1SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            2 => LookupType5Format2SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            3 => LookupType5Format3SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
            _ => new NotImplementedSubTable(),
        };
    }
}

internal sealed class LookupType5Format1SubTable : LookupSubTable
{
    private readonly CoverageTable coverageTable;
    private readonly SequenceRuleSetTable[] seqRuleSetTables;

    private LookupType5Format1SubTable(CoverageTable coverageTable, SequenceRuleSetTable[] seqRuleSetTables, LookupFlags lookupFlags, ushort markFilteringSet)
        : base(lookupFlags, markFilteringSet)
    {
        this.coverageTable = coverageTable;
        this.seqRuleSetTables = seqRuleSetTables;
    }

    public static LookupType5Format1SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        SequenceRuleSetTable[] seqRuleSets = TableLoadingUtils.LoadSequenceContextFormat1(reader, offset, out CoverageTable coverageTable);

        return new LookupType5Format1SubTable(coverageTable, seqRuleSets, lookupFlags, markFilteringSet);
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

            // It's a match. Perform substitutions and return true if anything changed.
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

internal sealed class LookupType5Format2SubTable : LookupSubTable
{
    private readonly CoverageTable coverageTable;
    private readonly ClassDefinitionTable classDefinitionTable;
    private readonly ClassSequenceRuleSetTable[] sequenceRuleSetTables;

    private LookupType5Format2SubTable(
        ClassSequenceRuleSetTable[] sequenceRuleSetTables,
        ClassDefinitionTable classDefinitionTable,
        CoverageTable coverageTable,
        LookupFlags lookupFlags,
        ushort markFilteringSet)
        : base(lookupFlags, markFilteringSet)
    {
        this.sequenceRuleSetTables = sequenceRuleSetTables;
        this.classDefinitionTable = classDefinitionTable;
        this.coverageTable = coverageTable;
    }

    public static LookupType5Format2SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        CoverageTable coverageTable = TableLoadingUtils.LoadSequenceContextFormat2(reader, offset, out ClassDefinitionTable classDefTable, out ClassSequenceRuleSetTable[] classSeqRuleSets);

        return new LookupType5Format2SubTable(classSeqRuleSets, classDefTable, coverageTable, lookupFlags, markFilteringSet);
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

        ClassSequenceRuleSetTable? ruleSetTable = this.sequenceRuleSetTables[offset];
        if (ruleSetTable is null)
        {
            return false;
        }

        SkippingGlyphIterator iterator = new(fontMetrics, collection, index, this.LookupFlags, this.MarkFilteringSet);
        foreach (ClassSequenceRuleTable ruleTable in ruleSetTable.SequenceRuleTables)
        {
            int remaining = count - 1;
            int seqLength = ruleTable.InputSequence.Length;
            if (seqLength > remaining)
            {
                continue;
            }

            //if (!AdvancedTypographicUtils.MatchClassSequence(iterator, index, ruleTable.InputSequence, index + count, this.classDefinitionTable))
            //{
            //    continue;
            //}
            if (!AdvancedTypographicUtils.MatchClassSequence(iterator, 1, ruleTable.InputSequence, this.classDefinitionTable))
            {
                continue;
            }

            // It's a match. Perform substitutions and return true if anything changed.
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

internal sealed class LookupType5Format3SubTable : LookupSubTable
{
    private readonly CoverageTable[] coverageTables;
    private readonly SequenceLookupRecord[] sequenceLookupRecords;

    private LookupType5Format3SubTable(
        CoverageTable[] coverageTables,
        SequenceLookupRecord[] sequenceLookupRecords,
        LookupFlags lookupFlags,
        ushort markFilteringSet)
        : base(lookupFlags, markFilteringSet)
    {
        this.coverageTables = coverageTables;
        this.sequenceLookupRecords = sequenceLookupRecords;
    }

    public static LookupType5Format3SubTable Load(BigEndianBinaryReader reader, long offset, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        SequenceLookupRecord[] seqLookupRecords = TableLoadingUtils.LoadSequenceContextFormat3(reader, offset, out CoverageTable[] coverageTables);

        return new LookupType5Format3SubTable(coverageTables, seqLookupRecords, lookupFlags, markFilteringSet);
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

        // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#53-context-substitution-format-3-coverage-based-glyph-contexts
        SkippingGlyphIterator iterator = new(fontMetrics, collection, index, this.LookupFlags, this.MarkFilteringSet);
        if (!AdvancedTypographicUtils.MatchCoverageSequence(iterator, this.coverageTables, index))
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
