// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Kern;

/// <summary>
/// Represents the coverage field of a kerning subtable, describing the format
/// and properties of the kerning data.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern"/>
/// </summary>
internal readonly struct KerningCoverage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KerningCoverage"/> struct.
    /// </summary>
    /// <param name="horizontal">Whether the table contains horizontal kerning data.</param>
    /// <param name="hasMinimum">Whether the table contains minimum values instead of kerning values.</param>
    /// <param name="crossStream">Whether kerning is perpendicular to the flow of text.</param>
    /// <param name="overrideAccumulator">Whether the kerning value should replace the currently accumulated value.</param>
    /// <param name="format">The subtable format number.</param>
    private KerningCoverage(bool horizontal, bool hasMinimum, bool crossStream, bool overrideAccumulator, byte format)
    {
        this.Horizontal = horizontal;
        this.HasMinimum = hasMinimum;
        this.CrossStream = crossStream;
        this.OverrideAccumulator = overrideAccumulator;
        this.Format = format;
    }

    /// <summary>
    /// Gets a value indicating whether the table contains horizontal kerning data.
    /// If <see langword="false"/>, the table contains vertical kerning data.
    /// </summary>
    public bool Horizontal { get; }

    /// <summary>
    /// Gets a value indicating whether the table contains minimum values.
    /// If <see langword="false"/>, the table contains kerning values.
    /// </summary>
    public bool HasMinimum { get; }

    /// <summary>
    /// Gets a value indicating whether kerning is perpendicular to the flow of text.
    /// </summary>
    public bool CrossStream { get; }

    /// <summary>
    /// Gets a value indicating whether the value in this table should replace
    /// the value currently being accumulated.
    /// </summary>
    public bool OverrideAccumulator { get; }

    /// <summary>
    /// Gets the format of the subtable. Only formats 0 and 2 have been defined.
    /// </summary>
    public byte Format { get; }

    /// <summary>
    /// Reads a <see cref="KerningCoverage"/> from the specified binary reader.
    /// </summary>
    /// <param name="reader">The binary reader positioned at the coverage field.</param>
    /// <returns>The parsed <see cref="KerningCoverage"/>.</returns>
    public static KerningCoverage Read(BigEndianBinaryReader reader)
    {
        // The coverage field is divided into the following sub-fields, with sizes given in bits:
        // Sub-field    | Bits #'s | Size | Description
        // -------------|----------|------|-----------------------------------------------
        // horizontal   |  0       |  1   | 1 if table has horizontal data, 0 if vertical.
        // minimum      |  1       |  1   | If this bit is set to 1, the table has minimum values.If set to 0, the table has kerning values.
        // cross-stream |  2       |  1   | If set to 1, kerning is perpendicular to the flow of the text.
        //                                  If the text is normally written horizontally, kerning will be done in the up and down directions.If kerning values are positive, the text will be kerned upwards; if they are negative, the text will be kerned downwards.
        //                                  If the text is normally written vertically, kerning will be done in the left and right directions.If kerning values are positive, the text will be kerned to the right; if they are negative, the text will be kerned to the left.
        //                                  The value 0x8000 in the kerning data resets the cross-stream kerning back to 0.
        // override     | 3        |  1   | If this bit is set to 1 the value in this table should replace the value currently being accumulated.
        // reserved1    | 4 -7     |  4   | Reserved.This should be set to zero.
        // format       | 8 -15    |  8   | Format of the subtable. Only formats 0 and 2 have been defined.Formats 1 and 3 through 255 are reserved for future use.
        ushort coverage = reader.ReadUInt16();
        bool horizontal = (coverage & 0x1) == 1;
        bool hasMinimum = ((coverage >> 1) & 0x1) == 1;
        bool crossStream = ((coverage >> 2) & 0x1) == 1;
        bool overrideAccumulator = ((coverage >> 3) & 0x1) == 1;
        byte format = (byte)((coverage >> 7) & 0xff);
        return new KerningCoverage(horizontal, hasMinimum, crossStream, overrideAccumulator, format);
    }
}
