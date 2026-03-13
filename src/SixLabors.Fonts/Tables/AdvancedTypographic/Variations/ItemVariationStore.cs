// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// Implements reading the item variation store, which is used in most glyph variation data.
/// <see href="https://docs.microsoft.com/de-de/typography/opentype/spec/otvarcommonformats#item-variation-store"/>
/// </summary>
internal class ItemVariationStore
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemVariationStore"/> class.
    /// </summary>
    /// <param name="variationRegionList">The variation region list defining regions in the design space.</param>
    /// <param name="itemVariations">The array of item variation data subtables.</param>
    public ItemVariationStore(VariationRegionList variationRegionList, ItemVariationData[] itemVariations)
    {
        this.VariationRegionList = variationRegionList;
        this.ItemVariations = itemVariations;
    }

    /// <summary>
    /// Gets the variation region list defining regions in the font's variation space.
    /// </summary>
    public VariationRegionList VariationRegionList { get; }

    /// <summary>
    /// Gets the array of item variation data subtables.
    /// </summary>
    public ItemVariationData[] ItemVariations { get; }

    /// <summary>
    /// Loads the item variation store from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="offset">The byte offset from the start of the stream to this store.</param>
    /// <param name="length">The optional total length of the parent table for bounds validation.</param>
    /// <returns>The <see cref="ItemVariationStore"/>.</returns>
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

        ItemVariationData[] itemVariations = new ItemVariationData[itemVariationDataCount];
        long itemVariationsOffset = reader.BaseStream.Position;
        for (int i = 0; i < itemVariationDataCount; i++)
        {
            uint variationDataOffset = reader.ReadOffset32();
            itemVariationsOffset += 4;
            if (length.HasValue && offset + variationDataOffset >= length)
            {
                throw new InvalidFontFileException("Bad offset to variation data subtable");
            }

            itemVariations[i] = ItemVariationData.Load(reader, offset + variationDataOffset);

            reader.BaseStream.Position = itemVariationsOffset;
        }

        VariationRegionList variationRegionList = VariationRegionList.Load(reader, offset + variationRegionListOffset);

        return new ItemVariationStore(variationRegionList, itemVariations);
    }
}
