// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations
{
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
        /// Flag indicating that some or all tuple variation tables reference a shared set of “point” numbers.
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
            reader.Seek(offset, SeekOrigin.Begin);
            ushort tupleVariationCount = reader.ReadUInt16();
            bool sharedPointNumbers = (tupleVariationCount & SharedPointNumbersMask) == SharedPointNumbersMask;

            int tupleVariationTables = tupleVariationCount & CountMask;
            var variationHeaders = new TupleVariation[tupleVariationTables];
            for (int i = 0; i < tupleVariationTables; i++)
            {
                variationHeaders[i] = TupleVariation.Load(reader, axisCount);
            }

            // TODO: parse serialized data
            int serializedDataOffset = is32BitOffset ? reader.ReadInt32() : reader.ReadOffset16();
            reader.Seek(offset + serializedDataOffset, SeekOrigin.Begin);
            return new GlyphVariationData();
        }
    }
}
