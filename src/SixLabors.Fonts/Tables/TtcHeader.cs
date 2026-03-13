// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables;

/// <summary>
/// Represents a font collection header (for .ttc font collections).
/// A font collection contains one or more fonts where typically the glyf table is shared by multiple fonts to save space,
/// but other tables are not.
/// Each font in the collection has its own set of tables.
/// </summary>
internal class TtcHeader
{
    internal const string TableName = "ttcf";

    /// <summary>
    /// Initializes a new instance of the <see cref="TtcHeader"/> class.
    /// </summary>
    /// <param name="ttcTag">The TTC tag, expected to be "ttcf".</param>
    /// <param name="majorVersion">The major version of the TTC header (1 or 2).</param>
    /// <param name="minorVersion">The minor version of the TTC header.</param>
    /// <param name="numFonts">The number of fonts in the collection.</param>
    /// <param name="offsetTable">Array of byte offsets to each font's offset table.</param>
    /// <param name="dsigTag">The DSIG table tag (version 2+ only, otherwise 0).</param>
    /// <param name="dsigLength">The length of the DSIG table in bytes (version 2+ only, otherwise 0).</param>
    /// <param name="dsigOffset">The byte offset of the DSIG table (version 2+ only, otherwise 0).</param>
    public TtcHeader(string ttcTag, ushort majorVersion, ushort minorVersion, uint numFonts, uint[] offsetTable, uint dsigTag, uint dsigLength, uint dsigOffset)
    {
        this.TtcTag = ttcTag;
        this.MajorVersion = majorVersion;
        this.MinorVersion = minorVersion;
        this.NumFonts = numFonts;
        this.OffsetTable = offsetTable;
        this.DsigTag = dsigTag;
        this.DsigLength = dsigLength;
        this.DsigOffset = dsigOffset;
    }

    /// <summary>
    /// Gets the tag, should be "ttcf".
    /// </summary>
    public string TtcTag { get; }

    /// <summary>
    /// Gets the major version of the TTC header. Version 1 has no DSIG; version 2 includes DSIG fields.
    /// </summary>
    public ushort MajorVersion { get; }

    /// <summary>
    /// Gets the minor version of the TTC header.
    /// </summary>
    public ushort MinorVersion { get; }

    /// <summary>
    /// Gets the number of fonts contained in the collection.
    /// </summary>
    public uint NumFonts { get; }

    /// <summary>
    /// Gets the array of offsets to the OffsetTable of each font. Use <see cref="FontReader"/> for each font.
    /// </summary>
    public uint[] OffsetTable { get; }

    /// <summary>
    /// Gets the tag of the DSIG (digital signature) table. Only present in version 2+ headers.
    /// </summary>
    public uint DsigTag { get; }

    /// <summary>
    /// Gets the length of the DSIG table in bytes. Only present in version 2+ headers.
    /// </summary>
    public uint DsigLength { get; }

    /// <summary>
    /// Gets the byte offset of the DSIG table from the beginning of the file. Only present in version 2+ headers.
    /// </summary>
    public uint DsigOffset { get; }

    /// <summary>
    /// Reads a <see cref="TtcHeader"/> from the given reader.
    /// </summary>
    /// <param name="reader">The binary reader positioned at the start of the TTC header.</param>
    /// <returns>The parsed <see cref="TtcHeader"/>.</returns>
    /// <exception cref="InvalidFontTableException">Thrown when the tag is not "ttcf".</exception>
    public static TtcHeader Read(BigEndianBinaryReader reader)
    {
        string tag = reader.ReadTag();

        if (tag != TableName)
        {
            throw new InvalidFontTableException($"Expected tag = {TableName} found {tag}", TableName);
        }

        ushort majorVersion = reader.ReadUInt16();
        ushort minorVersion = reader.ReadUInt16();
        uint numFonts = reader.ReadUInt32();
        uint[] offsetTable = new uint[numFonts];
        for (int i = 0; i < numFonts; ++i)
        {
            offsetTable[i] = reader.ReadOffset32();
        }

        // Version 2 fields
        uint dsigTag = 0;
        uint dsigLength = 0;
        uint dsigOffset = 0;
        if (majorVersion >= 2)
        {
            dsigTag = reader.ReadUInt32();
            dsigLength = reader.ReadUInt32();
            dsigOffset = reader.ReadUInt32();
        }

        return new TtcHeader(tag, majorVersion, minorVersion, numFonts, offsetTable, dsigTag, dsigLength, dsigOffset);
    }
}
