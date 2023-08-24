// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic
{
    internal sealed class ChainedSequenceRuleSetTable
    {
        private ChainedSequenceRuleSetTable(ChainedSequenceRuleTable[] subRules) => this.SequenceRuleTables = subRules;

        public ChainedSequenceRuleTable[] SequenceRuleTables { get; }

        public static ChainedSequenceRuleSetTable Load(BigEndianBinaryReader reader, long offset)
        {
            // ChainedSequenceRuleSet
            // +----------+--------------------------------------------+-----------------------------------------+
            // | Type     | Name                                       | Description                             |
            // +==========+============================================+=========================================+
            // | uint16   | chainedSeqRuleCount                        | Number of ChainedSequenceRule tables    |
            // +----------+--------------------------------------------+-----------------------------------------+
            // | Offset16 | chainedSeqRuleOffsets[chainedSeqRuleCount] | Array of offsets to ChainedSequenceRule |
            // |          |                                            | tables, from beginning of               |
            // |          |                                            | ChainedSequenceRuleSet table            |
            // +----------+--------------------------------------------+-----------------------------------------+
            reader.Seek(offset, SeekOrigin.Begin);
            ushort chainedSeqRuleCount = reader.ReadUInt16();

            using Buffer<ushort> chainedSeqRuleOffsetsBuffer = new(chainedSeqRuleCount);
            Span<ushort> chainedSeqRuleOffsets = chainedSeqRuleOffsetsBuffer.GetSpan();
            reader.ReadUInt16Array(chainedSeqRuleOffsets);

            var chainedSequenceRules = new ChainedSequenceRuleTable[chainedSeqRuleCount];
            for (int i = 0; i < chainedSequenceRules.Length; i++)
            {
                chainedSequenceRules[i] = ChainedSequenceRuleTable.Load(reader, offset + chainedSeqRuleOffsets[i]);
            }

            return new ChainedSequenceRuleSetTable(chainedSequenceRules);
        }
    }
}
