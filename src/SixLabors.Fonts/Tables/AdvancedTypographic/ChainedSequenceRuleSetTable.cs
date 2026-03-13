// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// A ChainedSequenceRuleSet table contains an array of ChainedSequenceRule tables that define
/// chained context rules for simple glyph contexts (glyph ID based).
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#chained-sequence-context-format-1-simple-glyph-contexts"/>
/// </summary>
internal sealed class ChainedSequenceRuleSetTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChainedSequenceRuleSetTable"/> class.
    /// </summary>
    /// <param name="subRules">The array of chained sequence rule tables.</param>
    private ChainedSequenceRuleSetTable(ChainedSequenceRuleTable[] subRules) => this.SequenceRuleTables = subRules;

    /// <summary>
    /// Gets the array of chained sequence rule tables.
    /// </summary>
    public ChainedSequenceRuleTable[] SequenceRuleTables { get; }

    /// <summary>
    /// Loads the <see cref="ChainedSequenceRuleSetTable"/> from the binary reader at the specified offset.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from the beginning of the ChainedSequenceRuleSet table.</param>
    /// <returns>The <see cref="ChainedSequenceRuleSetTable"/>.</returns>
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
