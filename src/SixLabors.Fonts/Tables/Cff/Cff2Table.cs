// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.Globalization;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;
using SixLabors.Fonts.Tables.General.Name;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.Cff
{
    internal sealed class Cff2Table : Table, ICffTable
    {
        internal const string TableName = "CFF2";

        private readonly CffGlyphData[] glyphs;

        public Cff2Table(CffFont cffFont, ItemVariationStore itemVariationStore)
        {
            this.glyphs = cffFont.Glyphs;
            this.ItemVariationStore = itemVariationStore;
        }

        public int GlyphCount => this.glyphs.Length;

        public ItemVariationStore ItemVariationStore { get; }

        public CffGlyphData GetGlyph(int index)
            => this.glyphs[index];

        public static Cff2Table? Load(FontReader fontReader)
        {
            if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
            {
                return null;
            }

            NameTable nameTable = fontReader.GetTable<NameTable>();
            string fontName = nameTable.GetNameById(CultureInfo.InvariantCulture, KnownNameIds.PostscriptName);

            using (binaryReader)
            {
                return Load(binaryReader, fontName);
            }
        }

        public static Cff2Table Load(BigEndianBinaryReader reader, string fontName)
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
                    Cff2Font cffFont = parser.Load(reader, hdrSize, topDictLength, fontName, position);
                    return new(cffFont, cffFont.ItemVariationStore);

                default:
                    throw new NotSupportedException("CFF version 2 is expected");
            }
        }
    }
}