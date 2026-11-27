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
    /// <returns>A class sequence rule set table. A truncated set has no rules.</returns>
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
        long tableEnd = reader.BaseStream.Length;
        if (offset > tableEnd - reader.StartOfStream - sizeof(ushort))
        {
            return new ClassSequenceRuleSetTable([]);
        }

        reader.Seek(offset, SeekOrigin.Begin);
        ushort seqRuleCount = reader.ReadUInt16();

        // The rule offsets must fit in the layout table before we read them.
        if (seqRuleCount * sizeof(ushort) > tableEnd - reader.BaseStream.Position)
        {
            return new ClassSequenceRuleSetTable([]);
        }

        using Buffer<ushort> seqRuleOffsetsBuffer = new(seqRuleCount);
        Span<ushort> seqRuleOffsets = seqRuleOffsetsBuffer.GetSpan();
        reader.ReadUInt16Array(seqRuleOffsets);

        ClassSequenceRuleTable[] subRules = new ClassSequenceRuleTable[seqRuleCount];
        int validCount = 0;
        for (int i = 0; i < subRules.Length; i++)
        {
            ClassSequenceRuleTable? rule = ClassSequenceRuleTable.Load(reader, offset + seqRuleOffsets[i]);
            if (rule is not null)
            {
                subRules[validCount++] = rule;
            }
        }

        if (validCount != subRules.Length)
        {
            // Preserve the order of valid rules. A failed rule has no input
            // to match and cannot stop a later rule from applying.
            Array.Resize(ref subRules, validCount);
        }

        return new ClassSequenceRuleSetTable(subRules);
    }
}
