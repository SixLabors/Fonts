// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.General
{
    [TableName(TableName)]
    internal class CpalTable : Table
    {
        private const string TableName = TableNames.Cpal;
        private readonly ushort[] palletteOffsets;
        private readonly GlyphColor[] palletteEntries;

        public CpalTable(ushort[] palletteOffsets, GlyphColor[] palletteEntries)
        {
            this.palletteEntries = palletteEntries;
            this.palletteOffsets = palletteOffsets;
        }

        public GlyphColor GetGlyphColor(int palletteIndex, int palletteEntryIndex)
            => this.palletteEntries[this.palletteOffsets[palletteIndex] + palletteEntryIndex];

        public static CpalTable? Load(FontReader fontReader)
        {
            if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
            {
                return null;
            }

            using (binaryReader)
            {
                return Load(binaryReader!);
            }
        }

        public static CpalTable Load(BigEndianBinaryReader reader)
        {
            // FORMAT 0

            // Type      | Name                            | Description
            // ----------|---------------------------------|----------------------------------------------------------------------------------------------------
            // uint16    | version                         | Table version number (=0).
            // uint16    | numPaletteEntries               | Number of palette entries in each palette.
            // uint16    | numPalettes                     | Number of palettes in the table.
            // uint16    | numColorRecords                 | Total number of color records, combined for all palettes.
            // Offset32  | offsetFirstColorRecord          | Offset from the beginning of CPAL table to the first ColorRecord.
            // uint16    | colorRecordIndices[numPalettes] | Index of each palette’s first color record in the combined color record array.

            // additional format 1 fields
            // Offset32  | offsetPaletteTypeArray          | Offset from the beginning of CPAL table to the Palette Type Array. Set to 0 if no array is provided.
            // Offset32  | offsetPaletteLabelArray         | Offset from the beginning of CPAL table to the Palette Labels Array. Set to 0 if no array is provided.
            // Offset32  | offsetPaletteEntryLabelArray    | Offset from the beginning of CPAL table to the Palette Entry Label Array.Set to 0 if no array is provided.
            ushort version = reader.ReadUInt16();
            ushort numPaletteEntries = reader.ReadUInt16();
            ushort numPalettes = reader.ReadUInt16();
            ushort numColorRecords = reader.ReadUInt16();
            uint offsetFirstColorRecord = reader.ReadOffset32();

            ushort[]? colorRecordIndices = reader.ReadUInt16Array(numPalettes);

            uint offsetPaletteTypeArray = 0;
            uint offsetPaletteLabelArray = 0;
            uint offsetPaletteEntryLabelArray = 0;
            if (version == 1)
            {
                offsetPaletteTypeArray = reader.ReadOffset32();
                offsetPaletteLabelArray = reader.ReadOffset32();
                offsetPaletteEntryLabelArray = reader.ReadOffset32();
            }

            reader.Seek(offsetFirstColorRecord, System.IO.SeekOrigin.Begin);
            var pallettes = new GlyphColor[numColorRecords];
            for (int n = 0; n < numColorRecords; n++)
            {
                byte blue = reader.ReadByte();
                byte green = reader.ReadByte();
                byte red = reader.ReadByte();
                byte alpha = reader.ReadByte();
                pallettes[n] = new GlyphColor(blue, green, red, alpha);
            }

            return new CpalTable(colorRecordIndices, pallettes);
        }
    }
}
