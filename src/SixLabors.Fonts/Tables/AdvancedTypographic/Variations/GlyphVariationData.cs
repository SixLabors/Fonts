// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// Implements loading glyph variation data structure.
/// </summary>
/// <see href="https://docs.microsoft.com/de-de/typography/opentype/spec/otvarcommonformats#tuple-variation-store-header"/>
internal class GlyphVariationData
{
    /// <summary>
    /// Mask for the low bits to give the number of tuple variation tables.
    /// </summary>
    internal const int CountMask = 0x0FFF;

    /// <summary>
    /// Flag indicating that some or all tuple variation tables reference a shared set of "point" numbers.
    /// These shared numbers are represented as packed point number data at the start of the serialized data.
    /// </summary>
    internal const int SharedPointNumbersMask = 0x8000;

    public static GlyphVariationData Load(BigEndianBinaryReader reader, long offset, bool is32BitOffset, int axisCount)
    {
        // GlyphVariationData
        // +----------------------+-------------------------------------------+------------------------------------------------------------------------------+
        // | Type                 | Name                                      | Description                                                                  |
        // +======================+===========================================+==============================================================================+
        // | uint16               | tupleVariationCount                       | A packed field. The high 4 bits are flags,                                   |
        // |                      |                                           | and the low 12 bits are the number of tuple variation tables for this glyph. |
        // |                      |                                           | The count can be any number between 1 and 4095.                              |
        // +----------------------+-------------------------------------------+------------------------------------------------------------------------------+
        // | Offset16             | dataOffset                                | Offset from the start of the GlyphVariationData table to the serialized data.|
        // +----------------------+-------------------------------------------+------------------------------------------------------------------------------+
        // | TupleVariation       | tupleVariationHeaders[tupleVariationCount]| Array of tuple variation headers.                                            |
        // +----------------------+-------------------------------------------+------------------------------------------------------------------------------+
        // NOTE: 'offset' is relative to the start of the gvar table.
        reader.Seek(offset, SeekOrigin.Begin);
        ushort tupleVariationCount = reader.ReadUInt16();
        bool sharedPointNumbers = (tupleVariationCount & SharedPointNumbersMask) == SharedPointNumbersMask;

        // Spec: dataOffset is Offset16 (always 16-bit), independent of the gvar offset array format.
        // This offset is relative to the start of this GlyphVariationData table.
        ushort serializedDataOffset = reader.ReadOffset16();

        TupleVariation[] variationHeaders = new TupleVariation[tupleVariationCount & CountMask];
        for (int i = 0; i < variationHeaders.Length; i++)
        {
            variationHeaders[i] = TupleVariation.Load(reader, axisCount);
        }

        long serializedDataPos = offset + serializedDataOffset;
        reader.Seek(serializedDataPos, SeekOrigin.Begin);

        _ = sharedPointNumbers;
        _ = is32BitOffset;
        _ = axisCount;
        _ = variationHeaders;
        return new GlyphVariationData();
    }
}
