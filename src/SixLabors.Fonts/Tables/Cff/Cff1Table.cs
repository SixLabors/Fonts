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

    public Cff1Table(CffFont cff1Font) => this.glyphs = cff1Font.Glyphs;

    public int GlyphCount => this.glyphs.Length;

    public ItemVariationStore? ItemVariationStore => null;

    public CffGlyphData GetGlyph(int index)
        => this.glyphs[index];

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
