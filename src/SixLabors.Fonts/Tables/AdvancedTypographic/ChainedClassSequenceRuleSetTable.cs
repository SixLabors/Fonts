// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

internal sealed class ChainedClassSequenceRuleSetTable
{
    private ChainedClassSequenceRuleSetTable(ChainedClassSequenceRuleTable[] subRules) => this.SubRules = subRules;

    public ChainedClassSequenceRuleTable[] SubRules { get; }

    public static ChainedClassSequenceRuleSetTable Load(BigEndianBinaryReader reader, long offset)
    {
        // ClassSequenceRuleSet
        // +----------+----------------------------------------+---------------------------------------+
        // | Type     | Name                                   | Description                           |
        // +==========+========================================+=======================================+
        // | uint16   | classSeqRuleCount                      | Number of ClassSequenceRule tables    |
        // +----------+----------------------------------------+---------------------------------------+
        // | Offset16 | classSeqRuleOffsets[classSeqRuleCount] | Array of offsets to ClassSequenceRule |
        // |          |                                        | tables, from beginning of             |
        // |          |                                        | ClassSequenceRuleSet table            |
        // +----------+----------------------------------------+---------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);
        ushort seqRuleCount = reader.ReadUInt16();

        using Buffer<ushort> seqRuleOffsetsBuffer = new(seqRuleCount);
        Span<ushort> seqRuleOffsets = seqRuleOffsetsBuffer.GetSpan();
        reader.ReadUInt16Array(seqRuleOffsets);

        ChainedClassSequenceRuleTable[] subRules = new ChainedClassSequenceRuleTable[seqRuleCount];
        for (int i = 0; i < subRules.Length; i++)
        {
            subRules[i] = ChainedClassSequenceRuleTable.Load(reader, offset + seqRuleOffsets[i]);
        }

        return new ChainedClassSequenceRuleSetTable(subRules);
    }
}
