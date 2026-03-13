// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;
using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// Defines a VariationAxisRecord.
/// <see href="https://docs.microsoft.com/de-de/typography/opentype/spec/fvar#variationaxisrecord"/>
/// </summary>
[DebuggerDisplay("Tag: {Tag}, MinValue: {MinValue}, MaxValue: {MaxValue}, DefaultValue: {DefaultValue}, AxisNameId: {AxisNameId}")]
internal class VariationAxisRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VariationAxisRecord"/> class.
    /// </summary>
    /// <param name="tag">The tag identifying the design variation for this axis.</param>
    /// <param name="minValue">The minimum coordinate value for this axis.</param>
    /// <param name="defaultValue">The default coordinate value for this axis.</param>
    /// <param name="maxValue">The maximum coordinate value for this axis.</param>
    /// <param name="flags">The axis qualifier flags.</param>
    /// <param name="axisNameId">The name ID for the display name of this axis.</param>
    internal VariationAxisRecord(string tag, float minValue, float defaultValue, float maxValue, ushort flags, ushort axisNameId)
    {
        this.Tag = tag;
        this.MinValue = minValue;
        this.MaxValue = maxValue;
        this.DefaultValue = defaultValue;
        this.Flags = flags;
        this.AxisNameId = axisNameId;
    }

    /// <summary>
    /// Gets the tag identifying the design variation for this axis (e.g. "wght", "wdth").
    /// </summary>
    public string Tag { get; }

    /// <summary>
    /// Gets the minimum coordinate value for this axis.
    /// </summary>
    public float MinValue { get; }

    /// <summary>
    /// Gets the default coordinate value for this axis.
    /// </summary>
    public float DefaultValue { get; }

    /// <summary>
    /// Gets the maximum coordinate value for this axis.
    /// </summary>
    public float MaxValue { get; }

    /// <summary>
    /// Gets the axis qualifier flags.
    /// </summary>
    public ushort Flags { get; }

    /// <summary>
    /// Gets the name ID for entries in the 'name' table that provide a display name for this axis.
    /// </summary>
    public ushort AxisNameId { get; }

    /// <summary>
    /// Loads a <see cref="VariationAxisRecord"/> from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="offset">The byte offset from the start of the stream to this axis record.</param>
    /// <returns>The <see cref="VariationAxisRecord"/>.</returns>
    public static VariationAxisRecord Load(BigEndianBinaryReader reader, long offset)
    {
        // VariationAxisRecord
        // +-----------------+----------------------------------------+----------------------------------------------------------------+
        // | Type            | Name                                   | Description                                                    |
        // +=================+========================================+================================================================+
        // | Tag             | axisTag                                | Tag identifying the design variation for the axis.             |
        // +-----------------+----------------------------------------+----------------------------------------------------------------+
        // | Fixed           | minValue                               | The minimum coordinate value for the axis.                     |
        // +-----------------+----------------------------------------+----------------------------------------------------------------+
        // | Fixed           | defaultValue                           | The default coordinate value for the axis.                     |
        // +-----------------+----------------------------------------+----------------------------------------------------------------+
        // | Fixed           | maxValue                               | The maximum coordinate value for the axis.                     |
        // +-----------------+----------------------------------------+----------------------------------------------------------------+
        // | uint16          | flags                                  | Axis qualifiers — see details below.                           |
        // +-----------------+----------------------------------------+----------------------------------------------------------------+
        // | uint16          | axisNameID                             | The name ID for entries in the 'name' table that provide       |
        // |                 |                                        | a display name for this axis.                                  |
        // +-----------------+----------------------------------------+----------------------------------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);

        string tag = reader.ReadTag();
        float minValue = reader.ReadFixed();
        float defaultValue = reader.ReadFixed();
        float maxValue = reader.ReadFixed();
        ushort flags = reader.ReadUInt16();
        ushort axisNameID = reader.ReadUInt16();

        return new VariationAxisRecord(tag, minValue, defaultValue, maxValue, flags, axisNameID);
    }
}
