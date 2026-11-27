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

        ushort xPlaDevOff = (valueFormat & ValueFormat.XPlacementDevice) != 0 ? reader.ReadOffset16() : (ushort)0;
        ushort yPlaDevOff = (valueFormat & ValueFormat.YPlacementDevice) != 0 ? reader.ReadOffset16() : (ushort)0;
        ushort xAdvDevOff = (valueFormat & ValueFormat.XAdvanceDevice) != 0 ? reader.ReadOffset16() : (ushort)0;
        ushort yAdvDevOff = (valueFormat & ValueFormat.YAdvanceDevice) != 0 ? reader.ReadOffset16() : (ushort)0;

        // Resolve device offsets to VariationIndex tables when the parent base is known.
        if (parentBase >= 0 && (xPlaDevOff | yPlaDevOff | xAdvDevOff | yAdvDevOff) != 0)
        {
            long savedPosition = reader.BaseStream.Position;
            this.XPlacementVariation = AdvancedTypographicUtils.ReadVariationIndex(reader, parentBase, xPlaDevOff);
            this.YPlacementVariation = AdvancedTypographicUtils.ReadVariationIndex(reader, parentBase, yPlaDevOff);
            this.XAdvanceVariation = AdvancedTypographicUtils.ReadVariationIndex(reader, parentBase, xAdvDevOff);
            this.YAdvanceVariation = AdvancedTypographicUtils.ReadVariationIndex(reader, parentBase, yAdvDevOff);
            reader.BaseStream.Position = savedPosition;
        }
    }

    /// <summary>
    /// Gets the horizontal adjustment for placement, in design units.
    /// </summary>
    public short XPlacement { get; }

    /// <summary>
    /// Gets the vertical adjustment for placement, in design units.
    /// </summary>
    public short YPlacement { get; }

    /// <summary>
    /// Gets the horizontal adjustment for advance, in design units.
    /// </summary>
    public short XAdvance { get; }

    /// <summary>
    /// Gets the vertical adjustment for advance, in design units.
    /// </summary>
    public short YAdvance { get; }

    /// <summary>
    /// Gets the one-based packed VariationIndex for horizontal placement.
    /// Zero means no variation data.
    /// </summary>
    public uint XPlacementVariation { get; }

    /// <summary>
    /// Gets the one-based packed VariationIndex for vertical placement.
    /// Zero means no variation data.
    /// </summary>
    public uint YPlacementVariation { get; }

    /// <summary>
    /// Gets the one-based packed VariationIndex for horizontal advance.
    /// Zero means no variation data.
    /// </summary>
    public uint XAdvanceVariation { get; }

    /// <summary>
    /// Gets the one-based packed VariationIndex for vertical advance.
    /// Zero means no variation data.
    /// </summary>
    public uint YAdvanceVariation { get; }

    /// <summary>
    /// Gets a value indicating whether this record has any variation data.
    /// </summary>
    public bool HasVariation
        => (this.XPlacementVariation | this.YPlacementVariation | this.XAdvanceVariation | this.YAdvanceVariation) != 0;
}
