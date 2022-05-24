// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Tables.Cff
{
    [TableName(TableName)]
    internal sealed class CffTable : Table
    {
        internal const string TableName = "CFF "; // 4 chars

        private readonly Cff1GlyphData[] glyphs;

        public CffTable(Cff1Font cff1Font) => this.glyphs = cff1Font.Glyphs;

        public int GlyphCount => this.glyphs.Length;

        public Cff1GlyphData GetGlyph(int index)
            => this.glyphs[index];

        public static CffTable? Load(FontReader fontReader)
        {
            if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
            {
                return null;
            }

            using (binaryReader)
            {
                return Load(binaryReader);
            }
        }

        public static CffTable Load(BigEndianBinaryReader reader)
        {
            // +------+---------------+----------------------------------------+
            // | Type | Name          | Description                            |
            // +======+===============+========================================+
            // | byte | majorVersion  | Format major version. Set to 1.        |
            // +------+---------------+----------------------------------------+
            // | byte | minorVersion  | Format minor version. Set to zero.     |
            // +------+---------------+----------------------------------------+
            // | byte | headerSize    | Header size (bytes).                   |
            // +------+---------------+----------------------------------------+
            // | byte | topDictLength | Length of Top DICT structure in bytes. |
            // +------+---------------+----------------------------------------+
            long position = reader.BaseStream.Position;
            byte[] header = reader.ReadBytes(4);
            byte major = header[0];
            byte minor = header[1];
            byte hdrSize = header[2];
            byte offSize = header[3];

            switch (major)
            {
                case 1:
                    Cff1Parser parser = new();
                    return new(parser.Load(reader, position));

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
