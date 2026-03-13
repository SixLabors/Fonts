// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.TrueType.Hinting;

/// <summary>
/// Represents the 'prep' (Control Value Program) table which contains TrueType instructions
/// that are executed whenever the point size or font transformation changes. The prep program
/// typically adjusts control values in the CVT for the current rendering size.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/prep"/>
/// </summary>
internal class PrepTable : Table
{
    /// <summary>
    /// The table tag name.
    /// </summary>
    internal const string TableName = "prep";

    /// <summary>
    /// Initializes a new instance of the <see cref="PrepTable"/> class.
    /// </summary>
    /// <param name="instructions">The control value program bytecode instructions.</param>
    public PrepTable(byte[] instructions) => this.Instructions = instructions;

    /// <summary>
    /// Gets the control value program bytecode instructions.
    /// </summary>
    public byte[] Instructions { get; }

    /// <summary>
    /// Loads the 'prep' table from the specified font reader.
    /// </summary>
    /// <param name="fontReader">The font reader.</param>
    /// <returns>The <see cref="PrepTable"/>, or <see langword="null"/> if the table is not present.</returns>
    public static PrepTable? Load(FontReader fontReader)
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
    /// Loads the 'prep' table from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader positioned at the start of the table.</param>
    /// <param name="tableLength">The length of the table in bytes.</param>
    /// <returns>The <see cref="PrepTable"/>.</returns>
    public static PrepTable Load(BigEndianBinaryReader reader, uint tableLength)
    {
        // HEADER

        // Type     | Description
        // ---------| ------------
        // uint8[n] | Set of instructions executed whenever point size or font or transformation change. n is the number of uint8 items that fit in the size of the table.
        byte[]? instructions = reader.ReadUInt8Array((int)tableLength);

        return new PrepTable(instructions);
    }
}
