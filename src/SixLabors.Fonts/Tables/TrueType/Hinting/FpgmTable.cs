// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.TrueType.Hinting;

/// <summary>
/// Represents the 'fpgm' (Font Program) table which contains TrueType instructions
/// that are executed once when the font is first loaded. These instructions typically
/// define functions and instruction definitions used by glyph programs.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/fpgm"/>
/// </summary>
internal class FpgmTable : Table
{
    /// <summary>
    /// The table tag name.
    /// </summary>
    internal const string TableName = "fpgm";

    /// <summary>
    /// Initializes a new instance of the <see cref="FpgmTable"/> class.
    /// </summary>
    /// <param name="instructions">The font program bytecode instructions.</param>
    public FpgmTable(byte[] instructions) => this.Instructions = instructions;

    /// <summary>
    /// Gets the font program bytecode instructions.
    /// </summary>
    public byte[] Instructions { get; }

    /// <summary>
    /// Loads the 'fpgm' table from the specified font reader.
    /// </summary>
    /// <param name="fontReader">The font reader.</param>
    /// <returns>The <see cref="FpgmTable"/>, or <see langword="null"/> if the table is not present.</returns>
    public static FpgmTable? Load(FontReader fontReader)
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
    /// Loads the 'fpgm' table from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader positioned at the start of the table.</param>
    /// <param name="tableLength">The length of the table in bytes.</param>
    /// <returns>The <see cref="FpgmTable"/>.</returns>
    public static FpgmTable Load(BigEndianBinaryReader reader, uint tableLength)
    {
        // HEADER

        // Type     | Description
        // ---------| ------------
        // uint8[n] | Instructions. n is the number of uint8 items that fit in the size of the table.
        byte[] instructions = reader.ReadUInt8Array((int)tableLength);

        return new FpgmTable(instructions);
    }
}
