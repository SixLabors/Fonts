// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Tables.AdvancedTypographic.Gsub;

namespace SixLabors.Fonts.Tables.AdvancedTypographic
{
    internal static class AdvancedTypographicUtils
    {
        internal static SequenceRuleSetTable[] LoadSequenceContextFormat1(BigEndianBinaryReader reader, long offset, out CoverageTable coverageTable)
        {
            // https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#seqctxt1
            ushort coverageOffset = reader.ReadOffset16();
            ushort seqRuleSetCount = reader.ReadUInt16();
            ushort[] seqRuleSetOffsets = reader.ReadUInt16Array(seqRuleSetCount);
            var seqRuleSets = new SequenceRuleSetTable[seqRuleSetCount];

            for (int i = 0; i < seqRuleSets.Length; i++)
            {
                seqRuleSets[i] = SequenceRuleSetTable.Load(reader, offset + seqRuleSetOffsets[i]);
            }

            coverageTable = CoverageTable.Load(reader, offset + coverageOffset);
            return seqRuleSets;
        }

        internal static CoverageTable LoadSequenceContextFormat2(BigEndianBinaryReader reader, long offset, out ClassDefinitionTable classDefTable, out ClassSequenceRuleSetTable[] classSeqRuleSets)
        {
            // https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-2-class-based-glyph-contexts
            ushort coverageOffset = reader.ReadOffset16();
            ushort classDefOffset = reader.ReadOffset16();
            ushort classSeqRuleSetCount = reader.ReadUInt16();
            ushort[] classSeqRuleSetOffsets = reader.ReadUInt16Array(classSeqRuleSetCount);

            var coverageTable = CoverageTable.Load(reader, offset + coverageOffset);
            classDefTable = ClassDefinitionTable.Load(reader, offset + classDefOffset);

            classSeqRuleSets = new ClassSequenceRuleSetTable[classSeqRuleSetCount];
            for (int i = 0; i < classSeqRuleSets.Length; i++)
            {
                classSeqRuleSets[i] = ClassSequenceRuleSetTable.Load(reader, offset + classSeqRuleSetOffsets[i]);
            }

            return coverageTable;
        }

        internal static SequenceLookupRecord[] LoadSequenceContextFormat3(BigEndianBinaryReader reader, long offset, out CoverageTable[] coverageTables)
        {
            // https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-3-coverage-based-glyph-contexts
            ushort glyphCount = reader.ReadUInt16();
            ushort seqLookupCount = reader.ReadUInt16();
            ushort[] coverageOffsets = reader.ReadUInt16Array(glyphCount);
            SequenceLookupRecord[] seqLookupRecords = SequenceLookupRecord.LoadArray(reader, seqLookupCount);

            coverageTables = new CoverageTable[glyphCount];
            for (int i = 0; i < coverageTables.Length; i++)
            {
                coverageTables[i] = CoverageTable.Load(reader, offset + coverageOffsets[i]);
            }

            return seqLookupRecords;
        }
    }
}
