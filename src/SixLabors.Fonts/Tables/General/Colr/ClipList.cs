// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Represents the COLR v1 ClipList table, which maps ranges of glyph IDs to clip boxes
/// that constrain paint operations.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#cliplist-table"/>
/// </summary>
internal sealed class ClipList
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClipList"/> class.
    /// </summary>
    /// <param name="records">The array of clip records mapping glyph ID ranges to clip box offsets.</param>
    /// <param name="boxes">The array of resolved clip boxes, one per record. Null entries indicate an unknown format.</param>
    public ClipList(ClipRecord[] records, ClipBox?[] boxes)
    {
        this.Records = records;
        this.Boxes = boxes;
    }

    /// <summary>
    /// Gets the array of clip records, sorted by start glyph ID.
    /// </summary>
    public ClipRecord[] Records { get; }

    /// <summary>
    /// Gets the array of resolved clip boxes, one per record.
    /// A <see langword="null"/> entry indicates a clip box with an unknown format.
    /// </summary>
    public ClipBox?[] Boxes { get; }

    /// <summary>
    /// Gets the number of clip records.
    /// </summary>
    public int Count => this.Records.Length;

    /// <summary>
    /// Loads a <see cref="ClipList"/> from the given reader at the specified offset.
    /// </summary>
    /// <param name="reader">The binary reader positioned within the COLR table.</param>
    /// <param name="offset">The offset from the beginning of the COLR table to the ClipList.</param>
    /// <returns>The loaded <see cref="ClipList"/>, or <see langword="null"/> if the offset is zero.</returns>
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

    /// <summary>
    /// Attempts to retrieve the clip box bounds for the specified glyph ID using a binary search
    /// over the sorted clip records.
    /// </summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <param name="colr">The COLR table used for resolving variation deltas.</param>
    /// <param name="processor">The glyph variation processor, or <see langword="null"/> for non-variable fonts.</param>
    /// <param name="bounds">
    /// When this method returns, contains the clip box bounds if found; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if a clip box was found for the glyph; otherwise, <see langword="false"/>.</returns>
    public bool TryGetClipBox(
        ushort glyphId,
        ColrTable colr,
        GlyphVariationProcessor? processor,
        [NotNullWhen(true)] out Bounds? bounds)
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
                bounds = null;
                return false;
            }

            bounds = box.GetBounds(colr, processor);
            return true;
        }

        bounds = null;
        return false;
    }
}
