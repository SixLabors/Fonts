// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SixLabors.Fonts.Tables.Cff
{
    internal abstract class CffParserBase
    {
        private readonly StringBuilder pooledStringBuilder = new();

        public CffDataDicEntry ReadEntry(BigEndianBinaryReader reader)
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
                    @operator = this.ReadOperator(reader, b0);
                    break; // **break after found operator
                }
                else if (b0 is 28 or 29)
                {
                    int num = this.ReadIntegerNumber(reader, b0);
                    operands.Add(new CffOperand(num, OperandKind.IntNumber));
                }
                else if (b0 == 30)
                {
                    double num = this.ReadRealNumber(reader);
                    operands.Add(new CffOperand(num, OperandKind.RealNumber));
                }
                else if (b0 is >= 32 and <= 254)
                {
                    int num = this.ReadIntegerNumber(reader, b0);
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

        private CFFOperator ReadOperator(BigEndianBinaryReader reader, byte b0)
        {
            // read operator key
            byte b1 = 0;
            if (b0 == 12)
            {
                // 2 bytes
                b1 = reader.ReadUInt8();
            }

            // get registered operator by its key
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
                            sb.Append(".");
                            break;
                        case 0xb:
                            sb.Append("E");
                            exponentMissing = true;
                            break;
                        case 0xc:
                            sb.Append("E-");
                            exponentMissing = true;
                            break;
                        case 0xd:
                            break;
                        case 0xe:
                            sb.Append("-");
                            break;
                        case 0xf:
                            done = true;
                            break;
                        default:
                            throw new Exception("IllegalArgumentException");
                    }
                }
            }

            if (exponentMissing)
            {
                // the exponent is missing, just append "0" to avoid an exception
                // not sure if 0 is the correct value, but it seems to fit
                // see PDFBOX-1522
                sb.Append("0");
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

        private int ReadIntegerNumber(BigEndianBinaryReader reader, byte b0)
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
}
