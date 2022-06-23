// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Tables.Cff
{
    [TableName(TableName)]
    internal sealed class Cff2Table : Table, ICffTable
    {
        internal const string TableName = "CFF2";

        private readonly CffGlyphData[] glyphs;

        public Cff2Table(CffFont cff1Font) => this.glyphs = cff1Font.Glyphs;

        public int GlyphCount => this.glyphs.Length;

        public CffGlyphData GetGlyph(int index)
            => this.glyphs[index];

        public static Cff2Table? Load(FontReader fontReader)
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

        public static Cff2Table Load(BigEndianBinaryReader reader)
        {
            long position = reader.BaseStream.Position;
            byte major = reader.ReadUInt8();
            byte minor = reader.ReadUInt8();
            byte hdrSize = reader.ReadUInt8();
            ushort topDictLength = reader.ReadUInt16();

            switch (major)
            {
                case 2:
                    Cff2Parser parser = new();
                    parser.Load(reader, hdrSize, topDictLength, position);
                    return new(parser.Load(reader, hdrSize, topDictLength, position));

                default:
                    throw new NotSupportedException("CFF version 2 is expected");
            }
        }
    }
}
