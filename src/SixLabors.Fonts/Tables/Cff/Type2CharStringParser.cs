// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Diagnostics;

namespace SixLabors.Fonts.Tables.Cff
{
    internal class Type2CharStringParser
    {
        private readonly byte[][] globalSubrRawBuffers;
        private readonly CffPrivateDictionary? privateDictionary;
#if DEBUG
        private int dbugCount = 0;
        private int dbugInstructionListMark = 0;
#endif
        private int hintStemCount = 0;
        private bool foundSomeStem = false;
        private bool enterPathConstructionSeq = false;
        private Type2GlyphInstructionList instructionList = new();
        private int currentIntegerCount = 0;
        private bool doStemCount = true;
        private FontDict? currentFontDict;
        private readonly int globalSubrBias;
        private int localSubrBias;
        private OperatorName latestOpName = OperatorName.Unknown;

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

#if DEBUG
        public ushort DbugCurrentGlyphIndex { get; set; }
#endif

        //-------------
        // from Technical Note #5176 (CFF spec)
        // resolve with bias
        // Card16 bias;
        // Card16 nSubrs = subrINDEX.count;
        // if (CharstringType == 1)
        //    bias = 0;
        // else if (nSubrs < 1240)
        //    bias = 107;
        // else if (nSubrs < 33900)
        //    bias = 1131;
        // else
        //    bias = 32768;
        // find local subroutine
        private static int CalculateBias(int nsubr) => (nsubr < 1240) ? 107 : (nsubr < 33900) ? 1131 : 32768;

        private void ParseType2CharStringBuffer(byte[] buffer)
        {
            byte b0 = 0;

            bool cont = true;

            var reader = new SimpleBinaryReader(buffer);
            while (cont && !reader.IsEnd())
            {
                b0 = reader.ReadByte();
#if DEBUG
                // easy for debugging here
                this.dbugCount++;
                if (b0 < 32)
                {
                }
#endif
                switch (b0)
                {
                    default: // else 32 -255

                        if (b0 < 32)
                        {
                            Debug.WriteLine("err!:" + b0);
                            return;
                        }

                        this.instructionList.AddInt(ReadIntegerNumber(ref reader, b0));
                        if (this.doStemCount)
                        {
                            this.currentIntegerCount++;
                        }

                        break;
                    case 255:

                        // from https://www.adobe.com/content/dam/acom/en/devnet/font/pdfs/5177.Type2.pdf
                        // If the charstring byte contains the value 255,
                        // the next four bytes indicate a two’s complement signed number.

                        // The first of these four bytes contains the highest order bits,
                        // he second byte contains the next higher order bits and
                        // the fourth byte contains the lowest order bits.

                        // eg. found in font Asana Math regular, glyph_index: 114 , 292, 1070 etc.
                        this.instructionList.AddFloat(reader.ReadFloatFixed1616());

                        if (this.doStemCount)
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

                        if (this.doStemCount)
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
                                this.instructionList.AddOp(OperatorName.Flex);
                                break;
                            case Type2Operator2.Hflex:
                                this.instructionList.AddOp(OperatorName.Hflex);
                                break;
                            case Type2Operator2.Hflex1:
                                this.instructionList.AddOp(OperatorName.Hflex1);
                                break;
                            case Type2Operator2.Flex1:
                                this.instructionList.AddOp(OperatorName.Flex1);
                                break;

                            //-------------------------
                            // 4.4: Arithmetic Operators
                            case Type2Operator2.Abs:
                                this.instructionList.AddOp(OperatorName.Abs);
                                break;
                            case Type2Operator2.Add:
                                this.instructionList.AddOp(OperatorName.Add);
                                break;
                            case Type2Operator2.Sub:
                                this.instructionList.AddOp(OperatorName.Sub);
                                break;
                            case Type2Operator2.Div:
                                this.instructionList.AddOp(OperatorName.Div);
                                break;
                            case Type2Operator2.Neg:
                                this.instructionList.AddOp(OperatorName.Neg);
                                break;
                            case Type2Operator2.Random:
                                this.instructionList.AddOp(OperatorName.Random);
                                break;
                            case Type2Operator2.Mul:
                                this.instructionList.AddOp(OperatorName.Mul);
                                break;
                            case Type2Operator2.Sqrt:
                                this.instructionList.AddOp(OperatorName.Sqrt);
                                break;
                            case Type2Operator2.Drop:
                                this.instructionList.AddOp(OperatorName.Drop);
                                break;
                            case Type2Operator2.Exch:
                                this.instructionList.AddOp(OperatorName.Exch);
                                break;
                            case Type2Operator2.Index:
                                this.instructionList.AddOp(OperatorName.Index);
                                break;
                            case Type2Operator2.Roll:
                                this.instructionList.AddOp(OperatorName.Roll);
                                break;
                            case Type2Operator2.Dup:
                                this.instructionList.AddOp(OperatorName.Dup);
                                break;

                            //-------------------------
                            // 4.5: Storage Operators
                            case Type2Operator2.Put:
                                this.instructionList.AddOp(OperatorName.Put);
                                break;
                            case Type2Operator2.Get:
                                this.instructionList.AddOp(OperatorName.Get);
                                break;

                            //-------------------------
                            // 4.6: Conditional
                            case Type2Operator2.And:
                                this.instructionList.AddOp(OperatorName.And);
                                break;
                            case Type2Operator2.Or:
                                this.instructionList.AddOp(OperatorName.Or);
                                break;
                            case Type2Operator2.Not:
                                this.instructionList.AddOp(OperatorName.Not);
                                break;
                            case Type2Operator2.Eq:
                                this.instructionList.AddOp(OperatorName.Eq);
                                break;
                            case Type2Operator2.Ifelse:
                                this.instructionList.AddOp(OperatorName.Ifelse);
                                break;
                        }

                        this.StopStemCount();

                        break;

                    //---------------------------------------------------------------------------
                    case (byte)Type2Operator1.Endchar:
                        this.AddEndCharOp();
                        cont = false;

                        // when we found end char
                        // stop reading this...
                        break;
                    case (byte)Type2Operator1.Rmoveto:
                        this.AddMoveToOp(OperatorName.Rmoveto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Hmoveto:
                        this.AddMoveToOp(OperatorName.Hmoveto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Vmoveto:
                        this.AddMoveToOp(OperatorName.Vmoveto);
                        this.StopStemCount();
                        break;

                    //---------------------------------------------------------------------------
                    case (byte)Type2Operator1.Rlineto:
                        this.instructionList.AddOp(OperatorName.Rlineto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Hlineto:
                        this.instructionList.AddOp(OperatorName.Hlineto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Vlineto:
                        this.instructionList.AddOp(OperatorName.Vlineto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Rrcurveto:
                        this.instructionList.AddOp(OperatorName.Rrcurveto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Hhcurveto:
                        this.instructionList.AddOp(OperatorName.Hhcurveto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Hvcurveto:
                        this.instructionList.AddOp(OperatorName.Hvcurveto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Rcurveline:
                        this.instructionList.AddOp(OperatorName.Rcurveline);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Rlinecurve:
                        this.instructionList.AddOp(OperatorName.Rlinecurve);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Vhcurveto:
                        this.instructionList.AddOp(OperatorName.Vhcurveto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.Vvcurveto:
                        this.instructionList.AddOp(OperatorName.Vvcurveto);
                        this.StopStemCount();
                        break;

                    //-------------------------------------------------------------------
                    // 4.3 Hint Operators
                    case (byte)Type2Operator1.Hstem:
                        this.AddStemToList(OperatorName.Hstem);
                        break;
                    case (byte)Type2Operator1.Vstem:
                        this.AddStemToList(OperatorName.Vstem);
                        break;
                    case (byte)Type2Operator1.Vstemhm:
                        this.AddStemToList(OperatorName.Vstemhm);
                        break;
                    case (byte)Type2Operator1.Hstemhm:
                        this.AddStemToList(OperatorName.Hstemhm);
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
                        if (!reader.IsEnd())
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

                        if (this.doStemCount)
                        {
                            this.currentIntegerCount--;
                        }

                        // subr_no must be adjusted with proper bias value
                        if (this.privateDictionary?.LocalSubrRawBuffers.Length > 0)
                        {
                            this.ParseType2CharStringBuffer(this.privateDictionary.LocalSubrRawBuffers[inst.Value + this.localSubrBias]);
                        }
                        else if (this.currentFontDict?.LocalSubr != null)
                        {
                            // use private dict
                            this.ParseType2CharStringBuffer(this.currentFontDict.LocalSubr[inst.Value + this.localSubrBias]);
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

                        if (this.doStemCount)
                        {
                            this.currentIntegerCount--;
                        }

                        // subr_no must be adjusted with proper bias value
                        // load global subr
                        this.ParseType2CharStringBuffer(this.globalSubrRawBuffers[inst2.Value + this.globalSubrBias]);

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

        public Type2GlyphInstructionList ParseType2CharString(byte[] buffer)
        {
            // Reset
            this.hintStemCount = 0;
            this.currentIntegerCount = 0;
            this.foundSomeStem = false;
            this.enterPathConstructionSeq = false;
            this.doStemCount = true;
            this.instructionList = new Type2GlyphInstructionList();

            //--------------------
#if DEBUG
            this.dbugInstructionListMark++;

            this.instructionList.dbugGlyphIndex = this.DbugCurrentGlyphIndex;

            if (this.DbugCurrentGlyphIndex == 496)
            {
            }
#endif
            this.ParseType2CharStringBuffer(buffer);

#if DEBUG
            if (this.DbugCurrentGlyphIndex == 496)
            {
                // _insts.dbugDumpInstructionListToFile("glyph_496.txt");
            }
#endif
            return this.instructionList;
        }

        private void StopStemCount()
        {
            this.currentIntegerCount = 0;
            this.doStemCount = false;
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
            if (!this.foundSomeStem && !this.enterPathConstructionSeq)
            {
                if (this.instructionList.Count > 0)
                {
                    this.instructionList.ChangeFirstInstToGlyphWidthValue();
                }
            }

            // takes an additional argument — the width(as described earlier), which may be expressed as zero or one numeric argument
            this.instructionList.AddOp(OperatorName.Endchar);
        }

        /// <summary>
        /// for hmoveto, vmoveto, rmoveto
        /// </summary>
        /// <param name="op">The operator name.</param>
        private void AddMoveToOp(OperatorName op)
        {
            // from https://www.adobe.com/content/dam/acom/en/devnet/font/pdfs/5177.Type2.pdf
            // Note 4 The first stack - clearing operator, which must be one of
            // hstem, hstemhm, vstem, vstemhm,
            // cntrmask, hintmask,
            // hmoveto, vmoveto, rmoveto,
            // or endchar,
            // takes an additional argument — the width(as described earlier), which may be expressed as zero or one numeric argument
            // just add
            if (!this.foundSomeStem && !this.enterPathConstructionSeq)
            {
                if (op == OperatorName.Rmoveto)
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
            this.instructionList.AddOp(op);
        }

        /// <summary>
        /// for hstem, hstemhm, vstem, vstemhm
        /// </summary>
        /// <param name="stemName">The operator name.</param>
        private void AddStemToList(OperatorName stemName)
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
                if (this.foundSomeStem)
                {
#if DEBUG
                    this.instructionList.dbugDumpInstructionListToFile("test_type2_" + (this.dbugInstructionListMark - 1) + ".txt");
#endif
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
            this.instructionList.AddOp(stemName);
            this.currentIntegerCount = 0; // clear
            this.foundSomeStem = true;
            this.latestOpName = stemName;
        }

        /// <summary>
        /// add hintmask
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        private void AddHintMaskToList(ref SimpleBinaryReader reader)
        {
            if (this.foundSomeStem && this.currentIntegerCount > 0)
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
                else
                {
                }
#endif
                if (this.doStemCount)
                {
                    switch (this.latestOpName)
                    {
                        case OperatorName.Hstem:
                            // add vstem  ***( from reason above)
                            this.hintStemCount += this.currentIntegerCount / 2; // save a snapshot of stem count
                            this.instructionList.AddOp(OperatorName.Vstem);
                            this.latestOpName = OperatorName.Vstem;
                            this.currentIntegerCount = 0; // clear
                            break;
                        case OperatorName.Hstemhm:
                            // add vstem  ***( from reason above) ??
                            this.hintStemCount += this.currentIntegerCount / 2; // save a snapshot of stem count
                            this.instructionList.AddOp(OperatorName.Vstem);
                            this.latestOpName = OperatorName.Vstem;
                            this.currentIntegerCount = 0; // clear
                            break;
                        case OperatorName.Vstemhm:
                            //-------
                            // TODO: review here?
                            // found this in xits.otf
                            this.hintStemCount += this.currentIntegerCount / 2; // save a snapshot of stem count
                            this.instructionList.AddOp(OperatorName.Vstem);
                            this.latestOpName = OperatorName.Vstem;
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
                if (!this.foundSomeStem)
                {
                    this.hintStemCount = this.currentIntegerCount / 2;
                    if (this.hintStemCount == 0)
                    {
                        return;
                    }

                    this.foundSomeStem = true; // ?
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            //----------------------
            // this is my hintmask extension, => to fit with our Evaluation stack
            int properNumberOfMaskBytes = (this.hintStemCount + 7) / 8;

            if (reader.Position + properNumberOfMaskBytes >= reader.BufferLength)
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

                this.instructionList.AddOp(OperatorName.Hintmask_bits, properNumberOfMaskBytes);
            }
            else
            {
                // last remaining <4 bytes
                switch (properNumberOfMaskBytes)
                {
                    case 0:
                    default:
                        throw new NotSupportedException(); // should not occur !
                    case 1:
                        this.instructionList.AddOp(OperatorName.Hintmask1, reader.ReadByte() << 24);
                        break;
                    case 2:
                        this.instructionList.AddOp(OperatorName.Hintmask2, (reader.ReadByte() << 24) | (reader.ReadByte() << 16));
                        break;
                    case 3:
                        this.instructionList.AddOp(OperatorName.Hintmask3, (reader.ReadByte() << 24) | (reader.ReadByte() << 16) | (reader.ReadByte() << 8));
                        break;
                    case 4:
                        this.instructionList.AddOp(OperatorName.Hintmask4, (reader.ReadByte() << 24) | (reader.ReadByte() << 16) | (reader.ReadByte() << 8) | reader.ReadByte());
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
                if (!this.foundSomeStem)
                {
                    // ????
                    this.hintStemCount = this.currentIntegerCount / 2;
                    this.foundSomeStem = true; // ?
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
            // this is my hintmask extension, => to fit with our Evaluation stack
            int properNumberOfMaskBytes = (this.hintStemCount + 7) / 8;
            if (reader.Position + properNumberOfMaskBytes >= reader.BufferLength)
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

                this.instructionList.AddOp(OperatorName.Cntrmask_bits, properNumberOfMaskBytes);
            }
            else
            {
                // last remaining <4 bytes
                switch (properNumberOfMaskBytes)
                {
                    case 0:
                    default:
                        throw new NotSupportedException(); // should not occur !
                    case 1:
                        this.instructionList.AddOp(OperatorName.Cntrmask1, reader.ReadByte() << 24);
                        break;
                    case 2:
                        this.instructionList.AddOp(
                            OperatorName.Cntrmask2,
                            (reader.ReadByte() << 24) | (reader.ReadByte() << 16));
                        break;
                    case 3:
                        this.instructionList.AddOp(
                            OperatorName.Cntrmask3,
                            (reader.ReadByte() << 24) | (reader.ReadByte() << 16) | (reader.ReadByte() << 8));
                        break;
                    case 4:
                        this.instructionList.AddOp(
                            OperatorName.Cntrmask4,
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

        private struct SimpleBinaryReader
        {
            private readonly byte[] buffer;

            public SimpleBinaryReader(byte[] buffer)
            {
                this.buffer = buffer;
                this.Position = 0;
            }

            public int BufferLength => this.buffer.Length;

            public int Position { get; private set; }

            public bool IsEnd() => this.Position >= this.buffer.Length;

            public byte ReadByte()

                // read current byte to stack and advance pos after read
                => this.buffer[this.Position++];

            public int ReadFloatFixed1616()
            {
                byte b0 = this.buffer[this.Position];
                byte b1 = this.buffer[this.Position + 1];
                byte b2 = this.buffer[this.Position + 2];
                byte b3 = this.buffer[this.Position + 3];

                this.Position += 4;
                return (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
            }
        }
    }
}
