// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Represents the Compact Font Format (CFF) version 1 table.
/// <see href="https://adobe-type-tools.github.io/font-tech-notes/pdfs/5176.CFF.pdf"/>
/// </summary>
internal sealed class Cff1Table : Table, ICffTable
{
    internal const string TableName = "CFF "; // 4 chars

    private readonly CffGlyphData[] glyphs;

    /// <summary>
    /// Initializes a new instance of the <see cref="Cff1Table"/> class.
    /// </summary>
    /// <param name="cff1Font">The parsed CFF1 font.</param>
    public Cff1Table(CffFont cff1Font) => this.glyphs = cff1Font.Glyphs;

    /// <inheritdoc/>
    public int GlyphCount => this.glyphs.Length;

    /// <inheritdoc/>
    public ItemVariationStore? ItemVariationStore => null;

    /// <inheritdoc/>
    public CffGlyphData GetGlyph(int index)
        => this.glyphs[index];

    /// <summary>
    /// Loads the CFF1 table from the specified font reader.
    /// </summary>
    /// <param name="fontReader">The font reader.</param>
    /// <returns>The <see cref="Cff1Table"/>, or <see langword="null"/> if the table is not present.</returns>
    public static Cff1Table? Load(FontReader fontReader)
    {
        if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
        {
            return null;
        }

        using (binaryReader)
        {
            return Load(binaryReader);
        }
    }

    /// <summary>
    /// Loads the CFF1 table from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader positioned at the CFF1 table header.</param>
    /// <returns>The <see cref="Cff1Table"/>.</returns>
    public static Cff1Table Load(BigEndianBinaryReader reader)
    {
        // +------+---------------+----------------------------------------+
        // | Type | Name          | Description                            |
        // +======+===============+========================================+
        // | byte | majorVersion  | Format major version. Set to 1.        |
        // +------+---------------+----------------------------------------+
        // | byte | minorVersion  | Format minor version. Set to zero.     |
        // +------+---------------+----------------------------------------+
        // | byte | headerSize    | Header size (bytes).                   |
        // +------+---------------+----------------------------------------+
        // | byte | topDictLength | Length of Top DICT structure in bytes. |
        // +------+---------------+----------------------------------------+
        long position = reader.BaseStream.Position;
        byte[] header = reader.ReadBytes(4);
        byte major = header[0];
        byte minor = header[1];
        byte hdrSize = header[2];
        byte offSize = header[3];

        switch (major)
        {
            case 1:
                Cff1Parser parser = new();
                return new(parser.Load(reader, position));

            default:
                throw new NotSupportedException();
        }
    }
}
