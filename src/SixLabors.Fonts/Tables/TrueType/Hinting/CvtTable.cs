// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.TrueType.Hinting;

/// <summary>
/// Represents the 'cvt ' (Control Value Table) which contains a list of values
/// that can be referenced by TrueType hinting instructions.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cvt"/>
/// </summary>
internal class CvtTable : Table
{
    /// <summary>
    /// The table tag name. Note the trailing space is required.
    /// </summary>
    internal const string TableName = "cvt "; // space on the end of cvt is important/required

    /// <summary>
    /// Initializes a new instance of the <see cref="CvtTable"/> class.
    /// </summary>
    /// <param name="controlValues">The array of control values.</param>
    public CvtTable(short[] controlValues) => this.ControlValues = controlValues;

    /// <summary>
    /// Gets the array of control values referenceable by TrueType hinting instructions.
    /// </summary>
    public short[] ControlValues { get; }

    /// <summary>
    /// Loads the 'cvt ' table from the specified font reader.
    /// </summary>
    /// <param name="fontReader">The font reader.</param>
    /// <returns>The <see cref="CvtTable"/>, or <see langword="null"/> if the table is not present.</returns>
    public static CvtTable? Load(FontReader fontReader)
    {
        if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader, out TableHeader? header))
        {
            return null;
        }

        using (binaryReader)
        {
            return Load(binaryReader, header.Length);
        }
    }

    /// <summary>
    /// Loads the 'cvt ' table from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader positioned at the start of the table.</param>
    /// <param name="tableLength">The length of the table in bytes.</param>
    /// <returns>The <see cref="CvtTable"/>.</returns>
    public static CvtTable Load(BigEndianBinaryReader reader, uint tableLength)
    {
        // HEADER

        // Type     | Description
        // ---------| ------------
        // FWORD[n] | List of n values referenceable by instructions.n is the number of FWORD items that fit in the size of the table.
        const int shortSize = sizeof(short);

        int itemCount = (int)(tableLength / shortSize);

        short[] controlValues = reader.ReadFWORDArray(itemCount);

        return new CvtTable(controlValues);
    }
}
