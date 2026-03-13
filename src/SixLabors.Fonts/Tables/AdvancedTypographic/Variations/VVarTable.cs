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
    /// <summary>
    /// The table name identifier for the VVAR table.
    /// </summary>
    internal const string TableName = "VVAR";

    /// <summary>
    /// Initializes a new instance of the <see cref="VVarTable"/> class.
    /// </summary>
    /// <param name="itemVariationStore">The item variation store containing delta data.</param>
    /// <param name="advanceWidthMapping">The optional delta-set index mapping for advance heights.</param>
    /// <param name="tsbMapping">The optional delta-set index mapping for top side bearings.</param>
    /// <param name="bsbMapping">The optional delta-set index mapping for bottom side bearings.</param>
    /// <param name="vOrgMapping">The optional delta-set index mapping for vertical origin Y coordinates.</param>
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

    /// <summary>
    /// Gets the item variation store containing the variation delta data.
    /// </summary>
    public ItemVariationStore ItemVariationStore { get; }

    /// <summary>
    /// Gets the optional delta-set index mapping for advance heights.
    /// </summary>
    public DeltaSetIndexMap[]? AdvanceWidthMapping { get; }

    /// <summary>
    /// Gets the optional delta-set index mapping for top side bearings.
    /// </summary>
    public DeltaSetIndexMap[]? TsbMapping { get; }

    /// <summary>
    /// Gets the optional delta-set index mapping for bottom side bearings.
    /// </summary>
    public DeltaSetIndexMap[]? BsbMapping { get; }

    /// <summary>
    /// Gets the optional delta-set index mapping for Y coordinates of vertical origins.
    /// </summary>
    public DeltaSetIndexMap[]? VOrgMapping { get; }

    /// <summary>
    /// Loads the VVAR table from the specified font reader.
    /// </summary>
    /// <param name="reader">The font reader.</param>
    /// <returns>The <see cref="VVarTable"/>, or <see langword="null"/> if the table is not present.</returns>
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

    /// <summary>
    /// Loads the VVAR table from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader positioned at the start of the VVAR table.</param>
    /// <returns>The <see cref="VVarTable"/>.</returns>
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
