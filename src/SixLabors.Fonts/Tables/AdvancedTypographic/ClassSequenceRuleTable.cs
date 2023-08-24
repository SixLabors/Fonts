// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic
{
    internal sealed class ClassSequenceRuleTable
    {
        private ClassSequenceRuleTable(ushort[] inputSequence, SequenceLookupRecord[] seqLookupRecords)
        {
            this.InputSequence = inputSequence;
            this.SequenceLookupRecords = seqLookupRecords;
        }

        public ushort[] InputSequence { get; }

        public SequenceLookupRecord[] SequenceLookupRecords { get; }

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
}
