// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// For all formats for both contextual and chained contextual lookups, a common record format
/// is used to specify an action—a nested lookup—to be applied to a glyph at a particular
/// sequence position within the input sequence.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-lookup-record"/>
/// </summary>
[DebuggerDisplay("SequenceIndex: {SequenceIndex}, LookupListIndex: {LookupListIndex}")]
internal readonly struct SequenceLookupRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SequenceLookupRecord"/> struct.
    /// </summary>
    /// <param name="sequenceIndex">The index into the current glyph sequence (first glyph = 0).</param>
    /// <param name="lookupListIndex">The lookup to apply at that position (zero-based).</param>
    public SequenceLookupRecord(ushort sequenceIndex, ushort lookupListIndex)
    {
        this.SequenceIndex = sequenceIndex;
        this.LookupListIndex = lookupListIndex;
    }

    /// <summary>
    /// Gets the index into the current glyph sequence (first glyph = 0).
    /// </summary>
    public ushort SequenceIndex { get; }

    /// <summary>
    /// Gets the lookup to apply at the specified sequence position (zero-based index into the LookupList).
    /// </summary>
    public ushort LookupListIndex { get; }

    /// <summary>
    /// Loads an array of <see cref="SequenceLookupRecord"/> values from the binary reader.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="count">The number of records to read.</param>
    /// <returns>The array of <see cref="SequenceLookupRecord"/>.</returns>
    public static SequenceLookupRecord[] LoadArray(BigEndianBinaryReader reader, int count)
    {
        // +--------+-----------------+---------------------------------------------------+
        // | Type   | Name            | Description                                       |
        // +========+=================+===================================================+
        // | uint16 | SequenceIndex   | Index into current glyph sequence-first glyph = 0 |
        // +--------+-----------------+---------------------------------------------------+
        // | uint16 | LookupListIndex | Lookup to apply to that position-zero-based.      |
        // +--------+-----------------+---------------------------------------------------+
        var records = new SequenceLookupRecord[count];
        for (int i = 0; i < records.Length; i++)
        {
            records[i] = new SequenceLookupRecord(reader.ReadUInt16(), reader.ReadUInt16());
        }

        return records;
    }
}
