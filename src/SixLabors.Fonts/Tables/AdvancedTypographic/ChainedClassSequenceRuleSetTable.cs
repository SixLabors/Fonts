// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// A ChainedClassSequenceRuleSet table contains an array of ChainedClassSequenceRule tables that define
/// chained context rules for class-based glyph contexts.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-2-class-based-glyph-contexts"/>
/// </summary>
internal sealed class ChainedClassSequenceRuleSetTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChainedClassSequenceRuleSetTable"/> class.
    /// </summary>
    /// <param name="subRules">The array of chained class sequence rule tables.</param>
    private ChainedClassSequenceRuleSetTable(ChainedClassSequenceRuleTable[] subRules) => this.SubRules = subRules;

    /// <summary>
    /// Gets the array of chained class sequence rule tables.
    /// </summary>
    public ChainedClassSequenceRuleTable[] SubRules { get; }

    /// <summary>
    /// Loads the <see cref="ChainedClassSequenceRuleSetTable"/> from the binary reader at the specified offset.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from the beginning of the ChainedClassSequenceRuleSet table.</param>
    /// <returns>The <see cref="ChainedClassSequenceRuleSetTable"/>.</returns>
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

        var subRules = new ChainedClassSequenceRuleTable[seqRuleCount];
        for (int i = 0; i < subRules.Length; i++)
        {
            subRules[i] = ChainedClassSequenceRuleTable.Load(reader, offset + seqRuleOffsets[i]);
        }

        return new ChainedClassSequenceRuleSetTable(subRules);
    }
}
