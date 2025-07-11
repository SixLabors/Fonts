// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

internal sealed class Cff1Table : Table, ICffTable
{
    internal const string TableName = "CFF "; // 4 chars

    private readonly CffGlyphData[] glyphs;

    public Cff1Table(CffFont cff1Font) => this.glyphs = cff1Font.Glyphs;

    public int GlyphCount => this.glyphs.Length;

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
                CffParser parser = new();
                return new Cff1Table(parser.Load(reader, position));

            default:
                throw new NotSupportedException();
        }
    }
}
