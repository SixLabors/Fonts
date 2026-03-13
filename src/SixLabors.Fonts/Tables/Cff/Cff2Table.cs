// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;
using SixLabors.Fonts.Tables.General.Name;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Represents the Compact Font Format (CFF) version 2 table.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2"/>
/// </summary>
internal sealed class Cff2Table : Table, ICffTable
{
    internal const string TableName = "CFF2";

    private readonly CffGlyphData[] glyphs;

    /// <summary>
    /// Initializes a new instance of the <see cref="Cff2Table"/> class.
    /// </summary>
    /// <param name="cffFont">The parsed CFF font.</param>
    /// <param name="itemVariationStore">The item variation store for font variations.</param>
    public Cff2Table(CffFont cffFont, ItemVariationStore itemVariationStore)
    {
        this.glyphs = cffFont.Glyphs;
        this.ItemVariationStore = itemVariationStore;
    }

    /// <inheritdoc/>
    public int GlyphCount => this.glyphs.Length;

    /// <inheritdoc/>
    public ItemVariationStore ItemVariationStore { get; }

    /// <inheritdoc/>
    public CffGlyphData GetGlyph(int index)
        => this.glyphs[index];

    /// <summary>
    /// Loads the CFF2 table from the specified font reader.
    /// </summary>
    /// <param name="fontReader">The font reader.</param>
    /// <returns>The <see cref="Cff2Table"/>, or <see langword="null"/> if the table is not present.</returns>
    public static Cff2Table? Load(FontReader fontReader)
    {
        if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
        {
            return null;
        }

        NameTable nameTable = fontReader.GetTable<NameTable>();
        string fontName = nameTable.GetNameById(CultureInfo.InvariantCulture, KnownNameIds.PostscriptName);

        using (binaryReader)
        {
            return Load(binaryReader, fontName);
        }
    }

    /// <summary>
    /// Loads the CFF2 table from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader positioned at the CFF2 table header.</param>
    /// <param name="fontName">The PostScript font name.</param>
    /// <returns>The <see cref="Cff2Table"/>.</returns>
    public static Cff2Table Load(BigEndianBinaryReader reader, string fontName)
    {
        long position = reader.BaseStream.Position;
        byte major = reader.ReadUInt8();
        byte minor = reader.ReadUInt8();
        byte hdrSize = reader.ReadUInt8();
        ushort topDictLength = reader.ReadUInt16();

        switch (major)
        {
            case 2:
                Cff2Parser parser = new();
                Cff2Font cffFont = parser.Load(reader, hdrSize, topDictLength, fontName, position);
                return new(cffFont, cffFont.ItemVariationStore);

            default:
                throw new NotSupportedException("CFF version 2 is expected");
        }
    }
}
