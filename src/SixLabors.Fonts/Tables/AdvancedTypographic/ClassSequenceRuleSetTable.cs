// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// A ClassSequenceRuleSet table contains an array of ClassSequenceRule tables that define
/// context rules for class-based glyph contexts in Sequence Context Format 2.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#sequence-context-format-2-class-based-glyph-contexts"/>
/// </summary>
internal sealed class ClassSequenceRuleSetTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClassSequenceRuleSetTable"/> class.
    /// </summary>
    /// <param name="sequenceRuleTables">The array of class sequence rule tables.</param>
    private ClassSequenceRuleSetTable(ClassSequenceRuleTable[] sequenceRuleTables)
        => this.SequenceRuleTables = sequenceRuleTables;

    /// <summary>
    /// Gets the array of class sequence rule tables.
    /// </summary>
    public ClassSequenceRuleTable[] SequenceRuleTables { get; }

    /// <summary>
    /// Loads the class sequence rule set table.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from beginning of the ClassSequenceRuleSet table.</param>
    /// <returns>A class sequence rule set table.</returns>
    public static ClassSequenceRuleSetTable Load(BigEndianBinaryReader reader, long offset)
    {
        // ClassSequenceRuleSet
        // +----------+----------------------------------------+---------------------------------------+
        // | Type     | Name                                   | Description                           |
        // +==========+========================================+=======================================+
        // | uint16   | classSeqRuleCount                      | Number of ClassSequenceRule tables.   |
        // +----------+----------------------------------------+---------------------------------------+
        // | Offset16 | classSeqRuleOffsets[classSeqRuleCount] | Array of offsets to ClassSequenceRule |
        // |          |                                        | tables, from beginning of             |
        // |          |                                        | ClassSequenceRuleSet table.           |
        // +----------+----------------------------------------+---------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);
        ushort seqRuleCount = reader.ReadUInt16();

        using Buffer<ushort> seqRuleOffsetsBuffer = new(seqRuleCount);
        Span<ushort> seqRuleOffsets = seqRuleOffsetsBuffer.GetSpan();
        reader.ReadUInt16Array(seqRuleOffsets);

        var subRules = new ClassSequenceRuleTable[seqRuleCount];
        for (int i = 0; i < subRules.Length; i++)
        {
            subRules[i] = ClassSequenceRuleTable.Load(reader, offset + seqRuleOffsets[i]);
        }

        return new ClassSequenceRuleSetTable(subRules);
    }
}
