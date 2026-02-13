// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace SixLabors.Fonts.Tables.Cff;

internal abstract class CffParserBase
{
    private readonly StringBuilder pooledStringBuilder = new();

    protected static void ReadFdSelect(BigEndianBinaryReader reader, long offset, CidFontInfo cidFontInfo)
    {
        if (cidFontInfo.FDSelect is 0)
        {
            return;
        }

        reader.BaseStream.Position = offset + cidFontInfo.FDSelect;
        switch (reader.ReadByte())
        {
            case 0:
            {
                cidFontInfo.FdSelectFormat = 0;
                for (int i = 0; i < cidFontInfo.CIDFountCount; i++)
                {
                    cidFontInfo.FdSelectMap[i] = reader.ReadByte();
                }

                break;
            }

            case 3:
            {
                cidFontInfo.FdSelectFormat = 3;
                ushort nRanges = reader.ReadUInt16();
                FDRange[] ranges = new FDRange[nRanges + 1];

                cidFontInfo.FdSelectFormat = 3;
                cidFontInfo.FdRanges = ranges;
                for (int i = 0; i < nRanges; ++i)
                {
                    ranges[i] = new FDRange(reader.ReadUInt16(), reader.ReadByte());
                }

                ranges[nRanges] = new FDRange(reader.ReadUInt16(), 0); // sentinel
                break;
            }

            case 4:
            {
                cidFontInfo.FdSelectFormat = 4;
                uint nRanges = reader.ReadUInt32();
                FDRange[] ranges = new FDRange[nRanges + 1];

                cidFontInfo.FdSelectFormat = 3;
                cidFontInfo.FdRanges = ranges;
                for (int i = 0; i < nRanges; ++i)
                {
                    ranges[i] = new FDRange(reader.ReadUInt32(), reader.ReadUInt16());
                }

                ranges[nRanges] = new FDRange(reader.ReadUInt32(), 0); // sentinel
                break;
            }

            default:
                throw new NotSupportedException("Only FD Select format 0, 3 and 4 are supported");
        }
    }

    protected FontDict[] ReadFdArray(BigEndianBinaryReader reader, long offset, long fdArrayOffset)
    {
        if (fdArrayOffset is 0)
        {
            return Array.Empty<FontDict>();
        }

        reader.BaseStream.Position = offset + fdArrayOffset;

        if (!TryReadIndexDataOffsets(reader, out CffIndexOffset[]? offsets))
        {
            return Array.Empty<FontDict>();
        }

        FontDict[] fontDicts = new FontDict[offsets.Length];
        for (int i = 0; i < fontDicts.Length; ++i)
        {
            // Read DICT data.
            List<CffDataDicEntry> dic = this.ReadDictData(reader, offsets[i].Length);

            // translate
            int fontDictsOffset = 0;
            int size = 0;
            int name = 0;

            foreach (CffDataDicEntry entry in dic)
            {
                switch (entry.Operator.Name)
                {
                    default:
                        throw new NotSupportedException();
                    case "FontName":
                        name = (int)entry.Operands[0].RealNumValue;
                        break;
                    case "Private": // private dic
                        size = (int)entry.Operands[0].RealNumValue;
                        fontDictsOffset = (int)entry.Operands[1].RealNumValue;
                        break;
                }
            }

            fontDicts[i] = new FontDict(name, size, fontDictsOffset);
        }

        foreach (FontDict fdict in fontDicts)
        {
            reader.BaseStream.Position = offset + fdict.PrivateDicOffset;

            List<CffDataDicEntry> dicData = this.ReadDictData(reader, fdict.PrivateDicSize);

            if (dicData.Count > 0)
            {
                // Interpret the values of private dict.
                foreach (CffDataDicEntry dicEntry in dicData)
                {
                    switch (dicEntry.Operator.Name)
                    {
                        case "Subrs":
                            int localSubrsOffset = (int)dicEntry.Operands[0].RealNumValue;
                            reader.BaseStream.Position = offset + fdict.PrivateDicOffset + localSubrsOffset;
                            fdict.LocalSubr = ReadSubrBuffer(reader);
                            break;

                        case "defaultWidthX":
                        case "nominalWidthX":
                            break;
                    }
                }
            }
        }

        return fontDicts;
    }

    protected CffDataDicEntry ReadEntry(BigEndianBinaryReader reader)
    {
        List<CffOperand> operands = new();

        //-----------------------------
        // An operator is preceded by the operand(s) that
        // specify its value.
        //--------------------------------

        //-----------------------------
        // Operators and operands may be distinguished by inspection of
        // their first byte:
        // 0–21 specify operators and
        // 28, 29, 30, and 32–254 specify operands(numbers).
        // Byte values 22–27, 31, and 255 are reserved.

        // An operator may be preceded by up to a maximum of 48 operands
        CFFOperator? @operator;
        while (true)
        {
            byte b0 = reader.ReadUInt8();

            if (b0 is >= 0 and <= 24)
            {
                // operators
                @operator = ReadOperator(reader, b0);
                break; // **break after found operator
            }
            else if (b0 is 28 or 29)
            {
                int num = ReadIntegerNumber(reader, b0);
                operands.Add(new CffOperand(num, OperandKind.IntNumber));
            }
            else if (b0 == 30)
            {
                double num = this.ReadRealNumber(reader);
                operands.Add(new CffOperand(num, OperandKind.RealNumber));
            }
            else if (b0 is >= 32 and <= 254)
            {
                int num = ReadIntegerNumber(reader, b0);
                operands.Add(new CffOperand(num, OperandKind.IntNumber));
            }
            else
            {
                throw new NotSupportedException("invalid DICT data b0 byte: " + b0);
            }
        }

        // I'm fairly confident that the operator can never be null.
        return new CffDataDicEntry(@operator!, operands.ToArray());
    }

    protected static bool TryReadIndexDataOffsets(BigEndianBinaryReader reader, [NotNullWhen(true)] out CffIndexOffset[]? value)
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
        ushort count = reader.ReadUInt16();
        if (count == 0)
        {
            value = null;
            return false;
        }

        int offSize = reader.ReadByte();
        int[] offsets = new int[count + 1];
        CffIndexOffset[] indexElems = new CffIndexOffset[count];
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

    protected static byte[][] ReadSubrBuffer(BigEndianBinaryReader reader)
    {
        if (!TryReadIndexDataOffsets(reader, out CffIndexOffset[]? offsets))
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

    protected List<CffDataDicEntry> ReadDictData(BigEndianBinaryReader reader, int length)
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

    private static CFFOperator ReadOperator(BigEndianBinaryReader reader, byte b0)
    {
        // Read operator key.
        byte b1 = 0;
        if (b0 == 12)
        {
            // 2 bytes
            b1 = reader.ReadUInt8();
        }

        // Get registered operator by its key.
        return CFFOperator.GetOperatorByKey(b0, b1);
    }

    private double ReadRealNumber(BigEndianBinaryReader reader)
    {
        // from https://typekit.files.wordpress.com/2013/05/5176.cff.pdf
        // A real number operand is provided in addition to integer
        // operands.This operand begins with a byte value of 30 followed
        // by a variable-length sequence of bytes.Each byte is composed
        // of two 4 - bit nibbles asdefined in Table 5.

        // The first nibble of a
        // pair is stored in the most significant 4 bits of a byte and the
        // second nibble of a pair is stored in the least significant 4 bits of a byte
        StringBuilder sb = this.pooledStringBuilder;
        sb.Clear(); // reset

        bool done = false;
        bool exponentMissing = false;
        while (!done)
        {
            int b = reader.ReadByte();

            int nb_0 = (b >> 4) & 0xf;
            int nb_1 = b & 0xf;

            for (int i = 0; !done && i < 2; ++i)
            {
                int nibble = (i == 0) ? nb_0 : nb_1;

                switch (nibble)
                {
                    case 0x0:
                    case 0x1:
                    case 0x2:
                    case 0x3:
                    case 0x4:
                    case 0x5:
                    case 0x6:
                    case 0x7:
                    case 0x8:
                    case 0x9:
                        sb.Append(nibble);
                        exponentMissing = false;
                        break;
                    case 0xa:
                        sb.Append('.');
                        break;
                    case 0xb:
                        sb.Append('E');
                        exponentMissing = true;
                        break;
                    case 0xc:
                        sb.Append("E-");
                        exponentMissing = true;
                        break;
                    case 0xd:
                        break;
                    case 0xe:
                        sb.Append('-');
                        break;
                    case 0xf:
                        done = true;
                        break;
                    default:
                        throw new FontException("Unable to read real number.");
                }
            }
        }

        if (exponentMissing)
        {
            // the exponent is missing, just append "0" to avoid an exception
            // not sure if 0 is the correct value, but it seems to fit
            // see PDFBOX-1522
            sb.Append('0');
        }

        if (sb.Length == 0)
        {
            return 0d;
        }

        if (!double.TryParse(
            sb.ToString(),
            NumberStyles.Number | NumberStyles.AllowExponent,
            CultureInfo.InvariantCulture,
            out double value))
        {
            throw new NotSupportedException();
        }

        return value;
    }

    private static int ReadIntegerNumber(BigEndianBinaryReader reader, byte b0)
    {
        if (b0 == 28)
        {
            return reader.ReadInt16();
        }

        if (b0 == 29)
        {
            return reader.ReadInt32();
        }

        if (b0 is >= 32 and <= 246)
        {
            return b0 - 139;
        }

        if (b0 is >= 247 and <= 250)
        {
            int b1 = reader.ReadByte();
            return ((b0 - 247) * 256) + b1 + 108;
        }

        if (b0 is >= 251 and <= 254)
        {
            int b1 = reader.ReadByte();
            return (-(b0 - 251) * 256) - b1 - 108;
        }

        throw new InvalidFontFileException("Invalid DICT data b0 byte: " + b0);
    }
}
