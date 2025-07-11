// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

internal static class TableLoadingUtils
{
    internal static SequenceRuleSetTable[] LoadSequenceContextFormat1(BigEndianBinaryReader reader, long offset, out CoverageTable coverageTable)
    {
        // https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#seqctxt1
        // SequenceContextFormat1
        // +----------+------------------------------------+---------------------------------------------------------------+
        // | Type     | Name                               | Description                                                   |
        // +==========+====================================+===============================================================+
        // | uint16   | format                             | Format identifier: format = 1                                 |
        // +----------+------------------------------------+---------------------------------------------------------------+
        // | Offset16 | coverageOffset                     | Offset to Coverage table, from beginning of                   |
        // |          |                                    | SequenceContextFormat1 table.                                 |
        // +----------+------------------------------------+---------------------------------------------------------------+
        // | uint16   | seqRuleSetCount                    | Number of SequenceRuleSet tables.                             |
        // +----------+------------------------------------+---------------------------------------------------------------+
        // | Offset16 | seqRuleSetOffsets[seqRuleSetCount] | Array of offsets to SequenceRuleSet tables, from beginning of |
        // |          |                                    | SequenceContextFormat1 table (offsets may be NULL).           |
        // +----------+------------------------------------+---------------------------------------------------------------+
        ushort coverageOffset = reader.ReadOffset16();
        ushort seqRuleSetCount = reader.ReadUInt16();

        using Buffer<ushort> seqRuleSetOffsetsBuffer = new(seqRuleSetCount);
        Span<ushort> seqRuleSetOffsets = seqRuleSetOffsetsBuffer.GetSpan();
        reader.ReadUInt16Array(seqRuleSetOffsets);

        SequenceRuleSetTable[] seqRuleSets = new SequenceRuleSetTable[seqRuleSetCount];

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
        // Context Positioning Subtable Format 2: Class-based Glyph Contexts.
        // +----------+----------------------------------------------+--------------------------------------------------------------------+
        // | Type     | Name                                         | Description                                                        |
        // +==========+==============================================+====================================================================+
        // | uint16   | format                                       | Format identifier: format = 2                                      |
        // +----------+----------------------------------------------+--------------------------------------------------------------------+
        // | Offset16 | coverageOffset                               | Offset to Coverage table, from beginning of                        |
        // |          |                                              | SequenceContextFormat2 table.                                      |
        // +----------+----------------------------------------------+--------------------------------------------------------------------+
        // | Offset16 | classDefOffset                               | Offset to ClassDef table, from beginning of                        |
        // |          |                                              | SequenceContextFormat2 table.                                      |
        // +----------+----------------------------------------------+--------------------------------------------------------------------+
        // | uint16   | classSeqRuleSetCount                         | Number of ClassSequenceRuleSet tables.                             |
        // +----------+----------------------------------------------+--------------------------------------------------------------------+
        // | Offset16 | classSeqRuleSetOffsets[classSeqRuleSetCount] | Array of offsets to ClassSequenceRuleSet tables, from beginning of |
        // |          |                                              | SequenceContextFormat2 table (may be NULL).                        |
        // +----------+----------------------------------------------+--------------------------------------------------------------------+
        ushort coverageOffset = reader.ReadOffset16();
        ushort classDefOffset = reader.ReadOffset16();
        ushort classSeqRuleSetCount = reader.ReadUInt16();

        using Buffer<ushort> classSeqRuleSetOffsetsBuffer = new(classSeqRuleSetCount);
        Span<ushort> classSeqRuleSetOffsets = classSeqRuleSetOffsetsBuffer.GetSpan();
        reader.ReadUInt16Array(classSeqRuleSetOffsets);

        CoverageTable coverageTable = CoverageTable.Load(reader, offset + coverageOffset);
        classDefTable = ClassDefinitionTable.Load(reader, offset + classDefOffset);

        classSeqRuleSets = new ClassSequenceRuleSetTable[classSeqRuleSetCount];
        for (int i = 0; i < classSeqRuleSets.Length; i++)
        {
            ushort ruleSetOffset = classSeqRuleSetOffsets[i];
            if (ruleSetOffset > 0)
            {
                classSeqRuleSets[i] = ClassSequenceRuleSetTable.Load(reader, offset + classSeqRuleSetOffsets[i]);
            }
        }

        return coverageTable;
    }

    internal static SequenceLookupRecord[] LoadSequenceContextFormat3(BigEndianBinaryReader reader, long offset, out CoverageTable[] coverageTables)
    {
        // https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-3-coverage-based-glyph-contexts
        // SequenceContextFormat3
        // +----------------------+----------------------------------+-------------------------------------------+
        // | Type                 | Name                             | Description                               |
        // +======================+==================================+===========================================+
        // | uint16               | format                           | Format identifier: format = 3             |
        // +----------------------+----------------------------------+-------------------------------------------+
        // | uint16               | glyphCount                       | Number of glyphs in the input sequence    |
        // +----------------------+----------------------------------+-------------------------------------------+
        // | uint16               | seqLookupCount                   | Number of SequenceLookupRecords           |
        // +----------------------+----------------------------------+-------------------------------------------+
        // | Offset16             | coverageOffsets[glyphCount]      | Array of offsets to Coverage tables, from |
        // |                      |                                  | beginning of SequenceContextFormat3       |
        // |                      |                                  | subtable                                  |
        // +----------------------+----------------------------------+-------------------------------------------+
        // | SequenceLookupRecord | seqLookupRecords[seqLookupCount] | Array of SequenceLookupRecords            |
        // +----------------------+----------------------------------+-------------------------------------------+
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

    internal static ChainedSequenceRuleSetTable[] LoadChainedSequenceContextFormat1(BigEndianBinaryReader reader, long offset, out CoverageTable coverageTable)
    {
        // https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-1-simple-glyph-contexts
        // ChainedSequenceContextFormat1
        // +----------+--------------------------------------------------+------------------------------------------+
        // | Type     | Name                                             | Description                              |
        // +==========+==================================================+==========================================+
        // | uint16   | format                                           | Format identifier: format = 1            |
        // +----------+--------------------------------------------------+------------------------------------------+
        // | Offset16 | coverageOffset                                   | Offset to Coverage table, from beginning |
        // |          |                                                  | of ChainSequenceContextFormat1 table     |
        // +----------+--------------------------------------------------+------------------------------------------+
        // | uint16   | chainedSeqRuleSetCount                           | Number of ChainedSequenceRuleSet tables  |
        // +----------+--------------------------------------------------+------------------------------------------+
        // | Offset16 | chainedSeqRuleSetOffsets[chainedSeqRuleSetCount] | Array of offsets to ChainedSeqRuleSet    |
        // |          |                                                  | tables, from beginning of                |
        // |          |                                                  | ChainedSequenceContextFormat1 table      |
        // |          |                                                  | (may be NULL)                            |
        // +----------+--------------------------------------------------+------------------------------------------+
        ushort coverageOffset = reader.ReadOffset16();
        ushort chainedSeqRuleSetCount = reader.ReadUInt16();

        using Buffer<ushort> chainedSeqRuleSetOffsetsBuffer = new(chainedSeqRuleSetCount);
        Span<ushort> chainedSeqRuleSetOffsets = chainedSeqRuleSetOffsetsBuffer.GetSpan();
        reader.ReadUInt16Array(chainedSeqRuleSetOffsets);

        ChainedSequenceRuleSetTable[] seqRuleSets = new ChainedSequenceRuleSetTable[chainedSeqRuleSetCount];

        for (int i = 0; i < seqRuleSets.Length; i++)
        {
            if (chainedSeqRuleSetOffsets[i] > 0)
            {
                seqRuleSets[i] = ChainedSequenceRuleSetTable.Load(reader, offset + chainedSeqRuleSetOffsets[i]);
            }
        }

        coverageTable = CoverageTable.Load(reader, offset + coverageOffset);
        return seqRuleSets;
    }

    internal static ChainedClassSequenceRuleSetTable[] LoadChainedSequenceContextFormat2(
        BigEndianBinaryReader reader,
        long offset,
        out CoverageTable coverageTable,
        out ClassDefinitionTable backtrackClassDefTable,
        out ClassDefinitionTable inputClassDefTable,
        out ClassDefinitionTable lookaheadClassDefTable)
    {
        // https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-2-class-based-glyph-contexts
        // ChainedSequenceContextFormat2
        // +----------+------------------------------------------------------------+---------------------------------------------------------------------+
        // | Type     | Name                                                       | Description                                                         |
        // +==========+============================================================+=====================================================================+
        // | uint16   | format                                                     | Format identifier: format = 2                                       |
        // +----------+------------------------------------------------------------+---------------------------------------------------------------------+
        // | Offset16 | coverageOffset                                             | Offset to Coverage table, from beginning                            |
        // |          |                                                            | of ChainedSequenceContextFormat2 table                              |
        // +----------+------------------------------------------------------------+---------------------------------------------------------------------+
        // | Offset16 | backtrackClassDefOffset                                    | Offset to ClassDef table containing                                 |
        // |          |                                                            | backtrack sequence context, from                                    |
        // |          |                                                            | beginning of ChainedSequenceContextFormat2 table                    |
        // +----------+------------------------------------------------------------+---------------------------------------------------------------------+
        // | Offset16 | inputClassDefOffset                                        | Offset to ClassDef table containing input                           |
        // |          |                                                            | sequence context, from beginning of                                 |
        // |          |                                                            | ChainedSequenceContextFormat2 table                                 |
        // +----------+------------------------------------------------------------+---------------------------------------------------------------------+
        // | Offset16 | lookaheadClassDefOffset                                    | Offset to ClassDef table containing                                 |
        // |          |                                                            | lookahead sequence context, from                                    |
        // |          |                                                            | beginning of ChainedSequenceContextFormat2 table                    |
        // +----------+------------------------------------------------------------+---------------------------------------------------------------------+
        // | uint16   | chainedClassSeqRuleSetCount                                | Number of ChainedClassSequenceRuleSet tables                        |
        // +----------+------------------------------------------------------------+---------------------------------------------------------------------+
        // | Offset16 | chainedClassSeqRuleSetOffsets[chainedClassSeqRuleSetCount] | Array of offsets to ChainedClassSequenceRuleSet tables,             |
        // |          |                                                            | from beginning of ChainedSequenceContextFormat2 table (may be NULL) |
        // +----------+------------------------------------------------------------+---------------------------------------------------------------------+
        ushort coverageOffset = reader.ReadOffset16();
        ushort backtrackClassDefOffset = reader.ReadOffset16();
        ushort inputClassDefOffset = reader.ReadOffset16();
        ushort lookaheadClassDefOffset = reader.ReadOffset16();
        ushort chainedClassSeqRuleSetCount = reader.ReadUInt16();
        ChainedClassSequenceRuleSetTable[] seqRuleSets = Array.Empty<ChainedClassSequenceRuleSetTable>();
        if (chainedClassSeqRuleSetCount != 0)
        {
            ushort[] chainedClassSeqRuleSetOffsets = new ushort[chainedClassSeqRuleSetCount];
            for (int i = 0; i < chainedClassSeqRuleSetCount; i++)
            {
                chainedClassSeqRuleSetOffsets[i] = reader.ReadOffset16();
            }

            seqRuleSets = new ChainedClassSequenceRuleSetTable[chainedClassSeqRuleSetCount];
            for (int i = 0; i < seqRuleSets.Length; i++)
            {
                if (chainedClassSeqRuleSetOffsets[i] > 0)
                {
                    seqRuleSets[i] =
                        ChainedClassSequenceRuleSetTable.Load(reader, offset + chainedClassSeqRuleSetOffsets[i]);
                }
            }
        }

        coverageTable = CoverageTable.Load(reader, offset + coverageOffset);
        backtrackClassDefTable = ClassDefinitionTable.Load(reader, offset + backtrackClassDefOffset);
        inputClassDefTable = ClassDefinitionTable.Load(reader, offset + inputClassDefOffset);
        lookaheadClassDefTable = ClassDefinitionTable.Load(reader, offset + lookaheadClassDefOffset);
        return seqRuleSets;
    }

    internal static SequenceLookupRecord[] LoadChainedSequenceContextFormat3(
        BigEndianBinaryReader reader,
        long offset,
        out CoverageTable[] backtrackCoverageTables,
        out CoverageTable[] inputCoverageTables,
        out CoverageTable[] lookaheadCoverageTables)
    {
        // https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#chseqctxt3
        // ChainedSequenceContextFormat3 1
        // +----------------------+-----------------------------------------------+----------------------------------------------------------------+
        // | Type                 | Name                                          | Description                                                    |
        // +======================+===============================================+================================================================+
        // | uint16               | format                                        | Format identifier: format = 3                                  |
        // +----------------------+-----------------------------------------------+----------------------------------------------------------------+
        // | uint16               | backtrackGlyphCount                           | Number of glyphs in the backtrack sequence                     |
        // +----------------------+-----------------------------------------------+----------------------------------------------------------------+
        // | Offset16             | backtrackCoverageOffsets[backtrackGlyphCount] | Array of offsets to coverage tables for the backtrack sequence |
        // +----------------------+-----------------------------------------------+----------------------------------------------------------------+
        // | uint16               | inputGlyphCount                               | Number of glyphs in the input sequence                         |
        // +----------------------+-----------------------------------------------+----------------------------------------------------------------+
        // | Offset16             | inputCoverageOffsets[inputGlyphCount]         | Array of offsets to coverage tables for the input sequence     |
        // +----------------------+-----------------------------------------------+----------------------------------------------------------------+
        // | uint16               | lookaheadGlyphCount                           | Number of glyphs in the lookahead sequence                     |
        // +----------------------+-----------------------------------------------+----------------------------------------------------------------+
        // | Offset16             | lookaheadCoverageOffsets[lookaheadGlyphCount] | Array of offsets to coverage tables for the lookahead sequence |
        // +----------------------+-----------------------------------------------+----------------------------------------------------------------+
        // | uint16               | seqLookupCount                                | Number of SequenceLookupRecords                                |
        // +----------------------+-----------------------------------------------+----------------------------------------------------------------+
        // | SequenceLookupRecord | seqLookupRecords[seqLookupCount]              | Array of SequenceLookupRecords                                 |
        // +----------------------+-----------------------------------------------+----------------------------------------------------------------+
        ushort backtrackGlyphCount = reader.ReadUInt16();
        ushort[] backtrackCoverageOffsets = reader.ReadUInt16Array(backtrackGlyphCount);

        ushort inputGlyphCount = reader.ReadUInt16();
        ushort[] inputCoverageOffsets = reader.ReadUInt16Array(inputGlyphCount);

        ushort lookaheadGlyphCount = reader.ReadUInt16();
        ushort[] lookaheadCoverageOffsets = reader.ReadUInt16Array(lookaheadGlyphCount);

        ushort seqLookupCount = reader.ReadUInt16();
        SequenceLookupRecord[] seqLookupRecords = SequenceLookupRecord.LoadArray(reader, seqLookupCount);

        backtrackCoverageTables = CoverageTable.LoadArray(reader, offset, backtrackCoverageOffsets);
        inputCoverageTables = CoverageTable.LoadArray(reader, offset, inputCoverageOffsets);
        lookaheadCoverageTables = CoverageTable.LoadArray(reader, offset, lookaheadCoverageOffsets);
        return seqLookupRecords;
    }
}
