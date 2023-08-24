// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

internal sealed class ChainedClassSequenceRuleTable
{
    private ChainedClassSequenceRuleTable(
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

    public static ChainedClassSequenceRuleTable Load(BigEndianBinaryReader reader, long offset)
    {
        // ChainedClassSequenceRule
        // +----------------------+----------------------------------------+--------------------------------------------+
        // | Type                 | Name                                   | Description                                |
        // +======================+========================================+============================================+
        // | uint16               | backtrackGlyphCount                    | Number of glyphs in the backtrack          |
        // |                      |                                        | sequence                                   |
        // +----------------------+----------------------------------------+--------------------------------------------+
        // | uint16               | backtrackSequence[backtrackGlyphCount] | Array of backtrack-sequence classes        |
        // +----------------------+----------------------------------------+--------------------------------------------+
        // | uint16               | inputGlyphCount                        | Total number of glyphs in the input        |
        // |                      |                                        | sequence                                   |
        // +----------------------+----------------------------------------+--------------------------------------------+
        // | uint16               | inputSequence[inputGlyphCount - 1]     | Array of input sequence classes, beginning |
        // |                      |                                        | with the second glyph position             |
        // +----------------------+----------------------------------------+--------------------------------------------+
        // | uint16               | lookaheadGlyphCount                    | Number of glyphs in the lookahead          |
        // |                      |                                        | sequence                                   |
        // +----------------------+----------------------------------------+--------------------------------------------+
        // | uint16               | lookaheadSequence[lookaheadGlyphCount] | Array of lookahead-sequence classes        |
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

        return new ChainedClassSequenceRuleTable(backtrackSequence, inputSequence, lookaheadSequence, seqLookupRecords);
    }
}
