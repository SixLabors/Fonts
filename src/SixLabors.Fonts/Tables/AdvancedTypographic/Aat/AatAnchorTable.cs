// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Buffers.Binary;
using SixLabors.Fonts.Tables.AdvancedTypographic.GPos;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Aat;

/// <summary>
/// The Apple ankr table supplies design-unit anchor points to kerx attachments.
/// <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html"/>
/// </summary>
internal sealed class AatAnchorTable : Table
{
    private readonly AatLookupTable lookup;
    private readonly byte[] anchors;

    /// <summary>
    /// Initializes a new instance of the <see cref="AatAnchorTable"/> class.
    /// </summary>
    /// <param name="lookup">The lookup from glyph IDs to anchor lists.</param>
    /// <param name="anchors">The encoded anchor lists.</param>
    private AatAnchorTable(AatLookupTable lookup, byte[] anchors)
    {
        this.lookup = lookup;
        this.anchors = anchors;
    }

    /// <summary>
    /// Reads the anchor lists referenced by the lookup table.
    /// </summary>
    /// <param name="fontReader">The font reader.</param>
    /// <param name="glyphCount">The number of glyphs in the font.</param>
    /// <returns>The anchor table, or <see langword="null"/> if it is missing or invalid.</returns>
    public static AatAnchorTable? Load(FontReader fontReader, int glyphCount)
    {
        if (!fontReader.TryGetBoundedReaderAtTablePosition("ankr", out BigEndianBinaryReader? reader))
        {
            return null;
        }

        using (reader)
        {
            uint tableLength = (uint)reader.BaseStream.Length;

            // +----------+-------------+------------------------------------------------------+
            // | Type     | Name        | Description                                          |
            // +----------+-------------+------------------------------------------------------+
            // | uint16   | version     | 0.                                                   |
            // | uint16   | flags       | Reserved.                                            |
            // | Offset32 | lookupTable | Offset from ankr to the glyph-to-anchor-list lookup. |
            // | Offset32 | glyphData   | Offset from ankr to the start of the lists.          |
            // +----------+-------------+------------------------------------------------------+
            if (tableLength < 12 || reader.ReadUInt16() != 0)
            {
                return null;
            }

            _ = reader.ReadUInt16();
            uint lookupOffset = reader.ReadOffset32();
            uint glyphData = reader.ReadOffset32();
            uint anchorEnd = glyphData < lookupOffset ? lookupOffset : tableLength;
            if (lookupOffset < 12 || glyphData < 12 || lookupOffset == glyphData || lookupOffset >= tableLength || glyphData > tableLength ||
                anchorEnd - glyphData > int.MaxValue)
            {
                return null;
            }

            uint lookupEnd = lookupOffset < glyphData ? glyphData : tableLength;
            AatLookupTable? lookup = AatLookupTable.Load(reader, lookupOffset, lookupEnd, glyphCount, 2);
            if (lookup is null)
            {
                return null;
            }

            // Lists can share coordinate bytes. Keep one copy of the data and
            // read only the points used by positioning.
            reader.Seek(glyphData, SeekOrigin.Begin);
            return new(lookup, reader.ReadBytes((int)(anchorEnd - glyphData)));
        }
    }

    /// <summary>
    /// Gets an indexed anchor in design units, defaulting to the origin if absent.
    /// </summary>
    /// <param name="glyph">The glyph ID.</param>
    /// <param name="index">The zero-based index in the glyph's anchor list.</param>
    /// <returns>The anchor in design units, or the origin if the point is unavailable.</returns>
    public AnchorXY GetAnchor(ushort glyph, ushort index)
    {
        if (!this.lookup.TryGetValue(glyph, out uint offset) ||
            !AatStateTable.Contains(offset, 4, this.anchors.Length))
        {
            return default;
        }

        // Each list contains a count followed by pairs of signed coordinates.
        ReadOnlySpan<byte> list = this.anchors.AsSpan((int)offset);
        uint count = BinaryPrimitives.ReadUInt32BigEndian(list);
        if (index >= count || count > (uint)(list.Length - 4) / 4)
        {
            return default;
        }

        ReadOnlySpan<byte> point = list[(4 + (index * 4))..];
        return new(BinaryPrimitives.ReadInt16BigEndian(point), BinaryPrimitives.ReadInt16BigEndian(point[2..]));
    }
}
