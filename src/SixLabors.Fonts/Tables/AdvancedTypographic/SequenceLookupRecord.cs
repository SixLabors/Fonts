// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic
{
    /// <summary>
    /// For all formats for both contextual and chained contextual lookups, a common record format
    /// is used to specify an action—a nested lookup—to be applied to a glyph at a particular
    /// sequence position within the input sequence.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-lookup-record"/>
    /// </summary>
    internal struct SequenceLookupRecord
    {
        public SequenceLookupRecord(ushort sequenceIndex, ushort lookupListIndex)
        {
            this.SequenceIndex = sequenceIndex;
            this.LookupListIndex = lookupListIndex;
        }

        public ushort SequenceIndex { get; }

        public ushort LookupListIndex { get; }

        public static SequenceLookupRecord[] LoadArray(BigEndianBinaryReader reader, int count)
        {
            // +--------+-----------------+---------------------------------------------------+
            // | Type   | Name            | Description                                       |
            // +========+=================+===================================================+
            // | uint16 | SequenceIndex   | Index into current glyph sequence-first glyph = 0 |
            // +--------+-----------------+---------------------------------------------------+
            // | uint16 | LookupListIndex | Lookup to apply to that position-zero-based       |
            // +--------+-----------------+---------------------------------------------------+
            var records = new SequenceLookupRecord[count];
            for (int i = 0; i < records.Length; i++)
            {
                records[i] = new SequenceLookupRecord(reader.ReadUInt16(), reader.ReadUInt16());
            }

            return records;
        }
    }
}
