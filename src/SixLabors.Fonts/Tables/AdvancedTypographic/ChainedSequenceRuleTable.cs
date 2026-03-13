// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// A ChainedSequenceRule table describes a chained context rule using glyph IDs
/// for backtrack, input, and lookahead sequences.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-1-simple-glyph-contexts"/>
/// </summary>
internal sealed class ChainedSequenceRuleTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChainedSequenceRuleTable"/> class.
    /// </summary>
    /// <param name="backtrackSequence">The array of backtrack glyph IDs.</param>
    /// <param name="inputSequence">The array of input glyph IDs, beginning with the second glyph.</param>
    /// <param name="lookaheadSequence">The array of lookahead glyph IDs.</param>
    /// <param name="seqLookupRecords">The array of sequence lookup records.</param>
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

    /// <summary>
    /// Gets the array of backtrack glyph IDs.
    /// </summary>
    public ushort[] BacktrackSequence { get; }

    /// <summary>
    /// Gets the array of input glyph IDs, beginning with the second glyph.
    /// </summary>
    public ushort[] InputSequence { get; }

    /// <summary>
    /// Gets the array of lookahead glyph IDs.
    /// </summary>
    public ushort[] LookaheadSequence { get; }

    /// <summary>
    /// Gets the sequence lookup records.
    /// The seqLookupRecords array lists the sequence lookup records that specify actions to be taken on glyphs at various positions within the input sequence.
    /// These do not have to be ordered in sequence position order; they are ordered according to the desired result.
    /// All of the sequence lookup records are processed in order, and each applies to the results of the actions indicated by the preceding record.
    /// </summary>
    public SequenceLookupRecord[] SequenceLookupRecords { get; }

    /// <summary>
    /// Loads the <see cref="ChainedSequenceRuleTable"/> from the binary reader at the specified offset.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from the beginning of the ChainedSequenceRule table.</param>
    /// <returns>The <see cref="ChainedSequenceRuleTable"/>.</returns>
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
