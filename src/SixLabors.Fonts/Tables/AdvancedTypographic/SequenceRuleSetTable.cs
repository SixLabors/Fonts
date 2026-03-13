// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// A SequenceRuleSet table contains an array of SequenceRule tables that define context rules
/// for simple (glyph ID based) glyph contexts in Sequence Context Format 1.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-1-simple-glyph-contexts"/>
/// </summary>
internal sealed class SequenceRuleSetTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SequenceRuleSetTable"/> class.
    /// </summary>
    /// <param name="sequenceRuleTables">The array of sequence rule tables.</param>
    private SequenceRuleSetTable(SequenceRuleTable[] sequenceRuleTables)
        => this.SequenceRuleTables = sequenceRuleTables;

    /// <summary>
    /// Gets the array of sequence rule tables.
    /// </summary>
    public SequenceRuleTable[] SequenceRuleTables { get; }

    /// <summary>
    /// Loads the <see cref="SequenceRuleSetTable"/> from the binary reader at the specified offset.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from the beginning of the SequenceRuleSet table.</param>
    /// <returns>The <see cref="SequenceRuleSetTable"/>.</returns>
    public static SequenceRuleSetTable Load(BigEndianBinaryReader reader, long offset)
    {
        // SequenceRuleSet
        // +----------+------------------------------+----------------------------------------------------------------+
        // | Type     | Name                         | Description                                                    |
        // +==========+==============================+================================================================+
        // | uint16   | seqRuleCount                 | Number of SequenceRule tables                                  |
        // +----------+------------------------------+----------------------------------------------------------------+
        // | Offset16 | seqRuleOffsets[posRuleCount] | Array of offsets to SequenceRule tables, from beginning of the |
        // |          |                              | SequenceRuleSet table                                          |
        // +----------+------------------------------+----------------------------------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);
        ushort seqRuleCount = reader.ReadUInt16();

        using Buffer<ushort> seqRuleOffsetsBuffer = new(seqRuleCount);
        Span<ushort> seqRuleOffsets = seqRuleOffsetsBuffer.GetSpan();
        reader.ReadUInt16Array(seqRuleOffsets);

        var sequenceRuleTables = new SequenceRuleTable[seqRuleCount];
        for (int i = 0; i < sequenceRuleTables.Length; i++)
        {
            sequenceRuleTables[i] = SequenceRuleTable.Load(reader, offset + seqRuleOffsets[i]);
        }

        return new SequenceRuleSetTable(sequenceRuleTables);
    }
}
