// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// A SequenceRule table describes a context rule using glyph IDs in Sequence Context Format 1.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-1-simple-glyph-contexts"/>
/// </summary>
internal sealed class SequenceRuleTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SequenceRuleTable"/> class.
    /// </summary>
    /// <param name="inputSequence">The array of input glyph IDs, starting with the second glyph.</param>
    /// <param name="seqLookupRecords">The array of sequence lookup records.</param>
    private SequenceRuleTable(ushort[] inputSequence, SequenceLookupRecord[] seqLookupRecords)
    {
        this.InputSequence = inputSequence;
        this.SequenceLookupRecords = seqLookupRecords;
    }

    /// <summary>
    /// Gets the array of input glyph IDs, starting with the second glyph.
    /// </summary>
    public ushort[] InputSequence { get; }

    /// <summary>
    /// Gets the array of sequence lookup records specifying actions to be applied.
    /// </summary>
    public SequenceLookupRecord[] SequenceLookupRecords { get; }

    /// <summary>
    /// Loads the <see cref="SequenceRuleTable"/> from the binary reader at the specified offset.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from the beginning of the SequenceRule table.</param>
    /// <returns>The <see cref="SequenceRuleTable"/>.</returns>
    public static SequenceRuleTable Load(BigEndianBinaryReader reader, long offset)
    {
        // +----------------------+----------------------------------+---------------------------------------------------------+
        // | Type                 | Name                             | Description                                             |
        // +======================+==================================+=========================================================+
        // | uint16               | glyphCount                       | Number of glyphs in the input glyph sequence            |
        // +----------------------+----------------------------------+---------------------------------------------------------+
        // | uint16               | seqLookupCount                   | Number of SequenceLookupRecords                         |
        // +----------------------+----------------------------------+---------------------------------------------------------+
        // | uint16               | inputSequence[glyphCount - 1]    | Array of input glyph IDs—starting with the second glyph |
        // +----------------------+----------------------------------+---------------------------------------------------------+
        // | SequenceLookupRecord | seqLookupRecords[seqLookupCount] | Array of Sequence lookup records                        |
        // +----------------------+----------------------------------+---------------------------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);
        ushort glyphCount = reader.ReadUInt16();
        ushort seqLookupCount = reader.ReadUInt16();
        ushort[] inputSequence = reader.ReadUInt16Array(glyphCount - 1);
        SequenceLookupRecord[] seqLookupRecords = SequenceLookupRecord.LoadArray(reader, seqLookupCount);

        return new SequenceRuleTable(inputSequence, seqLookupRecords);
    }
}
