// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Text;

namespace SixLabors.Fonts.Tables.Cff
{
    // The Type 2 Charstring Format
    // must be used in a CFF (Compact Font Format) or OpenType font
    // file to create a complete font program
    internal readonly struct Type2Instruction
    {
#if DEBUG
        [ThreadStatic]
        private static StringBuilder? dbugSb;
        private readonly bool dbugOnlyOp;
#endif
        public readonly int Value;
        public readonly byte Op;

        public Type2Instruction(OperatorName op, int value)
        {
            this.Op = (byte)op;
            this.Value = value;
#if DEBUG
            this.dbugOnlyOp = false;
#endif
        }

        public Type2Instruction(byte op, int value)
        {
            this.Op = op;
            this.Value = value;
#if DEBUG
            this.dbugOnlyOp = false;
#endif
        }

        public Type2Instruction(OperatorName op)
        {
            this.Op = (byte)op;
            this.Value = 0;
#if DEBUG
            this.dbugOnlyOp = true;
#endif
        }

        internal bool IsLoadInt => (OperatorName)this.Op == OperatorName.LoadInt;

        public float ReadValueAsFixed1616()
        {
            byte b0 = (byte)(0xFF & (this.Value >> 24));
            byte b1 = (byte)(0xFF & (this.Value >> 16));
            byte b2 = (byte)(0xFF & (this.Value >> 8));
            byte b3 = (byte)(0xFF & (this.Value >> 0));

            // This number is interpreted as a Fixed; that is, a signed number with 16 bits of fraction
            float intPart = (short)((b0 << 8) | b1);
            float fractionPart = (short)((b2 << 8) | b3) / (float)(1 << 16);
            return intPart + fractionPart;
        }

#if DEBUG
        public override string ToString()
        {
            // upper most 2 bits we use as our extension
            int merge_flags = this.Op >> 6;

            // so operator name is lower 6 bits
            int only_operator = this.Op & 0b111111;
            var op_name = (OperatorName)only_operator;

            if (this.dbugOnlyOp)
            {
                return op_name.ToString();
            }
            else
            {
                if (dbugSb == null)
                {
                    dbugSb = new StringBuilder();
                }

                dbugSb.Length = 0; // reset

                bool has_ExtenedForm = true;

                // this is my extension
                switch (merge_flags)
                {
                    case 0:
                        // nothing
                        has_ExtenedForm = false;
                        break;
                    case 1:
                        // contains merge data for LoadInt
                        dbugSb.Append(this.Value.ToString() + " ");
                        break;
                    case 2:
                        // contains merge data for LoadShort2
                        dbugSb.Append((short)(this.Value >> 16) + " " + (short)(this.Value >> 0) + " ");
                        break;
                    case 3:
                        // contains merge data for LoadSbyte4
                        dbugSb.Append((sbyte)(this.Value >> 24) + " " + (sbyte)(this.Value >> 16) + " " + (sbyte)(this.Value >> 8) + " " + (sbyte)this.Value + " ");
                        break;
                    default:
                        throw new NotSupportedException();
                }

                switch (op_name)
                {
                    case OperatorName.LoadInt:
                        dbugSb.Append(this.Value);
                        break;
                    case OperatorName.LoadFloat:
                        dbugSb.Append(this.ReadValueAsFixed1616().ToString());
                        break;

                    case OperatorName.LoadShort2:
                        dbugSb.Append((short)(this.Value >> 16) + " " + (short)(this.Value >> 0));
                        break;
                    case OperatorName.LoadSbyte4:
                        dbugSb.Append((sbyte)(this.Value >> 24) + " " + (sbyte)(this.Value >> 16) + " " + (sbyte)(this.Value >> 8) + " " + (sbyte)this.Value);
                        break;
                    case OperatorName.LoadSbyte3:
                        dbugSb.Append((sbyte)(this.Value >> 24) + " " + (sbyte)(this.Value >> 16) + " " + (sbyte)(this.Value >> 8));
                        break;

                    case OperatorName.Hintmask1:
                    case OperatorName.Hintmask2:
                    case OperatorName.Hintmask3:
                    case OperatorName.Hintmask4:
                    case OperatorName.Hintmask_bits:
                        dbugSb.Append(op_name.ToString() + " " + Convert.ToString(this.Value, 2));
                        break;
                    default:
                        if (has_ExtenedForm)
                        {
                            dbugSb.Append(op_name.ToString());
                        }
                        else
                        {
                            dbugSb.Append(op_name.ToString() + " " + this.Value.ToString());
                        }

                        break;
                }

                return dbugSb.ToString();
            }
        }
#endif
    }
}
