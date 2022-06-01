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
        private static StringBuilder? stringBuilder;
#endif
        public readonly int Value;
        public readonly byte Operator;

        public Type2Instruction(Type2InstructionKind op, int value)
        {
            this.Operator = (byte)op;
            this.Value = value;
        }

        public Type2Instruction(Type2InstructionKind op)
        {
            this.Operator = (byte)op;
            this.Value = 0;
        }

        internal bool IsLoadInt => (Type2InstructionKind)this.Operator == Type2InstructionKind.LoadInt;

        public float ReadValueAsFixed1616()
        {
            byte b0 = (byte)(0xFF & (this.Value >> 24));
            byte b1 = (byte)(0xFF & (this.Value >> 16));
            byte b2 = (byte)(0xFF & (this.Value >> 8));
            byte b3 = (byte)(0xFF & (this.Value >> 0));

            // This number is interpreted as a Fixed; that is, a signed number with 16 bits of fraction
            float number = (short)((b0 << 8) | b1);
            float fraction = (short)((b2 << 8) | b3) / 65536F;
            return number + fraction;
        }

#if DEBUG
        public override string ToString()
        {
            // Upper most 2 bits we use as our extension
            int mergeFlags = this.Operator >> 6;

            // Operator name is lower 6 bits
            int @operator = this.Operator & 0b111111;
            var name = (Type2InstructionKind)@operator;

            if (stringBuilder == null)
            {
                stringBuilder = new StringBuilder();
            }

            stringBuilder.Length = 0; // reset

            bool has_ExtenedForm = true;

            // this is my extension
            // TODO: What is this?
            switch (mergeFlags)
            {
                case 0:
                    // nothing
                    has_ExtenedForm = false;
                    break;
                case 1:
                    // contains merge data for LoadInt
                    stringBuilder.Append(this.Value.ToString() + " ");
                    break;
                case 2:
                    // contains merge data for LoadShort2
                    stringBuilder.Append((short)(this.Value >> 16) + " " + (short)(this.Value >> 0) + " ");
                    break;
                case 3:
                    // contains merge data for LoadSbyte4
                    stringBuilder.Append((sbyte)(this.Value >> 24) + " " + (sbyte)(this.Value >> 16) + " " + (sbyte)(this.Value >> 8) + " " + (sbyte)this.Value + " ");
                    break;
                default:
                    throw new NotSupportedException();
            }

            switch (name)
            {
                case Type2InstructionKind.LoadInt:
                    stringBuilder.Append(this.Value);
                    break;
                case Type2InstructionKind.LoadFloat:
                    stringBuilder.Append(this.ReadValueAsFixed1616().ToString());
                    break;

                case Type2InstructionKind.LoadShort2:
                    stringBuilder.Append((short)(this.Value >> 16) + " " + (short)(this.Value >> 0));
                    break;
                case Type2InstructionKind.LoadSbyte4:
                    stringBuilder.Append((sbyte)(this.Value >> 24) + " " + (sbyte)(this.Value >> 16) + " " + (sbyte)(this.Value >> 8) + " " + (sbyte)this.Value);
                    break;
                case Type2InstructionKind.LoadSbyte3:
                    stringBuilder.Append((sbyte)(this.Value >> 24) + " " + (sbyte)(this.Value >> 16) + " " + (sbyte)(this.Value >> 8));
                    break;

                case Type2InstructionKind.Hintmask1:
                case Type2InstructionKind.Hintmask2:
                case Type2InstructionKind.Hintmask3:
                case Type2InstructionKind.Hintmask4:
                case Type2InstructionKind.Hintmask_bits:
                    stringBuilder.Append(name.ToString() + " " + Convert.ToString(this.Value, 2));
                    break;
                default:
                    if (has_ExtenedForm)
                    {
                        stringBuilder.Append(name.ToString());
                    }
                    else
                    {
                        stringBuilder.Append(name.ToString() + " " + this.Value.ToString());
                    }

                    break;
            }

            return stringBuilder.ToString();
        }
#endif
    }
}
