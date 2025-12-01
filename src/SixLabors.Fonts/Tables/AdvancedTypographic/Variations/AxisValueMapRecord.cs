// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

internal class AxisValueMapRecord
{
    public AxisValueMapRecord(float fromCoordinate, float toCoordinate)
    {
        this.FromCoordinate = fromCoordinate;
        this.ToCoordinate = toCoordinate;
    }

    public float FromCoordinate { get; }

    public float ToCoordinate { get; }

    public static AxisValueMapRecord Load(BigEndianBinaryReader reader)
    {
        // AxisValueMapRecord
        // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
        // | Type            | Name                                   | Description                                                             |
        // +=================+========================================+=========================================================================+
        // | F2DOT14         | fromCoordinate                         | A normalized coordinate value obtained using default normalization.     |
        // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
        // | F2DOT14         | toCoordinate                           | The modified, normalized coordinate value.                              |
        // +-----------------+----------------------------------------+-------------------------------------------------------------------------+
        float fromCoordinate = reader.ReadF2Dot14();
        float toCoordinate = reader.ReadF2Dot14();

        return new AxisValueMapRecord(fromCoordinate, toCoordinate);
    }
}
