// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Tables.General.Kern;

internal sealed class KerningTable : Table
{
    internal const string TableName = "kern";
    private readonly KerningSubTable[] kerningSubTable;

    public KerningTable(KerningSubTable[] kerningSubTable)
        => this.kerningSubTable = kerningSubTable;

    public int Count => this.kerningSubTable.Length;

    public static KerningTable Load(FontReader fontReader)
    {
        if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
        {
            // this table is optional.
            return new KerningTable(Array.Empty<KerningSubTable>());
        }

        using (binaryReader)
        {
            // Move to start of table.
            return Load(binaryReader);
        }
    }

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

        var tables = new List<KerningSubTable>(subTableCount);
        for (int i = 0; i < subTableCount; i++)
        {
            var t = KerningSubTable.Load(reader); // returns null for unknown/supported table format
            if (t != null)
            {
                tables.Add(t);
            }
        }

        return new KerningTable(tables.ToArray());
    }

    public void UpdatePositions(FontMetrics fontMetrics, GlyphPositioningCollection collection, int left, int right)
    {
        if (this.Count == 0 || collection.Count == 0)
        {
            return;
        }

        ushort previous = collection[left].GlyphId;
        ushort current = collection[right].GlyphId;

        if (this.TryGetKerningOffset(previous, current, out Vector2 result))
        {
            collection.Advance(fontMetrics, right, current, (short)result.X, (short)result.Y);
        }
    }

    public bool TryGetKerningOffset(ushort previous, ushort current, out Vector2 result)
    {
        result = Vector2.Zero;
        if (this.Count == 0 || previous == 0 || current == 0)
        {
            return false;
        }

        bool kerned = false;
        foreach (KerningSubTable sub in this.kerningSubTable)
        {
            kerned |= sub.TryApplyOffset(previous, current, ref result);
        }

        return kerned;
    }
}
