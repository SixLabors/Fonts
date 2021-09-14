// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
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
            this.XPlacement = ((valueFormat & ValueFormat.XPlacement) != 0) ? reader.ReadInt16() : (short)0;
            this.YPlacement = ((valueFormat & ValueFormat.YPlacement) != 0) ? reader.ReadInt16() : (short)0;
            this.XAdvance = ((valueFormat & ValueFormat.XAdvance) != 0) ? reader.ReadInt16() : (short)0;
            this.YAdvance = ((valueFormat & ValueFormat.YAdvance) != 0) ? reader.ReadInt16() : (short)0;
            this.XPlacementDeviceOffset = ((valueFormat & ValueFormat.XPlacementDevice) != 0) ? reader.ReadInt16() : (short)0;
            this.YPlacementDeviceOffset = ((valueFormat & ValueFormat.YPlacementDevice) != 0) ? reader.ReadInt16() : (short)0;
            this.XAdvanceDeviceOffset = ((valueFormat & ValueFormat.XAdvanceDevice) != 0) ? reader.ReadInt16() : (short)0;
            this.YAdvanceDeviceOffset = ((valueFormat & ValueFormat.YAdvanceDevice) != 0) ? reader.ReadInt16() : (short)0;
        }

        public short XPlacement { get; }

        public short YPlacement { get; }

        public short XAdvance { get; }

        public short YAdvance { get; }

        public short XPlacementDeviceOffset { get; }

        public short YPlacementDeviceOffset { get; }

        public short XAdvanceDeviceOffset { get; }

        public short YAdvanceDeviceOffset { get; }
    }
}
