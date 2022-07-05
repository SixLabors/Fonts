// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations
{
    /// <summary>
    /// Implements reading the font variations table `hvar`.
    /// <see href="https://docs.microsoft.com/de-de/typography/opentype/spec/hvar"/>
    /// </summary>
    internal class HVarTable : Table
    {
        internal const string TableName = "hvar";

        public HVarTable(ItemVariationStore itemVariationStore) => this.ItemVariationStore = itemVariationStore;

        public ItemVariationStore ItemVariationStore { get; }

        public static HVarTable? Load(FontReader reader)
        {
            if (!reader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
            {
                return null;
            }

            using (binaryReader)
            {
                return Load(binaryReader);
            }
        }

        public static HVarTable Load(BigEndianBinaryReader reader)
        {
            // Horizontal metrics variations table
            // +--------------------------+----------------------------------------+-------------------------------------------------------------------------+
            // | Type                     | Name                                   | Description                                                             |
            // +==========================+========================================+=========================================================================+
            // | uint16                   | majorVersion                           | Major version number of the font variations table — set to 1.           |
            // +--------------------------+----------------------------------------+-------------------------------------------------------------------------+
            // | uint16                   | minorVersion                           | Minor version number of the font variations table — set to 0.           |
            // +--------------------------+----------------------------------------+-------------------------------------------------------------------------+
            // | Offset32                 | itemVariationStoreOffset               | Offset in bytes from the start of this table to the                     |
            // |                          |                                        | item variation store table.                                             |
            // +--------------------------+----------------------------------------+-------------------------------------------------------------------------+
            // | Offset32                 | advanceWidthMappingOffset              | Offset in bytes from the start of this table to the delta-set index     |
            // |                          |                                        | mapping for advance widths (may be NULL).                               |
            // +--------------------------+----------------------------------------+-------------------------------------------------------------------------+
            // | Offset32                 | lsbMappingOffset                       | Offset in bytes from the start of this table to the delta-set index     |
            // |                          |                                        | mapping for left side bearings (may be NULL).                           |
            // +--------------------------+----------------------------------------+-------------------------------------------------------------------------+
            // | Offset32                 | rsbMappingOffset                       | Offset in bytes from the start of this table to the delta-set index     |
            // |                          |                                        | mapping for right side bearings (may be NULL).                          |
            // +--------------------------+----------------------------------------+-------------------------------------------------------------------------+
            ushort major = reader.ReadUInt16();
            ushort minor = reader.ReadUInt16();
            uint itemVariationStoreOffset = reader.ReadOffset32();
            uint advanceWidthMappingOffset = reader.ReadOffset32();
            uint lsbMappingOffset = reader.ReadOffset32();
            uint rsbMappingOffset = reader.ReadOffset32();

            if (major != 1)
            {
                throw new NotSupportedException("Only version 1 of hvar table is supported");
            }

            var itemVariationStore = ItemVariationStore.Load(reader, itemVariationStoreOffset);

            var advanceWidthMapping = DeltaSetIndexMap.Load(reader, advanceWidthMappingOffset);
            var lsbMapping = DeltaSetIndexMap.Load(reader, lsbMappingOffset);
            var rsbMapping = DeltaSetIndexMap.Load(reader, rsbMappingOffset);

            return new HVarTable(itemVariationStore);
        }
    }
}
