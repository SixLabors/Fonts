// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Gsub
{
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
            ushort[] seqRuleOffsets = reader.ReadUInt16Array(seqRuleCount);

            var subRules = new ChainedClassSequenceRuleTable[seqRuleCount];
            for (int i = 0; i < subRules.Length; i++)
            {
                subRules[i] = ChainedClassSequenceRuleTable.Load(reader, offset + seqRuleOffsets[i]);
            }

            return new ChainedClassSequenceRuleSetTable(subRules);
        }
    }
}
