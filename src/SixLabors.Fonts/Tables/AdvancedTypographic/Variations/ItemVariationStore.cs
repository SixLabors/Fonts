// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations
{
    /// <summary>
    /// Implements reading the item variation store, which is used in most glyph variation data.
    /// <see href="https://docs.microsoft.com/de-de/typography/opentype/spec/otvarcommonformats#item-variation-store"/>
    /// </summary>
    internal class ItemVariationStore
    {
        public ItemVariationStore(VariationRegionList variationRegionList, ItemVariationData[] itemVariations)
        {
            this.VariationRegionList = variationRegionList;
            this.ItemVariations = itemVariations;
        }

        public VariationRegionList VariationRegionList { get; }

        public ItemVariationData[] ItemVariations { get; }

        public static ItemVariationStore Load(BigEndianBinaryReader reader, long offset, long? length = null)
        {
            // ItemVariationStore
            // +--------------------------+--------------------------------------------------+-------------------------------------------------------------------------+
            // | Type                     | Name                                             | Description                                                             |
            // +==========================+==================================================+=========================================================================+
            // | uint16                   | format                                           | Format — set to 1                                                       |
            // +--------------------------+--------------------------------------------------+-------------------------------------------------------------------------+
            // | Offset32                 | variationRegionListOffset                        | Offset in bytes from the start of the item variation store              |
            // |                          |                                                  | to the variation region list.                                           |
            // +--------------------------+--------------------------------------------------+-------------------------------------------------------------------------+
            // | uint16                   | itemVariationDataCount                           | The number of item variation data subtables.                            |
            // +--------------------------+--------------------------------------------------+-------------------------------------------------------------------------+
            // | Offset32                 | itemVariationDataOffsets[itemVariationDataCount] | Offsets in bytes from the start of the item variation store             |
            // |                          |                                                  | to each item variation data subtable.                                   |
            // +--------------------------+--------------------------------------------------+-------------------------------------------------------------------------+
            reader.Seek(offset, SeekOrigin.Begin);

            ushort format = reader.ReadUInt16();
            if (format != 1)
            {
                throw new InvalidFontFileException($"Invalid value for variation Store Format {format}. Should be '1'.");
            }

            uint variationRegionListOffset = reader.ReadOffset32();
            ushort itemVariationDataCount = reader.ReadUInt16();

            if (length.HasValue && variationRegionListOffset > length)
            {
                throw new InvalidFontFileException("Invalid variation region list offset");
            }

            var itemVariations = new ItemVariationData[itemVariationDataCount];
            long itemVariationsOffset = reader.BaseStream.Position;
            for (int i = 0; i < itemVariationDataCount; i++)
            {
                uint variationDataOffset = reader.ReadOffset32();
                itemVariationsOffset += 4;
                if (length.HasValue && offset + variationDataOffset >= length)
                {
                    throw new InvalidFontFileException("Bad offset to variation data subtable");
                }

                var itemVariationData = ItemVariationData.Load(reader, offset + variationDataOffset);
                itemVariations[i] = itemVariationData;

                reader.BaseStream.Position = itemVariationsOffset;
            }

            var variationRegionList = VariationRegionList.Load(reader, offset + variationRegionListOffset);

            return new ItemVariationStore(variationRegionList, itemVariations);
        }
    }
}
