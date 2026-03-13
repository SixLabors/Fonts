// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.CMap;

/// <summary>
/// Represents an encoding record in the 'cmap' table header. Each record specifies a platform ID,
/// encoding ID, and byte offset to the subtable for that encoding.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap"/>
/// </summary>
internal readonly struct EncodingRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EncodingRecord"/> struct.
    /// </summary>
    /// <param name="platformID">The platform identifier.</param>
    /// <param name="encodingID">The platform-specific encoding identifier.</param>
    /// <param name="offset">The byte offset from the beginning of the 'cmap' table to the subtable.</param>
    public EncodingRecord(PlatformIDs platformID, ushort encodingID, uint offset)
    {
        this.PlatformID = platformID;
        this.EncodingID = encodingID;
        this.Offset = offset;
    }

    /// <summary>
    /// Gets the platform identifier.
    /// </summary>
    public PlatformIDs PlatformID { get; }

    /// <summary>
    /// Gets the platform-specific encoding identifier.
    /// </summary>
    public ushort EncodingID { get; }

    /// <summary>
    /// Gets the byte offset from the beginning of the 'cmap' table to the subtable.
    /// </summary>
    public uint Offset { get; }

    /// <summary>
    /// Reads an <see cref="EncodingRecord"/> from the specified reader.
    /// </summary>
    /// <param name="reader">The binary reader positioned at the encoding record data.</param>
    /// <returns>The parsed <see cref="EncodingRecord"/>.</returns>
    public static EncodingRecord Read(BigEndianBinaryReader reader)
    {
        var platform = (PlatformIDs)reader.ReadUInt16();
        ushort encoding = reader.ReadUInt16();
        uint offset = reader.ReadOffset32();

        return new EncodingRecord(platform, encoding, offset);
    }
}
