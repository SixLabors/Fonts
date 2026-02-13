// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// Implements reading the font variations table `VVAR`.
/// The VVAR table is used in variable fonts to provide variations for vertical glyph metrics values.
/// This can be used to provide variation data for advance heights in the 'vmtx' table.
/// <see href="https://docs.microsoft.com/en-gb/typography/opentype/spec/vvar"/>
/// </summary>
internal class VVarTable : Table
{
    internal const string TableName = "VVAR";

    public VVarTable(
        ItemVariationStore itemVariationStore,
        DeltaSetIndexMap[]? advanceWidthMapping,
        DeltaSetIndexMap[]? tsbMapping,
        DeltaSetIndexMap[]? bsbMapping,
        DeltaSetIndexMap[]? vOrgMapping)
    {
        this.ItemVariationStore = itemVariationStore;
        this.AdvanceWidthMapping = advanceWidthMapping;
        this.TsbMapping = tsbMapping;
        this.BsbMapping = bsbMapping;
        this.VOrgMapping = vOrgMapping;
    }

    public ItemVariationStore ItemVariationStore { get; }

    public DeltaSetIndexMap[]? AdvanceWidthMapping { get; }

    public DeltaSetIndexMap[]? TsbMapping { get; }

    public DeltaSetIndexMap[]? BsbMapping { get; }

    public DeltaSetIndexMap[]? VOrgMapping { get; }

    public static VVarTable? Load(FontReader reader)
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

    public static VVarTable Load(BigEndianBinaryReader reader)
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
        // | Offset32                 | advanceHeightMappingOffset             | Offset in bytes from the start of this table to the delta-set index     |
        // |                          |                                        | mapping for advance heights (may be NULL).                              |
        // +--------------------------+----------------------------------------+-------------------------------------------------------------------------+
        // | Offset32                 | tsbMappingOffset                       | Offset in bytes from the start of this table to the delta-set index     |
        // |                          |                                        | mapping for top side bearings (may be NULL).                            |
        // +--------------------------+----------------------------------------+-------------------------------------------------------------------------+
        // | Offset32                 | bsbMappingOffset                       | Offset in bytes from the start of this table to the delta-set index     |
        // |                          |                                        | mapping for bottom side bearings (may be NULL).                         |
        // +--------------------------+----------------------------------------+-------------------------------------------------------------------------+
        // | Offset32                 | vOrgMappingOffset                      | Offset in bytes from the start of this table to the delta-set index     |
        // |                          |                                        | mapping for Y coordinates of vertical origins (may be NULL).            |
        // +--------------------------+----------------------------------------+-------------------------------------------------------------------------+
        ushort major = reader.ReadUInt16();
        ushort minor = reader.ReadUInt16();
        uint itemVariationStoreOffset = reader.ReadOffset32();
        uint advanceHeightMappingOffset = reader.ReadOffset32();
        uint tsbMappingOffset = reader.ReadOffset32();
        uint bsbMappingOffset = reader.ReadOffset32();
        uint vOrgMappingOffset = reader.ReadOffset32();

        if (major != 1)
        {
            throw new NotSupportedException("Only version 1 of hvar table is supported");
        }

        ItemVariationStore itemVariationStore = ItemVariationStore.Load(reader, itemVariationStoreOffset);

        DeltaSetIndexMap[]? advanceHeightMapping = DeltaSetIndexMap.Load(reader, advanceHeightMappingOffset);
        DeltaSetIndexMap[]? tsbMapping = DeltaSetIndexMap.Load(reader, tsbMappingOffset);
        DeltaSetIndexMap[]? bsbMapping = DeltaSetIndexMap.Load(reader, bsbMappingOffset);
        DeltaSetIndexMap[]? vOrgMapping = DeltaSetIndexMap.Load(reader, vOrgMappingOffset);

        return new VVarTable(itemVariationStore, advanceHeightMapping, tsbMapping, bsbMapping, vOrgMapping);
    }
}
