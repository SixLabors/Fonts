// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// A ChainedClassSequenceRule table describes a chained context rule using glyph class values
/// for backtrack, input, and lookahead sequences.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-2-class-based-glyph-contexts"/>
/// </summary>
internal sealed class ChainedClassSequenceRuleTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChainedClassSequenceRuleTable"/> class.
    /// </summary>
    /// <param name="backtrackSequence">The array of backtrack-sequence classes.</param>
    /// <param name="inputSequence">The array of input sequence classes, beginning with the second glyph position.</param>
    /// <param name="lookaheadSequence">The array of lookahead-sequence classes.</param>
    /// <param name="seqLookupRecords">The array of sequence lookup records.</param>
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

    /// <summary>
    /// Gets the array of backtrack-sequence classes.
    /// </summary>
    public ushort[] BacktrackSequence { get; }

    /// <summary>
    /// Gets the array of input sequence classes, beginning with the second glyph position.
    /// </summary>
    public ushort[] InputSequence { get; }

    /// <summary>
    /// Gets the array of lookahead-sequence classes.
    /// </summary>
    public ushort[] LookaheadSequence { get; }

    /// <summary>
    /// Gets the array of sequence lookup records specifying actions to be applied.
    /// </summary>
    public SequenceLookupRecord[] SequenceLookupRecords { get; }

    /// <summary>
    /// Loads the <see cref="ChainedClassSequenceRuleTable"/> from the binary reader at the specified offset.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from the beginning of the ChainedClassSequenceRule table.</param>
    /// <returns>The <see cref="ChainedClassSequenceRuleTable"/>.</returns>
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
