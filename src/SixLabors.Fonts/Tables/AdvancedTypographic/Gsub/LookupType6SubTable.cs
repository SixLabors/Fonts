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
                // TODO: Implement 1 & 2
                3 => LookupType6Format3SubTable.Load(reader, offset),
                _ => new NotImplementedSubTable()
            };
        }
    }

    internal class LookupType6Format3SubTable : LookupSubTable
    {
        private readonly SequenceLookupRecord[] seqLookupRecords;
        private readonly CoverageTable[] backtrackingCoverageTables;
        private readonly CoverageTable[] inputCoverageTables;
        private readonly CoverageTable[] lookaheadCoverageTables;

        private LookupType6Format3SubTable(
            SequenceLookupRecord[] seqLookupRecords,
            CoverageTable[] backtrackingCoverageTables,
            CoverageTable[] inputCoverageTables,
            CoverageTable[] lookaheadCoverageTables)
        {
            this.seqLookupRecords = seqLookupRecords;
            this.backtrackingCoverageTables = backtrackingCoverageTables;
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

            CoverageTable[] backtrackingCoverageTables = CoverageTable.LoadArray(reader, offset, backtrackCoverageOffsets);
            CoverageTable[] inputCoverageTables = CoverageTable.LoadArray(reader, offset, inputCoverageOffsets);
            CoverageTable[] lookaheadCoverageTables = CoverageTable.LoadArray(reader, offset, lookaheadCoverageOffsets);

            return new LookupType6Format3SubTable(seqLookupRecords, backtrackingCoverageTables, inputCoverageTables, lookaheadCoverageTables);
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
            if (index < this.backtrackingCoverageTables.Length
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

            for (int i = 0; i < this.backtrackingCoverageTables.Length; ++i)
            {
                int id = collection[index - 1 - i][0];
                if (id < 0 || this.backtrackingCoverageTables[i].CoverageIndexOf((ushort)id) < 0)
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
