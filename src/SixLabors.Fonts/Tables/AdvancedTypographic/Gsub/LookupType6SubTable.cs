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
    internal class LookupType6SubTable
    {
        private LookupType6SubTable()
        {
        }

        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort substFormat = reader.ReadUInt16();

            return substFormat switch
            {
                // TODO: Implement 2
                1 => LookupType6Format1SubTable.Load(reader, offset),
                3 => LookupType6Format3SubTable.Load(reader, offset),
                _ => new NotImplementedSubTable()
            };
        }
    }

    internal class LookupType6Format1SubTable : LookupSubTable
    {
        private readonly ChainedSequenceRuleSetTable[] seqRuleSetTables;
        private readonly CoverageTable coverageTable;

        private LookupType6Format1SubTable(ChainedSequenceRuleSetTable[] seqbRuleSetTables, CoverageTable coverageTable)
        {
            this.seqRuleSetTables = seqbRuleSetTables;
            this.coverageTable = coverageTable;
        }

        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset)
        {
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
            ushort[] chainedSeqRuleSetOffsets = reader.ReadUInt16Array(chainedSeqRuleSetCount);

            var seqRuleSets = new ChainedSequenceRuleSetTable[chainedSeqRuleSetCount];

            for (int i = 0; i < seqRuleSets.Length; i++)
            {
                seqRuleSets[i] = ChainedSequenceRuleSetTable.Load(reader, offset + chainedSeqRuleSetOffsets[i]);
            }

            var coverageTable = CoverageTable.Load(reader, offset + coverageOffset);
            return new LookupType6Format1SubTable(seqRuleSets, coverageTable);
        }

        public override bool TrySubstition(GSubTable table, GlyphSubstitutionCollection collection, ushort index, int count)
        {
            int glyphId = collection[index][0];
            if (glyphId < 0)
            {
                return false;
            }

            int offset = this.coverageTable.CoverageIndexOf((ushort)glyphId);
            if (offset > -1)
            {
                // TODO: Implement
                // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#61-chained-contexts-substitution-format-1-simple-glyph-contexts
                return false;
            }

            return false;
        }

        internal sealed class ChainedSequenceRuleSetTable
        {
            private ChainedSequenceRuleSetTable(ChainedSequenceRuleTable[] subRules)
                => this.SequenceRuleTables = subRules;

            public ChainedSequenceRuleTable[] SequenceRuleTables { get; }

            public static ChainedSequenceRuleSetTable Load(BigEndianBinaryReader reader, long offset)
            {
                // ChainedSequenceRuleSet
                // +----------+--------------------------------------------+-----------------------------------------+
                // | Type     | Name                                       | Description                             |
                // +==========+============================================+=========================================+
                // | uint16   | chainedSeqRuleCount                        | Number of ChainedSequenceRule tables    |
                // +----------+--------------------------------------------+-----------------------------------------+
                // | Offset16 | chainedSeqRuleOffsets[chainedSeqRuleCount] | Array of offsets to ChainedSequenceRule |
                // |          |                                            | tables, from beginning of               |
                // |          |                                            | ChainedSequenceRuleSet table            |
                // +----------+--------------------------------------------+-----------------------------------------+
                reader.Seek(offset, SeekOrigin.Begin);
                ushort chainedSeqRuleCount = reader.ReadUInt16();
                ushort[] chainedSeqRuleOffsets = reader.ReadUInt16Array(chainedSeqRuleCount);

                var chainedSequenceRules = new ChainedSequenceRuleTable[chainedSeqRuleCount];
                for (int i = 0; i < chainedSequenceRules.Length; i++)
                {
                    chainedSequenceRules[i] = ChainedSequenceRuleTable.Load(reader, offset + chainedSeqRuleOffsets[i]);
                }

                return new ChainedSequenceRuleSetTable(chainedSequenceRules);
            }

            public sealed class ChainedSequenceRuleTable
            {
                private ChainedSequenceRuleTable(
                    ushort[] backtrackSequence,
                    ushort[] inputSequence,
                    ushort[] lookaheadSequence,
                    SequenceLookupRecord[] seqLookupRecords)
                {
                    this.BacktrackSequence = backtrackSequence;
                    this.InputSequence = inputSequence;
                    this.LookaheadSequence = lookaheadSequence;
                    this.SequenceLookupRecords = seqLookupRecords;
                }

                public ushort[] BacktrackSequence { get; }

                public ushort[] InputSequence { get; }

                public ushort[] LookaheadSequence { get; }

                public SequenceLookupRecord[] SequenceLookupRecords { get; }

                public static ChainedSequenceRuleTable Load(BigEndianBinaryReader reader, long offset)
                {
                    // ChainedSequenceRule
                    // +----------------------+----------------------------------------+--------------------------------------------+
                    // | Type                 | Name                                   | Description                                |
                    // +======================+========================================+============================================+
                    // | uint16               | backtrackGlyphCount                    | Number of glyphs in the backtrack sequence |
                    // +----------------------+----------------------------------------+--------------------------------------------+
                    // | uint16               | backtrackSequence[backtrackGlyphCount] | Array of backtrack glyph IDs               |
                    // +----------------------+----------------------------------------+--------------------------------------------+
                    // | uint16               | inputGlyphCount                        | Number of glyphs in the input sequence     |
                    // +----------------------+----------------------------------------+--------------------------------------------+
                    // | uint16               | inputSequence[inputGlyphCount - 1]     | Array of input glyph IDs—start with        |
                    // |                      |                                        | second glyph                               |
                    // +----------------------+----------------------------------------+--------------------------------------------+
                    // | uint16               | lookaheadGlyphCount                    | Number of glyphs in the lookahead sequence |
                    // +----------------------+----------------------------------------+--------------------------------------------+
                    // | uint16               | lookaheadSequence[lookaheadGlyphCount] | Array of lookahead glyph IDs               |
                    // +----------------------+----------------------------------------+--------------------------------------------+
                    // | uint16               | seqLookupCount                         | Number of SequenceLookupRecords            |
                    // +----------------------+----------------------------------------+--------------------------------------------+
                    // | SequenceLookupRecord | seqLookupRecords[seqLookupCount]       | Array of SequenceLookupRecords             |
                    // +----------------------+----------------------------------------+--------------------------------------------+
                    reader.Seek(offset, SeekOrigin.Begin);
                    ushort backtrackGlyphCount = reader.ReadUInt16();
                    ushort[] backtrackSequence = reader.ReadUInt16Array(backtrackGlyphCount);

                    ushort inputGlyphCount = reader.ReadUInt16();
                    ushort[] inputSequence = reader.ReadUInt16Array(inputGlyphCount - 1);

                    ushort lookaheadGlyphCount = reader.ReadUInt16();
                    ushort[] lookaheadSequence = reader.ReadUInt16Array(lookaheadGlyphCount);

                    ushort seqLookupCount = reader.ReadUInt16();
                    SequenceLookupRecord[] seqLookupRecords = SequenceLookupRecord.LoadArray(reader, seqLookupCount);

                    return new ChainedSequenceRuleTable(backtrackSequence, inputSequence, lookaheadSequence, seqLookupRecords);
                }
            }
        }
    }

    internal class LookupType6Format3SubTable : LookupSubTable
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

        public static LookupSubTable Load(BigEndianBinaryReader reader, long offset)
        {
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

            CoverageTable[] backtrackCoverageTables = CoverageTable.LoadArray(reader, offset, backtrackCoverageOffsets);
            CoverageTable[] inputCoverageTables = CoverageTable.LoadArray(reader, offset, inputCoverageOffsets);
            CoverageTable[] lookaheadCoverageTables = CoverageTable.LoadArray(reader, offset, lookaheadCoverageOffsets);

            return new LookupType6Format3SubTable(seqLookupRecords, backtrackCoverageTables, inputCoverageTables, lookaheadCoverageTables);
        }

        public override bool TrySubstition(GSubTable table, GlyphSubstitutionCollection collection, ushort index, int count)
        {
            int glyphId = collection[index][0];
            if (glyphId < 0)
            {
                return false;
            }

            int inputLength = this.inputCoverageTables.Length;

            // Check that there are enough context glyphs
            if (index < this.backtrackCoverageTables.Length
                || inputLength + this.lookaheadCoverageTables.Length > count)
            {
                return false;
            }

            // Check all coverages: if any of them does not match, abort substitution
            for (int i = 0; i < this.inputCoverageTables.Length; ++i)
            {
                int id = collection[index + i][0];
                if (id < 0 || this.inputCoverageTables[i].CoverageIndexOf((ushort)id) < 0)
                {
                    return false;
                }
            }

            for (int i = 0; i < this.backtrackCoverageTables.Length; ++i)
            {
                int id = collection[index - 1 - i][0];
                if (id < 0 || this.backtrackCoverageTables[i].CoverageIndexOf((ushort)id) < 0)
                {
                    return false;
                }
            }

            for (int i = 0; i < this.lookaheadCoverageTables.Length; ++i)
            {
                int id = collection[index + inputLength + i][0];
                if (id < 0 || this.lookaheadCoverageTables[i].CoverageIndexOf((ushort)id) < 0)
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
                if (lookup.TrySubstition(table, collection, (ushort)(index + sequenceIndex), count - sequenceIndex))
                {
                    hasChanged = true;
                }
            }

            return hasChanged;
        }
    }
}
