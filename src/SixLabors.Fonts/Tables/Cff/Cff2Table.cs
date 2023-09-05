// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

internal sealed class Cff2Table : Table, ICffTable
{
    internal const string TableName = "CFF2";

    private readonly CffGlyphData[] glyphs;

    public Cff2Table(CffFont cff1Font) => this.glyphs = cff1Font.Glyphs;

    public int GlyphCount => this.glyphs.Length;

    public CffGlyphData GetGlyph(int index)
        => this.glyphs[index];

    public static Cff2Table? Load(FontReader fontReader)
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

    public static Cff2Table Load(BigEndianBinaryReader reader) => throw new NotSupportedException("CFF2 Fonts are not currently supported.");
}
