// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// A ClassSequenceRule table describes a context rule using glyph class values.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-2-class-based-glyph-contexts"/>
/// </summary>
internal sealed class ClassSequenceRuleTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClassSequenceRuleTable"/> class.
    /// </summary>
    /// <param name="inputSequence">The array of input sequence classes, beginning with the second glyph position.</param>
    /// <param name="seqLookupRecords">The array of sequence lookup records.</param>
    private ClassSequenceRuleTable(ushort[] inputSequence, SequenceLookupRecord[] seqLookupRecords)
    {
        this.InputSequence = inputSequence;
        this.SequenceLookupRecords = seqLookupRecords;
    }

    /// <summary>
    /// Gets the array of input sequence classes, beginning with the second glyph position.
    /// </summary>
    public ushort[] InputSequence { get; }

    /// <summary>
    /// Gets the array of sequence lookup records specifying actions to be applied.
    /// </summary>
    public SequenceLookupRecord[] SequenceLookupRecords { get; }

    /// <summary>
    /// Loads the <see cref="ClassSequenceRuleTable"/> from the binary reader at the specified offset.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from the beginning of the ClassSequenceRule table.</param>
    /// <returns>The <see cref="ClassSequenceRuleTable"/>.</returns>
    public static ClassSequenceRuleTable Load(BigEndianBinaryReader reader, long offset)
    {
        // ClassSequenceRule
        // +----------------------+----------------------------------+------------------------------------------+
        // | Type                 | Name                             | Description                              |
        // +======================+==================================+==========================================+
        // | uint16               | glyphCount                       | Number of glyphs to be matched           |
        // +----------------------+----------------------------------+------------------------------------------+
        // | uint16               | seqLookupCount                   | Number of SequenceLookupRecords          |
        // +----------------------+----------------------------------+------------------------------------------+
        // | uint16               | inputSequence[glyphCount - 1]    | Sequence of classes to be matched to the |
        // |                      |                                  | input glyph sequence, beginning with the |
        // |                      |                                  | second glyph position                    |
        // +----------------------+----------------------------------+------------------------------------------+
        // | SequenceLookupRecord | seqLookupRecords[seqLookupCount] | Array of SequenceLookupRecords           |
        // +----------------------+----------------------------------+------------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);
        ushort glyphCount = reader.ReadUInt16();
        ushort seqLookupCount = reader.ReadUInt16();
        ushort[] inputSequence = reader.ReadUInt16Array(glyphCount - 1);
        SequenceLookupRecord[] seqLookupRecords = SequenceLookupRecord.LoadArray(reader, seqLookupCount);

        return new ClassSequenceRuleTable(inputSequence, seqLookupRecords);
    }
}
