// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Tables.AdvancedTypographic;

namespace SixLabors.Fonts.Tables.General.Kern;

/// <summary>
/// Represents the OpenType 'kern' table, which contains kerning pair adjustments
/// for positioning glyphs within a font.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern"/>
/// </summary>
internal sealed class KernTable : Table
{
    /// <summary>
    /// The table tag name identifying the 'kern' table.
    /// </summary>
    public const string TableName = "kern";

    /// <summary>
    /// The array of kerning subtables contained in this table.
    /// </summary>
    private readonly KerningSubTable[] kerningSubTable;

    /// <summary>
    /// Initializes a new instance of the <see cref="KernTable"/> class.
    /// </summary>
    /// <param name="kerningSubTable">The array of kerning subtables.</param>
    public KernTable(KerningSubTable[] kerningSubTable)
        => this.kerningSubTable = kerningSubTable;

    /// <summary>
    /// Gets the number of kerning subtables.
    /// </summary>
    public int Count => this.kerningSubTable.Length;

    /// <summary>
    /// Loads the <see cref="KernTable"/> from the specified font reader.
    /// Returns an empty table if the 'kern' table is not present.
    /// </summary>
    /// <param name="fontReader">The font reader to read the table from.</param>
    /// <returns>The loaded <see cref="KernTable"/>.</returns>
    public static KernTable Load(FontReader fontReader)
    {
        if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
        {
            // this table is optional.
            return new KernTable([]);
        }

        using (binaryReader)
        {
            // Move to start of table.
            return Load(binaryReader);
        }
    }

    /// <summary>
    /// Loads the <see cref="KernTable"/> from the specified binary reader.
    /// </summary>
    /// <remarks>
    /// The version 0 header is defined by the
    /// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType 'kern' specification</see>.
    /// The version 1.0 header is defined by the
    /// <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kern.html">Apple TrueType Reference Manual</see>.
    /// </remarks>
    /// <param name="reader">The binary reader positioned at the start of the kern table data.</param>
    /// <returns>The loaded <see cref="KernTable"/>.</returns>
    public static KernTable Load(BigEndianBinaryReader reader)
    {
        // +--------+---------+-------------------------------------------+
        // | Type   | Field   | Description                               |
        // +========+=========+===========================================+
        // | uint16 | version | Table version number(0)                   |
        // +--------+---------+-------------------------------------------+
        // | uint16 | nTables | Number of subtables in the kerning table. |
        // +--------+---------+-------------------------------------------+
        ushort majorVersion = reader.ReadUInt16();
        int subTableCount;
        bool useAppleHeader;
        if (majorVersion == 0)
        {
            subTableCount = reader.ReadUInt16();
            useAppleHeader = false;
        }
        else if (majorVersion == 1)
        {
            _ = reader.ReadUInt16();
            subTableCount = checked((int)reader.ReadUInt32());
            useAppleHeader = true;
        }
        else
        {
            return new KernTable([]);
        }

        List<KerningSubTable> tables = new(subTableCount);
        for (int i = 0; i < subTableCount; i++)
        {
            KerningSubTable? t = KerningSubTable.Load(reader, useAppleHeader);
            if (t != null)
            {
                tables.Add(t);
            }
        }

        return new KernTable([.. tables]);
    }

    /// <summary>
    /// Updates glyph positions by applying kerning adjustments for the specified glyph pair.
    /// </summary>
    /// <param name="fontMetrics">The font metrics used for position calculations.</param>
    /// <param name="buffer">The glyph positioning buffer to update.</param>
    /// <param name="left">The index of the left glyph in the buffer.</param>
    /// <param name="right">The index of the right glyph in the buffer.</param>
    public void UpdatePositions(FontMetrics fontMetrics, ShapingBuffer buffer, int left, int right)
    {
        if (this.Count == 0 || buffer.Count == 0)
        {
            return;
        }

        ref GlyphShapingPosition currentPosition = ref buffer.PositionAt(left);
        if (currentPosition.IsKerned
            || buffer.MetricsAt(left).Metrics.FontMetrics != fontMetrics
            || buffer.MetricsAt(right).Metrics.FontMetrics != fontMetrics)
        {
            // A shared shaping buffer can contain glyph ids from several fonts.
            // A kerning table's glyph ids are meaningful only within its own font.
            return;
        }

        ushort currentId = buffer[left].GlyphId;
        ushort nextId = buffer[right].GlyphId;

        if (this.TryGetKerningOffset(currentId, nextId, out Vector2 result))
        {
            AdvancedTypographicUtils.ApplyPairKerning(fontMetrics, buffer, left, right, (int)result.X, (int)result.Y, yDown: false);
            currentPosition.IsKerned = true;
        }
    }

    /// <summary>
    /// Attempts to get the accumulated kerning offset for the specified pair of glyph indices
    /// by iterating through all kerning subtables.
    /// </summary>
    /// <param name="current">The glyph index of the current (left) glyph.</param>
    /// <param name="next">The glyph index of the next (right) glyph.</param>
    /// <param name="result">When this method returns, contains the accumulated kerning offset vector.</param>
    /// <returns><see langword="true"/> if any kerning was applied; otherwise, <see langword="false"/>.</returns>
    public bool TryGetKerningOffset(ushort current, ushort next, out Vector2 result)
    {
        result = Vector2.Zero;

        // Glyph zero stands for a missing character. It does not identify
        // a specific glyph that the font can kern.
        if (this.Count == 0 || current == 0 || next == 0)
        {
            return false;
        }

        bool kerned = false;
        foreach (KerningSubTable sub in this.kerningSubTable)
        {
            kerned |= sub.TryApplyOffset(current, next, ref result);
        }

        return kerned;
    }
}
