// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic
{
    internal sealed class ClassSequenceRuleSetTable
    {
        private ClassSequenceRuleSetTable(ClassSequenceRuleTable[] sequenceRuleTables)
            => this.SequenceRuleTables = sequenceRuleTables;

        public ClassSequenceRuleTable[] SequenceRuleTables { get; }

        public static ClassSequenceRuleSetTable Load(BigEndianBinaryReader reader, long offset)
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

            var subRules = new ClassSequenceRuleTable[seqRuleCount];
            for (int i = 0; i < subRules.Length; i++)
            {
                subRules[i] = ClassSequenceRuleTable.Load(reader, offset + seqRuleOffsets[i]);
            }

            return new ClassSequenceRuleSetTable(subRules);
        }
    }
}
