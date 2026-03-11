// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos;

/// <summary>
/// GPOS subtables use ValueRecords to describe all the variables and values used to adjust the position
/// of a glyph or set of glyphs. A ValueRecord may define any combination of X and Y values (in design units)
/// to add to (positive values) or subtract from (negative values) the placement and advance values provided in the font.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#value-record"/>
/// </summary>
internal readonly struct ValueRecord
{
    /// <summary>
    /// The deltaFormat value used by VariationIndex tables (as opposed to Device tables).
    /// </summary>
    private const ushort VariationIndexFormat = 0x8000;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueRecord"/> struct.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="valueFormat">Defines the types of data in the ValueRecord.</param>
    public ValueRecord(BigEndianBinaryReader reader, ValueFormat valueFormat)
        : this(reader, valueFormat, -1)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueRecord"/> struct.
    /// When <paramref name="parentBase"/> is non-negative, device offsets are resolved to
    /// VariationIndex (outerIndex, innerIndex) pairs for use with variable fonts.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="valueFormat">Defines the types of data in the ValueRecord.</param>
    /// <param name="parentBase">
    /// The absolute stream position of the immediate parent table (SinglePos subtable,
    /// PairPosFormat2 subtable, or PairSet table). Device offsets are relative to this position.
    /// Pass -1 to skip VariationIndex resolution.
    /// </param>
    public ValueRecord(BigEndianBinaryReader reader, ValueFormat valueFormat, long parentBase)
    {
        // +----------+------------------+--------------------------------------------------------------------------------------+
        // | Type     | Name             | Description                                                                          |
        // +==========+==================+======================================================================================+
        // | int16    | xPlacement       | Horizontal adjustment for placement, in                                              |
        // |          |                  | design units.                                                                        |
        // +----------+------------------+--------------------------------------------------------------------------------------+
        // | int16    | yPlacement       | Vertical adjustment for placement, in design                                         |
        // |          |                  | units.                                                                               |
        // +----------+------------------+--------------------------------------------------------------------------------------+
        // | int16    | xAdvance         | Horizontal adjustment for advance, in design                                         |
        // |          |                  | units — only used for horizontal layout.                                             |
        // +----------+------------------+--------------------------------------------------------------------------------------+
        // | int16    | yAdvance         | Vertical adjustment for advance, in design                                           |
        // |          |                  | units — only used for vertical layout.                                               |
        // +----------+------------------+--------------------------------------------------------------------------------------+
        // | Offset16 | xPlaDeviceOffset | Offset to Device table (non-variable font) /                                         |
        // |          |                  | VariationIndex table (variable font) for                                             |
        // |          |                  | horizontal placement, from beginning of the                                          |
        // |          |                  | immediate parent table (SinglePos or                                                 |
        // |          |                  | PairPosFormat2 lookup subtable, PairSet table                                        |
        // |          |                  | within a PairPosFormat1 lookup subtable) — may be NULL.                              |
        // +----------+------------------+--------------------------------------------------------------------------------------+
        // | Offset16 | yPlaDeviceOffset | Offset to Device table (non-variable font) /                                         |
        // |          |                  | VariationIndex table (variable font) for vertical                                    |
        // |          |                  | placement, from beginning of the immediate parent table (SinglePos or PairPosFormat2 |
        // |          |                  | lookup subtable, PairSet table within a                                              |
        // |          |                  | PairPosFormat1 lookup subtable) — may be NULL.                                       |
        // +----------+------------------+--------------------------------------------------------------------------------------+
        // | Offset16 | xAdvDeviceOffset | Offset to Device table (non-variable font) /                                         |
        // |          |                  | VariationIndex table (variable font) for                                             |
        // |          |                  | horizontal advance, from beginning of the                                            |
        // |          |                  | immediate parent table (SinglePos or                                                 |
        // |          |                  | PairPosFormat2 lookup subtable, PairSet table                                        |
        // |          |                  | within a PairPosFormat1 lookup subtable) — may be NULL.                              |
        // +----------+------------------+--------------------------------------------------------------------------------------+
        // | Offset16 | yAdvDeviceOffset | Offset to Device table (non-variable font) /                                         |
        // |          |                  | VariationIndex table (variable font) for vertical                                    |
        // |          |                  | advance, from beginning of the immediate                                             |
        // |          |                  | parent table (SinglePos or PairPosFormat2                                            |
        // |          |                  | lookup subtable, PairSet table within a                                              |
        // |          |                  | PairPosFormat1 lookup subtable) — may be NULL.                                       |
        // +----------+------------------+--------------------------------------------------------------------------------------+
        this.XPlacement = (valueFormat & ValueFormat.XPlacement) != 0 ? reader.ReadInt16() : (short)0;
        this.YPlacement = (valueFormat & ValueFormat.YPlacement) != 0 ? reader.ReadInt16() : (short)0;
        this.XAdvance = (valueFormat & ValueFormat.XAdvance) != 0 ? reader.ReadInt16() : (short)0;
        this.YAdvance = (valueFormat & ValueFormat.YAdvance) != 0 ? reader.ReadInt16() : (short)0;

        short xPlaDevOff = (valueFormat & ValueFormat.XPlacementDevice) != 0 ? reader.ReadInt16() : (short)0;
        short yPlaDevOff = (valueFormat & ValueFormat.YPlacementDevice) != 0 ? reader.ReadInt16() : (short)0;
        short xAdvDevOff = (valueFormat & ValueFormat.XAdvanceDevice) != 0 ? reader.ReadInt16() : (short)0;
        short yAdvDevOff = (valueFormat & ValueFormat.YAdvanceDevice) != 0 ? reader.ReadInt16() : (short)0;

        // Resolve device offsets to VariationIndex tables when the parent base is known.
        if (parentBase >= 0 && ((ushort)xPlaDevOff | (ushort)yPlaDevOff | (ushort)xAdvDevOff | (ushort)yAdvDevOff) != 0)
        {
            long savedPosition = reader.BaseStream.Position;
            this.XPlacementVariation = ResolveVariationIndex(reader, parentBase, xPlaDevOff);
            this.YPlacementVariation = ResolveVariationIndex(reader, parentBase, yPlaDevOff);
            this.XAdvanceVariation = ResolveVariationIndex(reader, parentBase, xAdvDevOff);
            this.YAdvanceVariation = ResolveVariationIndex(reader, parentBase, yAdvDevOff);
            reader.BaseStream.Position = savedPosition;
        }
    }

    public short XPlacement { get; }

    public short YPlacement { get; }

    public short XAdvance { get; }

    public short YAdvance { get; }

    /// <summary>
    /// Gets the packed VariationIndex for horizontal placement: (outerIndex &lt;&lt; 16) | innerIndex.
    /// Zero means no variation data.
    /// </summary>
    public uint XPlacementVariation { get; }

    /// <summary>
    /// Gets the packed VariationIndex for vertical placement: (outerIndex &lt;&lt; 16) | innerIndex.
    /// Zero means no variation data.
    /// </summary>
    public uint YPlacementVariation { get; }

    /// <summary>
    /// Gets the packed VariationIndex for horizontal advance: (outerIndex &lt;&lt; 16) | innerIndex.
    /// Zero means no variation data.
    /// </summary>
    public uint XAdvanceVariation { get; }

    /// <summary>
    /// Gets the packed VariationIndex for vertical advance: (outerIndex &lt;&lt; 16) | innerIndex.
    /// Zero means no variation data.
    /// </summary>
    public uint YAdvanceVariation { get; }

    /// <summary>
    /// Gets a value indicating whether this record has any variation data.
    /// </summary>
    public bool HasVariation
        => (this.XPlacementVariation | this.YPlacementVariation | this.XAdvanceVariation | this.YAdvanceVariation) != 0;

    /// <summary>
    /// Reads a Device/VariationIndex table at the given offset and returns a packed VariationIndex
    /// (outerIndex &lt;&lt; 16 | innerIndex) if it is a VariationIndex table (deltaFormat == 0x8000),
    /// or 0 if null, a Device table, or invalid.
    /// </summary>
    private static uint ResolveVariationIndex(BigEndianBinaryReader reader, long parentBase, short deviceOffset)
    {
        if (deviceOffset == 0)
        {
            return 0;
        }

        // Device offsets are relative to the parent table base.
        // Use absolute positioning to avoid BigEndianBinaryReader.Seek startOfStream rebasing.
        reader.BaseStream.Position = parentBase + (ushort)deviceOffset;

        // VariationIndex table (reuses the Device table format):
        // uint16 deltaSetOuterIndex
        // uint16 deltaSetInnerIndex
        // uint16 deltaFormat  (0x8000 for VariationIndex, 1/2/3 for Device)
        ushort first = reader.ReadUInt16();
        ushort second = reader.ReadUInt16();
        ushort format = reader.ReadUInt16();

        if (format == VariationIndexFormat)
        {
            return ((uint)first << 16) | second;
        }

        // TODO: Device table (per-ppem pixel adjustments for non-variable fonts) — not yet implemented.
        return 0;
    }
}
