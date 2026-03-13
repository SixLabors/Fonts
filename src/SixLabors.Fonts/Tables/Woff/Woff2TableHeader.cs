// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Woff;

/// <summary>
/// Represents a table directory entry in a WOFF2 font file.
/// Each entry describes a single font table within the WOFF2 container.
/// See: <see href="https://www.w3.org/TR/WOFF2/#table_dir_format"/>.
/// </summary>
internal sealed class Woff2TableHeader : TableHeader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Woff2TableHeader"/> class.
    /// </summary>
    /// <param name="tag">The 4-byte table identifier tag.</param>
    /// <param name="checkSum">The checksum of the uncompressed table data.</param>
    /// <param name="offset">The offset to the table data within the decompressed WOFF2 data stream.</param>
    /// <param name="len">The length of the table data (transform length if transformed, otherwise original length).</param>
    public Woff2TableHeader(string tag, uint checkSum, uint offset, uint len)
        : base(tag, checkSum, offset, len)
    {
    }
}
