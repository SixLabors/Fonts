// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables;

/// <summary>
/// Represents a table record entry in the font directory.
/// Each record contains the tag, checksum, offset, and length of a font table.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#table-directory"/>
/// </summary>
internal class TableHeader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableHeader"/> class.
    /// </summary>
    /// <param name="tag">The four-byte table tag identifier.</param>
    /// <param name="checkSum">The checksum for the table.</param>
    /// <param name="offset">The byte offset of the table from the beginning of the font file.</param>
    /// <param name="len">The length of the table in bytes.</param>
    public TableHeader(string tag, uint checkSum, uint offset, uint len)
    {
        this.Tag = tag;
        this.CheckSum = checkSum;
        this.Offset = offset;
        this.Length = len;
    }

    /// <summary>
    /// Gets the four-byte table tag identifier (e.g. "head", "glyf", "cmap").
    /// </summary>
    public string Tag { get; }

    /// <summary>
    /// Gets the byte offset of the table from the beginning of the font file.
    /// </summary>
    public uint Offset { get; }

    /// <summary>
    /// Gets the checksum for the table, used to verify table integrity.
    /// </summary>
    public uint CheckSum { get; }

    /// <summary>
    /// Gets the length of the table data in bytes.
    /// </summary>
    public uint Length { get; }

    /// <summary>
    /// Reads a <see cref="TableHeader"/> from the given reader.
    /// </summary>
    /// <param name="reader">The binary reader positioned at the table record.</param>
    /// <returns>The parsed <see cref="TableHeader"/>.</returns>
    public static TableHeader Read(BigEndianBinaryReader reader) => new TableHeader(
            reader.ReadTag(),
            reader.ReadUInt32(),
            reader.ReadOffset32(),
            reader.ReadUInt32());

    /// <summary>
    /// Creates a <see cref="BigEndianBinaryReader"/> positioned at the start of this table's data.
    /// </summary>
    /// <param name="stream">The font file stream.</param>
    /// <returns>A reader positioned at the table data.</returns>
    public virtual BigEndianBinaryReader CreateReader(Stream stream)
    {
        stream.Seek(this.Offset, SeekOrigin.Begin);

        return new BigEndianBinaryReader(stream, true);
    }
}
