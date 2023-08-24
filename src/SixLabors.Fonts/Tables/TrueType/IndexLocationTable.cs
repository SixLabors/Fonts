// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using SixLabors.Fonts.Tables.General;

namespace SixLabors.Fonts.Tables.TrueType
{
    internal sealed class IndexLocationTable : Table
    {
        internal const string TableName = "loca";

        public IndexLocationTable(uint[] convertedData)
            => this.GlyphOffsets = convertedData;

        public uint[] GlyphOffsets { get; }

        public static IndexLocationTable? Load(FontReader fontReader)
        {
            HeadTable head = fontReader.GetTable<HeadTable>();

            MaximumProfileTable maxp = fontReader.GetTable<MaximumProfileTable>();

            // Must not get a binary reader until all depended data is retrieved in case they need to use the stream.
            if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
            {
                return null;
            }

            using (binaryReader)
            {
                return Load(binaryReader, maxp.GlyphCount, head.IndexLocationFormat);
            }
        }

        public static IndexLocationTable Load(BigEndianBinaryReader reader, int glyphCount, HeadTable.IndexLocationFormats format)
        {
            int entryCount = glyphCount + 1;

            if (format == HeadTable.IndexLocationFormats.Offset16)
            {
                // Type     | Name        | Description
                // ---------|-------------|---------------------------------------
                // Offset16 | offsets[n]  | The actual local offset divided by 2 is stored. The value of n is numGlyphs + 1. The value for numGlyphs is found in the 'maxp' table.
                using Buffer<ushort> dataBuffer = new(entryCount);
                Span<ushort> data = dataBuffer.GetSpan();
                reader.ReadUInt16Array(data);

                uint[] convertedData = new uint[entryCount];
                for (int i = 0; i < entryCount; i++)
                {
                    convertedData[i] = (uint)(data[i] * 2);
                }

                return new IndexLocationTable(convertedData);
            }
            else if (format == HeadTable.IndexLocationFormats.Offset32)
            {
                // Type     | Name        | Description
                // ---------|-------------|---------------------------------------
                // Offset32 | offsets[n]  | The actual local offset is stored. The value of n is numGlyphs + 1. The value for numGlyphs is found in the 'maxp' table.
                uint[] data = reader.ReadUInt32Array(entryCount);

                return new IndexLocationTable(data);
            }
            else
            {
                throw new InvalidFontTableException("indexToLocFormat an invalid value", "head");
            }
        }
    }
}
