// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

internal sealed class ClipList
{
    public ClipList(ClipRecord[] records, ClipBox?[] boxes)
    {
        this.Records = records;
        this.Boxes = boxes;
    }

    public ClipRecord[] Records { get; }

    // One ClipBox per record; null means "no box".
    public ClipBox?[] Boxes { get; }

    public int Count => this.Records.Length;

    public static ClipList? Load(BigEndianBinaryReader reader, long offset)
    {
        if (offset == 0)
        {
            return null;
        }

        reader.Seek(offset, SeekOrigin.Begin);

        _ = reader.ReadByte(); // Version. Always 1.
        uint count = reader.ReadUInt32();

        ClipRecord[] records = new ClipRecord[count];
        for (int i = 0; i < count; i++)
        {
            ushort start = reader.ReadUInt16();
            ushort end = reader.ReadUInt16();
            uint boxOffset = reader.ReadOffset24();
            records[i] = new ClipRecord(start, end, boxOffset);
        }

        // TODO: Should this be nullable?
        ClipBox?[] boxes = new ClipBox?[count];
        for (int i = 0; i < count; i++)
        {
            uint boxOffset = records[i].ClipBoxOffset;
            reader.Seek(offset + boxOffset, SeekOrigin.Begin);

            byte format = reader.ReadByte();
            short xMin = reader.ReadFWORD();
            short yMin = reader.ReadFWORD();
            short xMax = reader.ReadFWORD();
            short yMax = reader.ReadFWORD();

            switch (format)
            {
                case 1:
                    boxes[i] = new ClipBoxFormat1(xMin, yMin, xMax, yMax);
                    break;

                case 2:
                    uint varIndexBase = reader.ReadUInt32();
                    boxes[i] = new ClipBoxFormat2(xMin, yMin, xMax, yMax, varIndexBase);
                    break;

                default:
                    boxes[i] = null; // Unknown format
                    break;
            }
        }

        return new ClipList(records, boxes);
    }

    public bool TryGetClipBox(ushort glyphId, IVariationResolver? varResolver, out Bounds bounds)
    {
        int lo = 0;
        int hi = this.Records.Length - 1;

        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            ClipRecord rec = this.Records[mid];

            if (glyphId < rec.StartGlyphId)
            {
                hi = mid - 1;
                continue;
            }

            if (glyphId > rec.EndGlyphId)
            {
                lo = mid + 1;
                continue;
            }

            ClipBox? box = this.Boxes[mid];
            if (box is null)
            {
                bounds = default;
                return false;
            }

            bounds = box.GetBounds(varResolver);
            return true;
        }

        bounds = default;
        return false;
    }
}
