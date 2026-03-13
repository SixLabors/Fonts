// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Tables.General.Kern;

/// <summary>
/// Represents the OpenType 'kern' table, which contains kerning pair adjustments
/// for positioning glyphs within a font.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern"/>
/// </summary>
internal sealed class KerningTable : Table
{
    /// <summary>
    /// The table tag name identifying the 'kern' table.
    /// </summary>
    internal const string TableName = "kern";

    /// <summary>
    /// The array of kerning subtables contained in this table.
    /// </summary>
    private readonly KerningSubTable[] kerningSubTable;

    /// <summary>
    /// Initializes a new instance of the <see cref="KerningTable"/> class.
    /// </summary>
    /// <param name="kerningSubTable">The array of kerning subtables.</param>
    public KerningTable(KerningSubTable[] kerningSubTable)
        => this.kerningSubTable = kerningSubTable;

    /// <summary>
    /// Gets the number of kerning subtables.
    /// </summary>
    public int Count => this.kerningSubTable.Length;

    /// <summary>
    /// Loads the <see cref="KerningTable"/> from the specified font reader.
    /// Returns an empty table if the 'kern' table is not present.
    /// </summary>
    /// <param name="fontReader">The font reader to read the table from.</param>
    /// <returns>The loaded <see cref="KerningTable"/>.</returns>
    public static KerningTable Load(FontReader fontReader)
    {
        if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
        {
            // this table is optional.
            return new KerningTable([]);
        }

        using (binaryReader)
        {
            // Move to start of table.
            return Load(binaryReader);
        }
    }

    /// <summary>
    /// Loads the <see cref="KerningTable"/> from the specified binary reader.
    /// </summary>
    /// <param name="reader">The binary reader positioned at the start of the kern table data.</param>
    /// <returns>The loaded <see cref="KerningTable"/>.</returns>
    public static KerningTable Load(BigEndianBinaryReader reader)
    {
        // +--------+---------+-------------------------------------------+
        // | Type   | Field   | Description                               |
        // +========+=========+===========================================+
        // | uint16 | version | Table version number(0)                   |
        // +--------+---------+-------------------------------------------+
        // | uint16 | nTables | Number of subtables in the kerning table. |
        // +--------+---------+-------------------------------------------+
        ushort version = reader.ReadUInt16();
        ushort subTableCount = reader.ReadUInt16();

        List<KerningSubTable> tables = new(subTableCount);
        for (int i = 0; i < subTableCount; i++)
        {
            KerningSubTable? t = KerningSubTable.Load(reader); // returns null for unknown/supported table format
            if (t != null)
            {
                tables.Add(t);
            }
        }

        return new KerningTable([.. tables]);
    }

    /// <summary>
    /// Updates glyph positions by applying kerning adjustments for the specified glyph pair.
    /// </summary>
    /// <param name="fontMetrics">The font metrics used for position calculations.</param>
    /// <param name="collection">The glyph positioning collection to update.</param>
    /// <param name="left">The index of the left glyph in the collection.</param>
    /// <param name="right">The index of the right glyph in the collection.</param>
    public void UpdatePositions(FontMetrics fontMetrics, GlyphPositioningCollection collection, int left, int right)
    {
        if (this.Count == 0 || collection.Count == 0)
        {
            return;
        }

        GlyphShapingData current = collection[left];
        if (current.IsKerned)
        {
            // Already kerned via previous processing.
            return;
        }

        ushort currentId = current.GlyphId;
        ushort nextId = collection[right].GlyphId;

        if (this.TryGetKerningOffset(currentId, nextId, out Vector2 result))
        {
            collection.Advance(fontMetrics, left, currentId, (short)result.X, (short)result.Y);
            current.IsKerned = true;
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
