// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations
{
    internal class ItemVariationStore
    {
        private static readonly ItemVariationStore EmptyItemVariationStoreTable = new(VariationRegionList.EmptyVariationRegionList, Array.Empty<ItemVariationData>());

        public ItemVariationStore(VariationRegionList variationRegionList, ItemVariationData[] itemVariations)
        {
            this.VariationRegionList = variationRegionList;
            this.ItemVariations = itemVariations;
        }

        public VariationRegionList VariationRegionList { get; }

        public ItemVariationData[] ItemVariations { get; }

        public static ItemVariationStore Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);

            // Length in bytes of the Item Variation Store structure that follows.
            ushort length = reader.ReadUInt16();
            if (length == 0)
            {
                return EmptyItemVariationStoreTable;
            }

            long variationDataStoreStart = offset + 2;

            ushort format = reader.ReadUInt16();
            if (format != 1)
            {
                throw new InvalidFontFileException($"Invalid value for variation Store Format {format}. Should be '1'.");
            }

            uint variationRegionListOffset = reader.ReadUInt32();
            ushort itemVariationDataCount = reader.ReadUInt16();

            if (variationRegionListOffset > length)
            {
                throw new InvalidFontFileException("Invalid variation region list offset");
            }

            var itemVariations = new ItemVariationData[itemVariationDataCount];
            for (int i = 0; i < itemVariationDataCount; i++)
            {
                uint variationDataOffset = reader.ReadOffset32();
                if (offset >= length)
                {
                    throw new InvalidFontFileException("Bad offset to variation data subtable");
                }

                var itemVariationData = ItemVariationData.Load(reader, variationDataStoreStart + variationDataOffset);
                itemVariations[i] = itemVariationData;
            }

            var variationRegionList = VariationRegionList.Load(reader, variationDataStoreStart + variationRegionListOffset);

            // Make sure we point to the stream to the end of the variation store data.
            reader.Seek(offset + length, SeekOrigin.Begin);

            return new ItemVariationStore(variationRegionList, itemVariations);
        }
    }
}
