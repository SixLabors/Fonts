// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// Represents a single axis value mapping record used in the avar table
/// to remap a normalized coordinate value to a modified value.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"/>
/// </summary>
internal class AxisValueMapRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AxisValueMapRecord"/> class.
    /// </summary>
    /// <param name="fromCoordinate">The normalized coordinate value obtained using default normalization.</param>
    /// <param name="toCoordinate">The modified, normalized coordinate value.</param>
    public AxisValueMapRecord(float fromCoordinate, float toCoordinate)
    {
        this.FromCoordinate = fromCoordinate;
        this.ToCoordinate = toCoordinate;
    }

    /// <summary>
    /// Gets the normalized coordinate value obtained using default normalization.
    /// </summary>
    public float FromCoordinate { get; }

    /// <summary>
    /// Gets the modified, normalized coordinate value.
    /// </summary>
    public float ToCoordinate { get; }

    /// <summary>
    /// Loads an <see cref="AxisValueMapRecord"/> from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <returns>The <see cref="AxisValueMapRecord"/>.</returns>
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
