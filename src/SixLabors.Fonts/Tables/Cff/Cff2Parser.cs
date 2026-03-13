// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Parses a Compact Font Format (CFF) version 2 font program.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2"/>
/// </summary>
internal class Cff2Parser : CffParserBase
{
    private static readonly ItemVariationStore EmptyItemVariationStoreTable = new(VariationRegionList.EmptyVariationRegionList, []);

    private long offset;

    private double[]? fontMatrix;
    private int charStringIndexOffset;
    private int variationStoreOffset;
    private int? fdArrayOffset;
    private int? fdSelectOffset;
    private ItemVariationStore? itemVariationStore;

    /// <summary>
    /// Loads and parses a CFF2 font from the given reader.
    /// </summary>
    /// <param name="reader">The binary reader positioned after the CFF2 header.</param>
    /// <param name="hdrSize">The header size in bytes.</param>
    /// <param name="topDictLength">The length of the Top DICT data in bytes.</param>
    /// <param name="fontName">The PostScript font name.</param>
    /// <param name="offset">The absolute offset of the CFF2 table in the font stream.</param>
    /// <returns>The parsed <see cref="Cff2Font"/>.</returns>
    public Cff2Font Load(BigEndianBinaryReader reader, byte hdrSize, ushort topDictLength, string fontName, long offset)
    {
        this.offset = offset;
        reader.Seek(hdrSize, SeekOrigin.Begin);

        this.ReadTopDictData(reader, topDictLength);
        reader.Seek(hdrSize + topDictLength, SeekOrigin.Begin);

        CidFontInfo cidFontInfo = new()
        {
            FDArray = this.fdArrayOffset.GetValueOrDefault(),
            FDSelect = this.fdSelectOffset.GetValueOrDefault(),
        };

        byte[][] globalSubrRawBuffers = ReadSubrBuffer(reader, cff2: true);

        // The Item Variation Store is optional. When present, its offset is
        // relative to the start of the CFF2 table.
        if (this.variationStoreOffset > 0)
        {
            reader.Seek(this.variationStoreOffset, SeekOrigin.Begin);
            ushort variationStoreLength = reader.ReadUInt16();
            this.itemVariationStore = variationStoreLength == 0
                ? EmptyItemVariationStoreTable
                : ItemVariationStore.Load(reader, this.variationStoreOffset + 2);
        }
        else
        {
            this.itemVariationStore = EmptyItemVariationStoreTable;
        }

        if (this.fdSelectOffset.HasValue)
        {
            ReadFdSelect(reader, this.offset, cidFontInfo);
        }

        CffIndexOffset[] charStringOffsets = this.ReadCharStringIndex(reader);
        byte[][] charStringBuffers = ReadCharStringBuffers(reader, charStringOffsets);

        int fdArrayOffset = this.fdArrayOffset.GetValueOrDefault();
        FontDict[] fontDicts = this.ReadFdArray(reader, this.offset, fdArrayOffset, cff2: true);
        CffTopDictionary topDictionary = new()
        {
            CidFontInfo = cidFontInfo,
            FontMatrix = this.fontMatrix ?? [0.001, 0, 0, 0.001, 0, 0]
        };

        CffPrivateDictionary privateDictionary = fontDicts.Length > 0
            ? new(fontDicts[0].LocalSubr, 0, 0)
            : new([], 0, 0);
        int glyphCount = charStringOffsets.Length;
        CffGlyphData[] glyphs = this.ReadCharStringsIndex(topDictionary, globalSubrRawBuffers, fontDicts, privateDictionary, charStringBuffers, glyphCount);

        return new(fontName, topDictionary, glyphs, this.itemVariationStore);
    }

    /// <summary>
    /// Reads the CFF2 Top DICT data, extracting offsets for CharStrings, FDArray, FDSelect, and variation store.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <param name="topDictLength">The length in bytes of the Top DICT data.</param>
    private void ReadTopDictData(BigEndianBinaryReader reader, ushort topDictLength)
    {
        long startPosition = reader.BaseStream.Position;
        long maxPosition = startPosition + topDictLength;
        while (reader.BaseStream.Position < maxPosition)
        {
            CffDataDicEntry dataDicEntry = this.ReadEntry(reader);
            switch (dataDicEntry.Operator.Name)
            {
                case "FontMatrix":
                    this.fontMatrix = new double[dataDicEntry.Operands.Length];
                    for (int i = 0; i < dataDicEntry.Operands.Length; i++)
                    {
                        this.fontMatrix[i] = dataDicEntry.Operands[i].RealNumValue;
                    }

                    break;
                case "CharStrings":
                    this.charStringIndexOffset = (int)dataDicEntry.Operands[0].RealNumValue;
                    break;
                case "FDArray":
                    this.fdArrayOffset = (int)dataDicEntry.Operands[0].RealNumValue;
                    break;
                case "FDSelect":
                    this.fdSelectOffset = (int)dataDicEntry.Operands[0].RealNumValue;
                    break;
                case "vstore":
                    this.variationStoreOffset = (int)dataDicEntry.Operands[0].RealNumValue;
                    break;
                default:
                    throw new InvalidFontFileException("Error parsing TopDictData.");
            }
        }
    }

    /// <summary>
    /// Reads the CharString INDEX offsets for CFF2.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <returns>An array of <see cref="CffIndexOffset"/> representing each charstring's position and length.</returns>
    private CffIndexOffset[] ReadCharStringIndex(BigEndianBinaryReader reader)
    {
        reader.BaseStream.Position = this.offset + this.charStringIndexOffset;
        if (!TryReadIndexDataOffsets(reader, out CffIndexOffset[]? offsets, cff2: true))
        {
            throw new InvalidFontFileException("No glyph data found.");
        }

        return offsets;
    }

    /// <summary>
    /// Reads the raw charstring byte buffers for each glyph from the CharString INDEX.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <param name="offsets">The charstring INDEX offsets.</param>
    /// <returns>An array of byte arrays, each containing a glyph's charstring data.</returns>
    private static byte[][] ReadCharStringBuffers(BigEndianBinaryReader reader, CffIndexOffset[] offsets)
    {
        int glyphCount = offsets.Length;
        byte[][] charStringBuffers = new byte[offsets.Length][];
        for (int i = 0; i < glyphCount; ++i)
        {
            CffIndexOffset cffIndexOffset = offsets[i];
            charStringBuffers[i] = reader.ReadBytes(cffIndexOffset.Length);
        }

        return charStringBuffers;
    }

    /// <summary>
    /// Creates glyph data objects for all glyphs from the pre-read charstring buffers.
    /// </summary>
    /// <param name="topDictionary">The top-level dictionary containing font metadata.</param>
    /// <param name="globalSubrBuffers">The global subroutine buffers.</param>
    /// <param name="fontDicts">The Font DICT array for CID fonts.</param>
    /// <param name="privateDictionary">The private dictionary containing local subroutine references.</param>
    /// <param name="charStringBuffers">The raw charstring byte buffers for each glyph.</param>
    /// <param name="glyphCount">The total number of glyphs.</param>
    /// <returns>An array of <see cref="CffGlyphData"/> for each glyph.</returns>
    private CffGlyphData[] ReadCharStringsIndex(
        CffTopDictionary topDictionary,
        byte[][] globalSubrBuffers,
        FontDict[] fontDicts,
        CffPrivateDictionary? privateDictionary,
        byte[][] charStringBuffers,
        int glyphCount)
    {
        // 14. CharStrings INDEX

        // This contains the charstrings of all the glyphs in a font stored in
        // an INDEX structure.

        // Charstring objects contained within this
        // INDEX are accessed by GID.

        // The first charstring(GID 0) must be
        // the.notdef glyph.

        // The number of glyphs available in a font may
        // be determined from the count field in the INDEX.

        //

        // The format of the charstring data, and therefore the method of
        // interpretation, is specified by the
        // CharstringType  operator in the Top DICT.

        // The CharstringType operator has a default value
        // of 2 indicating the Type 2 charstring format which was designed
        // in conjunction with CFF.

        // Type 1 charstrings are documented in
        // the “Adobe Type 1 Font Format” published by Addison - Wesley.

        // Type 2 charstrings are described in Adobe Technical Note #5177:
        // “Type 2 Charstring Format.” Other charstring types may also be
        // supported by this method.
        CffGlyphData[] glyphs = new CffGlyphData[glyphCount];
        byte[][]? localSubBuffer = privateDictionary?.LocalSubrRawBuffers;

        // Is the font a CID font?
        FDRangeProvider fdRangeProvider = new(topDictionary.CidFontInfo);
        bool isCidFont = topDictionary.CidFontInfo.FdRanges.Length > 0;
        int vsIndex = fontDicts.Length > 0 ? fontDicts[0].VsIndex : 0;
        for (int i = 0; i < glyphCount; ++i)
        {
            byte[] charstringsBuffer = charStringBuffers[i];

            // Now we can parse the raw glyph instructions
            // Select proper local private dict.
            if (isCidFont)
            {
                fdRangeProvider.SetCurrentGlyphIndex((ushort)i);
                int fdIndex = fdRangeProvider.SelectedFDArray;
                localSubBuffer = fontDicts[fdIndex].LocalSubr;
                vsIndex = fontDicts[fdIndex].VsIndex;
            }

            glyphs[i] = new CffGlyphData(
                (ushort)i,
                globalSubrBuffers,
                localSubBuffer ?? [],
                privateDictionary?.NominalWidthX ?? 0,
                charstringsBuffer,
                2,
                this.itemVariationStore,
                vsIndex)
            {
                FontMatrix = topDictionary.FontMatrix
            };
        }

        return glyphs;
    }
}
