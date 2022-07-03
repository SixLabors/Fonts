// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

namespace SixLabors.Fonts.Tables.Cff
{
    /// <summary>
    /// Parses a Compact Font Format (CFF) version 2 described in https://docs.microsoft.com/de-de/typography/opentype/spec/cff2
    /// </summary>
    internal class Cff2Parser : CffParserBase
    {
        private long offset;

        private int fontMatrixOffset;
        private int charStringIndexOffset;
        private int variationStoreOffset;
        private int? fdArrayOffset;
        private int? fdSelectOffset;
        private ItemVariationStore? itemVariationStore;

        public Cff2Font Load(BigEndianBinaryReader reader, byte hdrSize, ushort topDictLength, long offset)
        {
            this.offset = offset;
            reader.Seek(hdrSize, SeekOrigin.Begin);

            // TODO: spec states: font name can be copied from the name ID 6 value in the 'name' table.
            string fontName = "placeHolder";

            long startPos = reader.BaseStream.Position;
            this.ReadTopDictData(reader, topDictLength);
            reader.Seek(hdrSize + topDictLength, SeekOrigin.Begin);

            byte[][] globalSubrRawBuffers = this.ReadGlobalSubrIndex(reader);

            this.itemVariationStore = ItemVariationStore.Load(reader, this.variationStoreOffset);

            // TODO: FDSelect?
            CffIndexOffset[] charStringOffsets = this.ReadCharStringIndex(reader);
            byte[][] charStringBuffers = this.ReadCharStringBuffers(reader, charStringOffsets);

            int fdArrayOffset = this.fdArrayOffset.GetValueOrDefault();
            FontDict[] fontDicts = this.ReadFdArray(reader, this.offset, fdArrayOffset);
            var topDictionary = new CffTopDictionary
            {
                CidFontInfo = new CidFontInfo()
                {
                    FDArray = fdArrayOffset,
                }
            };

            var privateDictionary = new CffPrivateDictionary(fontDicts[0].LocalSubr, 0, 0);
            int glyphCount = charStringOffsets.Length;
            CffGlyphData[] glyphs = this.ReadCharStringsIndex(topDictionary, globalSubrRawBuffers, fontDicts, privateDictionary, charStringBuffers, glyphCount);

            return new(fontName, topDictionary, glyphs, this.itemVariationStore);
        }

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
                        this.fontMatrixOffset = (int)dataDicEntry.Operands[0].RealNumValue;
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

        private byte[][] ReadGlobalSubrIndex(BigEndianBinaryReader reader, bool cff2 = true)

            // 16. Local / Global Subrs INDEXes
            // Both Type 1 and Type 2 charstrings support the notion of
            // subroutines or subrs.

            // A subr is typically a sequence of charstring
            // bytes representing a sub - program that occurs in more than one
            // place in a font’s charstring data.

            // This subr may be stored once
            // but referenced many times from within one or more charstrings
            // by the use of the call subr  operator whose operand is the
            // number of the subr to be called.

            // The subrs are local to a  particular font and
            // cannot be shared between fonts.

            // Type 2 charstrings also permit global subrs which function in the same
            // way but are called by the call gsubr operator and may be shared
            // across fonts.

            // Local subrs are stored in an INDEX structure which is located via
            // the offset operand of the Subrs  operator in the Private DICT.
            // A font without local subrs has no Subrs operator in the Private DICT.

            // Global subrs are stored in an INDEX structure which follows the
            // String INDEX. A FontSet without any global subrs is represented
            // by an empty Global Subrs INDEX.
            => this.ReadSubrBuffer(reader, cff2);

        private byte[][] ReadSubrBuffer(BigEndianBinaryReader reader, bool cff2 = true)
        {
            if (!this.TryReadIndexDataOffsets(reader, cff2, out CffIndexOffset[]? offsets))
            {
                return Array.Empty<byte[]>();
            }

            byte[][] rawBufferList = new byte[offsets.Length][];

            for (int i = 0; i < rawBufferList.Length; ++i)
            {
                CffIndexOffset offset = offsets[i];
                rawBufferList[i] = reader.ReadBytes(offset.Length);
            }

            return rawBufferList;
        }

        private CffIndexOffset[] ReadCharStringIndex(BigEndianBinaryReader reader)
        {
            reader.BaseStream.Position = this.offset + this.charStringIndexOffset;
            if (!this.TryReadIndexDataOffsets(reader, true, out CffIndexOffset[]? offsets))
            {
                throw new InvalidFontFileException("No glyph data found.");
            }

            return offsets;
        }

        private byte[][] ReadCharStringBuffers(BigEndianBinaryReader reader, CffIndexOffset[] offsets)
        {
            int glyphCount = offsets.Length;
            byte[][] charStringBuffers = new byte[offsets.Length][];
            for (int i = 0; i < glyphCount; ++i)
            {
                CffIndexOffset cffIndexOffset = offsets[i];
                byte[] charStringsBuffer = reader.ReadBytes(cffIndexOffset.Length);
                charStringBuffers[i] = charStringsBuffer;
            }

            return charStringBuffers;
        }

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
            var glyphs = new CffGlyphData[glyphCount];
            byte[][]? localSubBuffer = privateDictionary?.LocalSubrRawBuffers;

            // Is the font a CID font?
            FDRangeProvider fdRangeProvider = new(topDictionary.CidFontInfo);
            bool isCidFont = topDictionary.CidFontInfo.FdRanges.Length > 0;
            for (int i = 0; i < glyphCount; ++i)
            {
                byte[] charstringsBuffer = charStringBuffers[i];

                // Now we can parse the raw glyph instructions
                // Select proper local private dict.
                if (isCidFont)
                {
                    fdRangeProvider.SetCurrentGlyphIndex((ushort)i);
                    localSubBuffer = fontDicts[fdRangeProvider.SelectedFDArray].LocalSubr;
                }

                glyphs[i] = new CffGlyphData(
                    (ushort)i,
                    globalSubrBuffers,
                    localSubBuffer ?? Array.Empty<byte[]>(),
                    privateDictionary?.NominalWidthX ?? 0,
                    charstringsBuffer,
                    2,
                    this.itemVariationStore);
            }

            return glyphs;
        }

        private bool TryReadIndexDataOffsets(BigEndianBinaryReader reader, bool cff2, [NotNullWhen(true)] out CffIndexOffset[]? value)
        {
            // INDEX Data
            // An INDEX is an array of variable-sized objects.It comprises a
            // header, an offset array, and object data.
            // The offset array specifies offsets within the object data.
            // An object is retrieved by
            // indexing the offset array and fetching the object at the
            // specified offset.
            // The object’s length can be determined by subtracting its offset
            // from the next offset in the offset array.
            // An additional offset is added at the end of the offset array so the
            // length of the last object may be determined.
            // The INDEX format is shown in Table 7

            // Table 7 INDEX Format
            // Type        Name                  Description
            // Card16      count                 Number of objects stored in INDEX
            // OffSize     offSize               Offset array element size
            // Offset      offset[count + 1]     Offset array(from byte preceding object data)
            // Card8       data[<varies>]        Object data

            // Offsets in the offset array are relative to the byte that precedes
            // the object data. Therefore the first element of the offset array
            // is always 1. (This ensures that every object has a corresponding
            // offset which is always nonzero and permits the efficient
            // implementation of dynamic object loading.)

            // An empty INDEX is represented by a count field with a 0 value
            // and no additional fields.Thus, the total size of an empty INDEX
            // is 2 bytes.

            // Note 2
            // An INDEX may be skipped by jumping to the offset specified by the last
            // element of the offset array
            uint count = cff2 ? reader.ReadUInt32() : reader.ReadUInt16();

            if (count == 0)
            {
                value = null;
                return false;
            }

            int offSize = reader.ReadByte();
            int[] offsets = new int[count + 1];
            var indexElems = new CffIndexOffset[count];
            for (int i = 0; i <= count; ++i)
            {
                offsets[i] = reader.ReadOffset(offSize);
            }

            for (int i = 0; i < count; ++i)
            {
                indexElems[i] = new CffIndexOffset(offsets[i], offsets[i + 1] - offsets[i]);
            }

            value = indexElems;
            return true;
        }

        private List<CffDataDicEntry> ReadDICTData(BigEndianBinaryReader reader, int length)
        {
            // 4. DICT Data

            // Font dictionary data comprising key-value pairs is represented
            // in a compact tokenized format that is similar to that used to
            // represent Type 1 charstrings.

            // Dictionary keys are encoded as 1- or 2-byte operators and dictionary values are encoded as
            // variable-size numeric operands that represent either integer or
            // real values.

            //-----------------------------
            // A DICT is simply a sequence of
            // operand(s)/operator bytes concatenated together.
            int maxIndex = (int)(reader.BaseStream.Position + length);
            List<CffDataDicEntry> dicData = new();
            while (reader.BaseStream.Position < maxIndex)
            {
                CffDataDicEntry dicEntry = this.ReadEntry(reader);
                dicData.Add(dicEntry);
            }

            return dicData;
        }
    }
}
