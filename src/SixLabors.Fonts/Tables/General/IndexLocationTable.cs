// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Exceptions;

namespace SixLabors.Fonts.Tables.General
{
    [TableName(TableName)]
    internal sealed class IndexLocationTable : Table
    {
        private const string TableName = "loca";

        public IndexLocationTable(uint[] convertedData)
        {
            this.GlyphOffsets = convertedData;
        }

        public uint[] GlyphOffsets { get; }

        public static IndexLocationTable Load(FontReader reader)
        {
            HeadTable head = reader.GetTable<HeadTable>();
            MaximumProfileTable maxp = reader.GetTable<MaximumProfileTable>();

            // must not get a binary reader untill all depended data is retrieved in case they need to use the stream
            using (BinaryReader binaryReader = reader.GetReaderAtTablePosition(TableName))
            {
                return Load(binaryReader, maxp.GlyphCount, head.IndexLocationFormat);
            }
        }

        public static IndexLocationTable Load(BinaryReader reader, int glyphCount, HeadTable.IndexLocationFormats format)
        {
            int entrycount = glyphCount + 1;

            if (format == HeadTable.IndexLocationFormats.Offset16)
            {
                // Type     | Name        | Description
                // ---------|-------------|---------------------------------------
                // Offset16 | offsets[n]  | The actual local offset divided by 2 is stored. The value of n is numGlyphs + 1. The value for numGlyphs is found in the 'maxp' table.
                ushort[] data = reader.ReadUInt16Array(entrycount);
                var convertedData = new uint[entrycount];
                for (int i = 0; i < entrycount; i++)
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
                uint[] data = reader.ReadUInt32Array(entrycount);

                return new IndexLocationTable(data);
            }
            else
            {
                throw new InvalidFontTableException("indexToLocFormat an invalid value", "head");
            }
        }
    }
}