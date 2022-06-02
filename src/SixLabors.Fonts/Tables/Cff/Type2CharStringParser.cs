// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Diagnostics;

namespace SixLabors.Fonts.Tables.Cff
{
    /// <summary>
    /// Decodes the commands and numbers making up a Type 2 CharString. A Type 2 CharString extends on the Type 1 CharString format.
    /// Compared to the Type 1 format, the Type 2 encoding offers smaller size and an opportunity for better rendering quality and
    /// performance. The Type 2 charstring operators are (with one exception) a superset of the Type 1 operators.
    /// </summary>
    /// <remarks>
    /// A Type 2 charstring program is a sequence of unsigned 8-bit bytes that encode numbers and operators.
    /// The byte value specifies a operator, a number, or subsequent bytes that are to be interpreted in a specific manner
    /// </remarks>
    internal class Type2CharStringParser
    {
        private readonly byte[][] globalSubrRawBuffers;
        private readonly CffPrivateDictionary? privateDictionary;
        private bool enterPathConstructionSeq = false;
        private Type2GlyphInstructionCollection instructionList = new();
        private int currentIntegerCount = 0;
        private bool countStems = true;
        private int hintStemCount = 0;
        private bool stemFound = false;
        private FontDict? currentFontDict;
        private readonly int globalSubrBias;
        private int localSubrBias;
        private Type2InstructionKind instructionKind = Type2InstructionKind.Unknown;

        // from https://www.adobe.com/content/dam/acom/en/devnet/font/pdfs/5177.Type2.pdf
        // Type 2 Charstring Organization:
        // ...
        // The sequence and form of a Type 2 charstring program may be represented as:

        // w? {hs* vs* cm* hm* mt subpath}? {mt subpath}* endchar

        // where,
        // w= width,
        // hs = hstem or hstemhm command
        // vs = vstem or vstemhm command
        // cm = cntrmask operator
        // hm = hintmask operator
        // mt = moveto (i.e.any of the moveto) operators

        // subpath = refers to the construction of a subpath(one complete closed contour),
        // which may include hintmaskoperators where appropriate.

        //-------------
        //
        // width: If the charstring has a width other than that of defaultWidthX(see Technical Note #5176, “The Compact Font Format Specification”),
        // it must be specified as the first number in the charstring,
        // and encoded as the difference from nominalWidthX
        public Type2CharStringParser(byte[][] globalSubrRawBuffers, CffPrivateDictionary? privateDictionary)
        {
            this.globalSubrRawBuffers = globalSubrRawBuffers;
            this.privateDictionary = privateDictionary;

            if (globalSubrRawBuffers.Length > 0)
            {
                this.globalSubrBias = CalculateBias(globalSubrRawBuffers.Length);
            }

            if (this.privateDictionary?.LocalSubrRawBuffers.Length > 0)
            {
                this.localSubrBias = CalculateBias(this.privateDictionary.LocalSubrRawBuffers.Length);
            }
        }

        private static int CalculateBias(int subCount)
            => (subCount < 1240) ? 107 : (subCount < 33900) ? 1131 : 32768;

        private void Parse(ReadOnlySpan<byte> buffer)
        {
            byte b0 = 0;
            bool endCharEncountered = false;
            SimpleBinaryReader reader = new(buffer);
            while (!endCharEncountered && reader.CanRead())
            {
                b0 = reader.ReadByte();
                switch (b0)
                {
                    default: // else 32 -255

                        if (b0 < 32)
                        {
                            Debug.WriteLine("err!:" + b0);
                            return;
                        }

                        this.instructionList.AddInt(ReadIntegerNumber(ref reader, b0));
                        if (this.countStems)
                        {
                            this.currentIntegerCount++;
                        }

                        break;
                    case 255:

                        // If the charstring byte contains the value 255,
                        // the next four bytes indicate a two’s complement signed number.
                        //
                        // The first of these four bytes contains the highest order bits,
                        // he second byte contains the next higher order bits and
                        // the fourth byte contains the lowest order bits.
                        //
                        // eg. found in font Asana Math regular, glyph_index: 114 , 292, 1070 etc.
                        this.instructionList.AddFloatFixed1616(reader.ReadFloatFixed1616());

                        if (this.countStems)
                        {
                            this.currentIntegerCount++;
                        }

                        break;
                    case (byte)Type2Operator1.Shortint: // 28

                        // shortint
                        // First byte of a 3-byte sequence specifying a number.
                        // a ShortInt value is specified by using the operator (28) followed by two bytes
                        // which represent numbers between –32768 and + 32767.The
                        // most significant byte follows the(28)
                        byte sb0 = reader.ReadByte();
                        byte sb1 = reader.ReadByte();
                        this.instructionList.AddInt((short)((sb0 << 8) | sb1));

                        if (this.countStems)
                        {
                            this.currentIntegerCount++;
                        }

                        break;

                    //---------------------------------------------------
                    case (byte)Type2Operator1.Reserved0_: // ???
                    case (byte)Type2Operator1.Reserved2_: // ???
                    case (byte)Type2Operator1.Reserved9_: // ???
                    case (byte)Type2Operator1.Reserved13_: // ???
                    case (byte)Type2Operator1.Reserved15_: // ???
                    case (byte)Type2Operator1.Reserved16_: // ???
                    case (byte)Type2Operator1.Reserved17_: // ???
                        // reserved, do nothing ?
                        break;

                    case (byte)Type2Operator1.Escape: // 12
                        b0 = reader.ReadByte();
                        switch ((Type2Operator2)b0)
                        {
                            default:
                                if (b0 <= 38)
                                {
                                    Debug.WriteLine("err!:" + b0);
                                    return;
                                }

                                break;

                            //-------------------------
                            // 4.1: Path Construction Operators
                            case Type2Operator2.Flex:
                                this.instructionList.AddOperator(Type2InstructionKind.Flex);
                                break;
                            case Type2Operator2.Hflex:
                                this.instructionList.AddOperator(Type2InstructionKind.Hflex);
                                break;
                            case Type2Operator2.Hflex1:
                                this.instructionList.AddOperator(Type2InstructionKind.Hflex1);
                                break;
                            case Type2Operator2.Flex1:
                                this.instructionList.AddOperator(Type2InstructionKind.Flex1);
                                break;

                            //-------------------------
                            // 4.4: Arithmetic Operators
                            case Type2Operator2.Abs:
                                this.instructionList.AddOperator(Type2InstructionKind.Abs);
                                break;
                            case Type2Operator2.Add:
                                this.instructionList.AddOperator(Type2InstructionKind.Add);
                                break;
                            case Type2Operator2.Sub:
                                this.instructionList.AddOperator(Type2InstructionKind.Sub);
                                break;
                            case Type2Operator2.Div:
                                this.instructionList.AddOperator(Type2InstructionKind.Div);
                                break;
                            case Type2Operator2.Neg:
                                this.instructionList.AddOperator(Type2InstructionKind.Neg);
                                break;
                            case Type2Operator2.Random:
                                this.instructionList.AddOperator(Type2InstructionKind.Random);
                                break;
                            case Type2Operator2.Mul:
                                this.instructionList.AddOperator(Type2InstructionKind.Mul);
                                break;
                            case Type2Operator2.Sqrt:
                                this.instructionList.AddOperator(Type2InstructionKind.Sqrt);
                                break;
                            case Type2Operator2.Drop:
                                this.instructionList.AddOperator(Type2InstructionKind.Drop);
                                break;
                            case Type2Operator2.Exch:
                                this.instructionList.AddOperator(Type2InstructionKind.Exch);
                                break;
                            case Type2Operator2.Index:
                                this.instructionList.AddOperator(Type2InstructionKind.Index);
                                break;
                            case Type2Operator2.Roll:
                                this.instructionList.AddOperator(Type2InstructionKind.Roll);
                                break;
                            case Type2Operator2.Dup:
                                this.instructionList.AddOperator(Type2InstructionKind.Dup);
                                break;

                            //-------------------------
                            // 4.5: Storage Operators
                            case Type2Operator2.Put:
                                this.instructionList.AddOperator(Type2InstructionKind.Put);
                                break;
                            case Type2Operator2.Get:
                                this.instructionList.AddOperator(Type2InstructionKind.Get);
                                break;

                            //-------------------------
                            // 4.6: Conditional
                            case Type2Operator2.And:
                                this.instructionList.AddOperator(Type2InstructionKind.And);
                                break;
                            case Type2Operator2.Or:
                                this.instructionList.AddOperator(Type2InstructionKind.Or);
                                break;
                            case Type2Operator2.Not:
                                this.instructionList.AddOperator(Type2InstructionKind.Not);
                                break;
                            case Type2Operator2.Eq:
                                this.instructionList.AddOperator(Type2InstructionKind.Eq);
                                break;
                            case Type2Operator2.Ifelse:
                                this.instructionList.AddOperator(Type2InstructionKind.Ifelse);
                                break;
                        }

                        this.StopStemCount();

                        break;

                    //---------------------------------------------------------------------------
                    case (byte)Type2Operator1.Endchar:
                        this.AddEndCharOp();
                        endCharEncountered = false;

                        // when we found end char
                        // stop reading this...
                        break;
                    case (byte)Type2Operator1.Rmoveto:
                        this.AddMoveToOp(Type2InstructionKind.Rmoveto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Hmoveto:
                        this.AddMoveToOp(Type2InstructionKind.Hmoveto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Vmoveto:
                        this.AddMoveToOp(Type2InstructionKind.Vmoveto);
                        this.StopStemCount();
                        break;

                    //---------------------------------------------------------------------------
                    case (byte)Type2Operator1.Rlineto:
                        this.instructionList.AddOperator(Type2InstructionKind.Rlineto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Hlineto:
                        this.instructionList.AddOperator(Type2InstructionKind.Hlineto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Vlineto:
                        this.instructionList.AddOperator(Type2InstructionKind.Vlineto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Rrcurveto:
                        this.instructionList.AddOperator(Type2InstructionKind.Rrcurveto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Hhcurveto:
                        this.instructionList.AddOperator(Type2InstructionKind.Hhcurveto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Hvcurveto:
                        this.instructionList.AddOperator(Type2InstructionKind.Hvcurveto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Rcurveline:
                        this.instructionList.AddOperator(Type2InstructionKind.Rcurveline);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Rlinecurve:
                        this.instructionList.AddOperator(Type2InstructionKind.Rlinecurve);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Vhcurveto:
                        this.instructionList.AddOperator(Type2InstructionKind.Vhcurveto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Vvcurveto:
                        this.instructionList.AddOperator(Type2InstructionKind.Vvcurveto);
                        this.StopStemCount();
                        break;

                    //-------------------------------------------------------------------
                    // 4.3 Hint Operators
                    case (byte)Type2Operator1.Hstem:
                        this.AddStemToList(Type2InstructionKind.Hstem);
                        break;
                    case (byte)Type2Operator1.Vstem:
                        this.AddStemToList(Type2InstructionKind.Vstem);
                        break;
                    case (byte)Type2Operator1.Vstemhm:
                        this.AddStemToList(Type2InstructionKind.Vstemhm);
                        break;
                    case (byte)Type2Operator1.Hstemhm:
                        this.AddStemToList(Type2InstructionKind.Hstemhm);
                        break;

                    //-------------------------------------------------------------------
                    case (byte)Type2Operator1.Hintmask:
                        this.AddHintMaskToList(ref reader);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Cntrmask:
                        this.AddCounterMaskToList(ref reader);
                        this.StopStemCount();
                        break;

                    //-------------------------------------------------------------------
                    // 4.7: Subroutine Operators
                    case (byte)Type2Operator1.Return:
#if DEBUG
                        if (reader.CanRead())
                        {
                            throw new NotSupportedException();
                        }
#endif
                        return;

                    //-------------------------------------------------------------------
                    case (byte)Type2Operator1.Callsubr:

                        // get local subr proc
                        Type2Instruction inst = this.instructionList.RemoveLast();
                        if (!inst.IsLoadInt)
                        {
                            throw new NotSupportedException();
                        }

                        if (this.countStems)
                        {
                            this.currentIntegerCount--;
                        }

                        // subr_no must be adjusted with proper bias value
                        if (this.privateDictionary?.LocalSubrRawBuffers.Length > 0)
                        {
                            this.Parse(this.privateDictionary.LocalSubrRawBuffers[inst.Value + this.localSubrBias]);
                        }
                        else if (this.currentFontDict?.LocalSubr != null)
                        {
                            // use private dict
                            this.Parse(this.currentFontDict.LocalSubr[inst.Value + this.localSubrBias]);
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }

                        break;
                    case (byte)Type2Operator1.Callgsubr:

                        Type2Instruction inst2 = this.instructionList.RemoveLast();
                        if (!inst2.IsLoadInt)
                        {
                            throw new NotSupportedException();
                        }

                        if (this.countStems)
                        {
                            this.currentIntegerCount--;
                        }

                        // subr_no must be adjusted with proper bias value
                        // load global subr
                        this.Parse(this.globalSubrRawBuffers[inst2.Value + this.globalSubrBias]);

                        break;
                }
            }
        }

        public void SetCidFontDict(FontDict fontdic)
        {
            this.currentFontDict = fontdic;
            if (fontdic.LocalSubr != null)
            {
                this.localSubrBias = CalculateBias(fontdic.LocalSubr.Length);
            }
            else
            {
                this.localSubrBias = 0;
            }
        }

        public Type2GlyphInstructionCollection ParseType2CharString(ReadOnlySpan<byte> buffer)
        {
            // Reset
            this.hintStemCount = 0;
            this.currentIntegerCount = 0;
            this.stemFound = false;
            this.enterPathConstructionSeq = false;
            this.countStems = true;
            this.instructionList = new Type2GlyphInstructionCollection();

            this.Parse(buffer);
            return this.instructionList;
        }

        private void StopStemCount()
        {
            this.currentIntegerCount = 0;
            this.countStems = false;
        }

        private void AddEndCharOp()
        {
            // from https://www.adobe.com/content/dam/acom/en/devnet/font/pdfs/5177.Type2.pdf
            // Note 4 The first stack - clearing operator, which must be one of
            // hstem, hstemhm, vstem, vstemhm,
            // cntrmask, hintmask,
            // hmoveto, vmoveto, rmoveto,
            // or endchar,
            // takes an additional argument — the width(as described earlier), which may be expressed as zero or one numeric argument
            if (!this.stemFound && !this.enterPathConstructionSeq)
            {
                if (this.instructionList.Count > 0)
                {
                    this.instructionList.ChangeFirstInstToGlyphWidthValue();
                }
            }

            // takes an additional argument — the width(as described earlier), which may be expressed as zero or one numeric argument
            this.instructionList.AddOperator(Type2InstructionKind.Endchar);
        }

        /// <summary>
        /// for hmoveto, vmoveto, rmoveto
        /// </summary>
        /// <param name="op">The operator name.</param>
        private void AddMoveToOp(Type2InstructionKind op)
        {
            // from https://www.adobe.com/content/dam/acom/en/devnet/font/pdfs/5177.Type2.pdf
            // Note 4 The first stack - clearing operator, which must be one of
            // hstem, hstemhm, vstem, vstemhm,
            // cntrmask, hintmask,
            // hmoveto, vmoveto, rmoveto,
            // or endchar,
            // takes an additional argument — the width(as described earlier), which may be expressed as zero or one numeric argument
            // just add
            if (!this.stemFound && !this.enterPathConstructionSeq)
            {
                if (op == Type2InstructionKind.Rmoveto)
                {
                    if ((this.instructionList.Count % 2) != 0)
                    {
                        this.instructionList.ChangeFirstInstToGlyphWidthValue();
                    }
                }
                else
                {
                    // vmoveto, hmoveto
                    if (this.instructionList.Count > 1)
                    {
                        // ...
                        this.instructionList.ChangeFirstInstToGlyphWidthValue();
                    }
                }
            }

            this.enterPathConstructionSeq = true;
            this.instructionList.AddOperator(op);
        }

        /// <summary>
        /// for hstem, hstemhm, vstem, vstemhm
        /// </summary>
        /// <param name="kind">The instruction operator.</param>
        private void AddStemToList(Type2InstructionKind kind)
        {
            // from https://www.adobe.com/content/dam/acom/en/devnet/font/pdfs/5177.Type2.pdf
            // Note 4 The first stack - clearing operator, which must be one of
            // hstem, hstemhm, vstem, vstemhm,
            // cntrmask, hintmask,
            // hmoveto, vmoveto, rmoveto,
            // or endchar,
            // takes an additional argument — the width(as described earlier), which may be expressed as zero or one numeric argument

            // support 4 kinds

            // 1.
            // |- y dy {dya dyb}*  hstemhm (18) |-
            // 2.
            // |- x dx {dxa dxb}* vstemhm (23) |-
            // 3.
            // |- y dy {dya dyb}*  hstem (1) |-
            // 4.
            // |- x dx {dxa dxb}*  vstem (3) |-
            //-----------------------

            // notes
            // The sequence and form of a Type 2 charstring program may be
            // represented as:
            // w? { hs* vs*cm * hm * mt subpath}? { mt subpath} *endchar
            if ((this.currentIntegerCount % 2) != 0)
            {
                // all kind has even number of stem
                if (this.stemFound)
                {
                    throw new NotSupportedException();
                }
                else
                {
                    // the first one is 'width'
                    this.instructionList.ChangeFirstInstToGlyphWidthValue();
                    this.currentIntegerCount--;
                }
            }

            this.hintStemCount += this.currentIntegerCount / 2; // save a snapshot of stem count
            this.instructionList.AddOperator(kind);
            this.currentIntegerCount = 0; // clear
            this.stemFound = true;
            this.instructionKind = kind;
        }

        /// <summary>
        /// add hintmask
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        private void AddHintMaskToList(ref SimpleBinaryReader reader)
        {
            if (this.stemFound && this.currentIntegerCount > 0)
            {
                // type2 5177.pdf
                // ...
                // If hstem and vstem hints are both declared at the beginning of
                // a charstring, and this sequence is followed directly by the
                // hintmask or cntrmask operators, ...
                // the vstem hint operator need not be included ***
#if DEBUG
                if ((this.currentIntegerCount % 2) != 0)
                {
                    throw new NotSupportedException();
                }
#endif
                if (this.countStems)
                {
                    switch (this.instructionKind)
                    {
                        case Type2InstructionKind.Hstem:
                            // add vstem  ***( from reason above)
                            this.hintStemCount += this.currentIntegerCount / 2; // save a snapshot of stem count
                            this.instructionList.AddOperator(Type2InstructionKind.Vstem);
                            this.instructionKind = Type2InstructionKind.Vstem;
                            this.currentIntegerCount = 0; // clear
                            break;
                        case Type2InstructionKind.Hstemhm:
                            // add vstem  ***( from reason above) ??
                            this.hintStemCount += this.currentIntegerCount / 2; // save a snapshot of stem count
                            this.instructionList.AddOperator(Type2InstructionKind.Vstem);
                            this.instructionKind = Type2InstructionKind.Vstem;
                            this.currentIntegerCount = 0; // clear
                            break;
                        case Type2InstructionKind.Vstemhm:
                            //-------
                            // TODO: review here?
                            // found this in xits.otf
                            this.hintStemCount += this.currentIntegerCount / 2; // save a snapshot of stem count
                            this.instructionList.AddOperator(Type2InstructionKind.Vstem);
                            this.instructionKind = Type2InstructionKind.Vstem;
                            this.currentIntegerCount = 0; // clear
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }
                else
                {
                }
            }

            if (this.hintStemCount == 0)
            {
                if (!this.stemFound)
                {
                    this.hintStemCount = this.currentIntegerCount / 2;
                    if (this.hintStemCount == 0)
                    {
                        return;
                    }

                    this.stemFound = true; // ?
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            //----------------------
            // this is my hintmask extension, => to fit with our Evaluation stack
            int properNumberOfMaskBytes = (this.hintStemCount + 7) / 8;

            if (reader.Position + properNumberOfMaskBytes >= reader.Length)
            {
                throw new NotSupportedException();
            }

            if (properNumberOfMaskBytes > 4)
            {
                int remaining = properNumberOfMaskBytes;

                for (; remaining > 3;)
                {
                    this.instructionList.AddInt(
                       (reader.ReadByte() << 24) |
                       (reader.ReadByte() << 16) |
                       (reader.ReadByte() << 8) |
                       reader.ReadByte());
                    remaining -= 4; // ***
                }

                switch (remaining)
                {
                    case 0:
                        // do nothing
                        break;
                    case 1:
                        this.instructionList.AddInt(reader.ReadByte() << 24);
                        break;
                    case 2:
                        this.instructionList.AddInt(
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16));

                        break;
                    case 3:
                        this.instructionList.AddInt(
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16) |
                            (reader.ReadByte() << 8));
                        break;
                    default:
                        throw new NotSupportedException(); // should not occur !
                }

                this.instructionList.AddOperator(Type2InstructionKind.Hintmask_bits, properNumberOfMaskBytes);
            }
            else
            {
                // Last remaining < 4 bytes
                switch (properNumberOfMaskBytes)
                {
                    case 0:
                    default:
                        throw new NotSupportedException(); // should not occur !
                    case 1:
                        this.instructionList.AddOperator(Type2InstructionKind.Hintmask1, reader.ReadByte() << 24);
                        break;
                    case 2:
                        this.instructionList.AddOperator(Type2InstructionKind.Hintmask2, (reader.ReadByte() << 24) | (reader.ReadByte() << 16));
                        break;
                    case 3:
                        this.instructionList.AddOperator(Type2InstructionKind.Hintmask3, (reader.ReadByte() << 24) | (reader.ReadByte() << 16) | (reader.ReadByte() << 8));
                        break;
                    case 4:
                        this.instructionList.AddOperator(Type2InstructionKind.Hintmask4, (reader.ReadByte() << 24) | (reader.ReadByte() << 16) | (reader.ReadByte() << 8) | reader.ReadByte());
                        break;
                }
            }
        }

        /// <summary>
        /// cntrmask
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        private void AddCounterMaskToList(ref SimpleBinaryReader reader)
        {
            if (this.hintStemCount == 0)
            {
                if (!this.stemFound)
                {
                    // ????
                    this.hintStemCount = this.currentIntegerCount / 2;
                    this.stemFound = true; // ?
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else
            {
                this.hintStemCount += this.currentIntegerCount / 2;
            }

            //----------------------
            // This is my hintmask extension, => to fit with our Evaluation stack
            int properNumberOfMaskBytes = (this.hintStemCount + 7) / 8;
            if (reader.Position + properNumberOfMaskBytes >= reader.Length)
            {
                throw new NotSupportedException();
            }

            if (properNumberOfMaskBytes > 4)
            {
                int remaining = properNumberOfMaskBytes;

                for (; remaining > 3;)
                {
                    this.instructionList.AddInt(
                       (reader.ReadByte() << 24) |
                       (reader.ReadByte() << 16) |
                       (reader.ReadByte() << 8) |
                       reader.ReadByte());
                    remaining -= 4; // ***
                }

                switch (remaining)
                {
                    case 0:
                        // do nothing
                        break;
                    case 1:
                        this.instructionList.AddInt(reader.ReadByte() << 24);
                        break;
                    case 2:
                        this.instructionList.AddInt(
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16));

                        break;
                    case 3:
                        this.instructionList.AddInt(
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16) |
                            (reader.ReadByte() << 8));
                        break;
                    default:
                        throw new NotSupportedException(); // should not occur !
                }

                this.instructionList.AddOperator(Type2InstructionKind.Cntrmask_bits, properNumberOfMaskBytes);
            }
            else
            {
                // last remaining < 4 bytes
                switch (properNumberOfMaskBytes)
                {
                    case 0:
                    default:
                        throw new NotSupportedException(); // should not occur !
                    case 1:
                        this.instructionList.AddOperator(Type2InstructionKind.Cntrmask1, reader.ReadByte() << 24);
                        break;
                    case 2:
                        this.instructionList.AddOperator(
                            Type2InstructionKind.Cntrmask2,
                            (reader.ReadByte() << 24) | (reader.ReadByte() << 16));
                        break;
                    case 3:
                        this.instructionList.AddOperator(
                            Type2InstructionKind.Cntrmask3,
                            (reader.ReadByte() << 24) | (reader.ReadByte() << 16) | (reader.ReadByte() << 8));
                        break;
                    case 4:
                        this.instructionList.AddOperator(
                            Type2InstructionKind.Cntrmask4,
                            (reader.ReadByte() << 24) | (reader.ReadByte() << 16) | (reader.ReadByte() << 8) | reader.ReadByte());
                        break;
                }
            }
        }

        private static int ReadIntegerNumber(ref SimpleBinaryReader reader, byte b0)
        {
            if (b0 is >= 32 and <= 246)
            {
                return b0 - 139;
            }
            else if (b0 <= 250)
            {
                // && b0 >= 247 , *** if-else sequence is important! ***
                byte b1 = reader.ReadByte();
                return ((b0 - 247) * 256) + b1 + 108;
            }
            else if (b0 <= 254)
            {
                // &&  b0 >= 251 ,*** if-else sequence is important! ***
                byte b1 = reader.ReadByte();
                return (-(b0 - 251) * 256) - b1 - 108;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private ref struct SimpleBinaryReader
        {
            private readonly ReadOnlySpan<byte> buffer;

            public SimpleBinaryReader(ReadOnlySpan<byte> buffer)
            {
                this.buffer = buffer;
                this.Position = 0;
            }

            public int Length => this.buffer.Length;

            public int Position { get; private set; }

            public bool CanRead() => (uint)this.Position < this.buffer.Length;

            public byte ReadByte() => this.buffer[this.Position++];

            public int ReadFloatFixed1616()
            {
                // Read a BE int, we parse it later.
                byte b3 = this.buffer[this.Position + 3];
                byte b2 = this.buffer[this.Position + 2];
                byte b1 = this.buffer[this.Position + 1];
                byte b0 = this.buffer[this.Position];
                this.Position += 4;

                return (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
            }
        }
    }
}
