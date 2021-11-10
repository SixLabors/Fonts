// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic
{
    internal sealed class SequenceRuleTable
    {
        private SequenceRuleTable(ushort[] inputSequence, SequenceLookupRecord[] seqLookupRecords)
        {
            this.InputSequence = inputSequence;
            this.SequenceLookupRecords = seqLookupRecords;
        }

        public ushort[] InputSequence { get; }

        public SequenceLookupRecord[] SequenceLookupRecords { get; }

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
}
