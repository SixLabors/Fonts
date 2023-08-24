// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

internal sealed class SequenceRuleSetTable
{
    private SequenceRuleSetTable(SequenceRuleTable[] sequenceRuleTables)
        => this.SequenceRuleTables = sequenceRuleTables;

    public SequenceRuleTable[] SequenceRuleTables { get; }

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
