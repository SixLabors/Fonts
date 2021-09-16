// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Gsub
{
    internal sealed class ChainedSequenceRuleTable
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
