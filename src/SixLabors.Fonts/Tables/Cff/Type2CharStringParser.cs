// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Diagnostics;

namespace SixLabors.Fonts.Tables.Cff
{
    internal class Type2CharStringParser
    {
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

        public Type2CharStringParser()
        {
        }

#if DEBUG
        int _dbugCount = 0;
        int _dbugInstructionListMark = 0;
#endif
        int _hintStemCount = 0;
        bool _foundSomeStem = false;
        bool _enterPathConstructionSeq = false;

        Type2GlyphInstructionList _insts;
        int _current_integer_count = 0;
        bool _doStemCount = true;
        Cff1Font _currentCff1Font;
        int _globalSubrBias;
        int _localSubrBias;

        public void SetCurrentCff1Font(Cff1Font currentCff1Font)
        {
            // this will provide subr buffer for callsubr callgsubr
            this._currentFontDict = null; // reset
            this._currentCff1Font = currentCff1Font;

            if (this._currentCff1Font._globalSubrRawBufferList != null)
            {
                this._globalSubrBias = CalculateBias(currentCff1Font._globalSubrRawBufferList.Count);
            }
            if (this._currentCff1Font._localSubrRawBufferList != null)
            {
                this._localSubrBias = CalculateBias(currentCff1Font._localSubrRawBufferList.Count);
            }
        }


        static int CalculateBias(int nsubr)
        {
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
            return (nsubr < 1240) ? 107 : (nsubr < 33900) ? 1131 : 32768;
        }

        struct SimpleBinaryReader
        {
            byte[] _buffer;
            int _pos;

            public SimpleBinaryReader(byte[] buffer)
            {
                this._buffer = buffer;
                this._pos = 0;
            }

            public bool IsEnd() => this._pos >= this._buffer.Length;

            public byte ReadByte()
            {
                // read current byte to stack and advance pos after read
                return this._buffer[this._pos++];
            }

            public int BufferLength => this._buffer.Length;

            public int Position => this._pos;

            public int ReadFloatFixed1616()
            {
                byte b0 = this._buffer[this._pos];
                byte b1 = this._buffer[this._pos + 1];
                byte b2 = this._buffer[this._pos + 2];
                byte b3 = this._buffer[this._pos + 3];

                this._pos += 4;
                return (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
            }
        }

        void ParseType2CharStringBuffer(byte[] buffer)
        {
            byte b0 = 0;

            bool cont = true;

            var reader = new SimpleBinaryReader(buffer);
            while (cont && !reader.IsEnd())
            {
                b0 = reader.ReadByte();
#if DEBUG
                // easy for debugging here
                this._dbugCount++;
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

                        //
                        this._insts.AddInt(ReadIntegerNumber(ref reader, b0));
                        if (this._doStemCount)
                        {
                            this._current_integer_count++;
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

                        this._insts.AddFloat(reader.ReadFloatFixed1616());

                        if (this._doStemCount)
                        {
                            this._current_integer_count++;
                        }

                        break;
                    case (byte)Type2Operator1.shortint: // 28

                        // shortint
                        // First byte of a 3-byte sequence specifying a number.
                        // a ShortInt value is specified by using the operator (28) followed by two bytes
                        // which represent numbers between –32768 and + 32767.The
                        // most significant byte follows the(28)
                        byte s_b0 = reader.ReadByte();
                        byte s_b1 = reader.ReadByte();
                        this._insts.AddInt((short)((s_b0 << 8) | (s_b1)));
                        //
                        if (this._doStemCount)
                        {
                            this._current_integer_count++;
                        }
                        break;
                    //---------------------------------------------------
                    case (byte)Type2Operator1._Reserved0_:// ???
                    case (byte)Type2Operator1._Reserved2_:// ???
                    case (byte)Type2Operator1._Reserved9_:// ???
                    case (byte)Type2Operator1._Reserved13_:// ???
                    case (byte)Type2Operator1._Reserved15_:// ???
                    case (byte)Type2Operator1._Reserved16_: // ???
                    case (byte)Type2Operator1._Reserved17_: // ???
                        // reserved, do nothing ?
                        break;

                    case (byte)Type2Operator1.escape: // 12


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
                            case Type2Operator2.flex:
                                this._insts.AddOp(OperatorName.flex);
                                break;
                            case Type2Operator2.hflex:
                                this._insts.AddOp(OperatorName.hflex);
                                break;
                            case Type2Operator2.hflex1:
                                this._insts.AddOp(OperatorName.hflex1);
                                break;
                            case Type2Operator2.flex1:
                                this._insts.AddOp(OperatorName.flex1);
                                ;
                                break;
                            //-------------------------
                            // 4.4: Arithmetic Operators
                            case Type2Operator2.abs:
                                this._insts.AddOp(OperatorName.abs);
                                break;
                            case Type2Operator2.add:
                                this._insts.AddOp(OperatorName.add);
                                break;
                            case Type2Operator2.sub:
                                this._insts.AddOp(OperatorName.sub);
                                break;
                            case Type2Operator2.div:
                                this._insts.AddOp(OperatorName.div);
                                break;
                            case Type2Operator2.neg:
                                this._insts.AddOp(OperatorName.neg);
                                break;
                            case Type2Operator2.random:
                                this._insts.AddOp(OperatorName.random);
                                break;
                            case Type2Operator2.mul:
                                this._insts.AddOp(OperatorName.mul);
                                break;
                            case Type2Operator2.sqrt:
                                this._insts.AddOp(OperatorName.sqrt);
                                break;
                            case Type2Operator2.drop:
                                this._insts.AddOp(OperatorName.drop);
                                break;
                            case Type2Operator2.exch:
                                this._insts.AddOp(OperatorName.exch);
                                break;
                            case Type2Operator2.index:
                                this._insts.AddOp(OperatorName.index);
                                break;
                            case Type2Operator2.roll:
                                this._insts.AddOp(OperatorName.roll);
                                break;
                            case Type2Operator2.dup:
                                this._insts.AddOp(OperatorName.dup);
                                break;

                            //-------------------------
                            // 4.5: Storage Operators
                            case Type2Operator2.put:
                                this._insts.AddOp(OperatorName.put);
                                break;
                            case Type2Operator2.get:
                                this._insts.AddOp(OperatorName.get);
                                break;
                            //-------------------------
                            // 4.6: Conditional
                            case Type2Operator2.and:
                                this._insts.AddOp(OperatorName.and);
                                break;
                            case Type2Operator2.or:
                                this._insts.AddOp(OperatorName.or);
                                break;
                            case Type2Operator2.not:
                                this._insts.AddOp(OperatorName.not);
                                break;
                            case Type2Operator2.eq:
                                this._insts.AddOp(OperatorName.eq);
                                break;
                            case Type2Operator2.ifelse:
                                this._insts.AddOp(OperatorName.ifelse);
                                break;
                        }

                        this.StopStemCount();

                        break;

                    //---------------------------------------------------------------------------
                    case (byte)Type2Operator1.endchar:
                        this.AddEndCharOp();
                        cont = false;
                        // when we found end char
                        // stop reading this...
                        break;
                    case (byte)Type2Operator1.rmoveto:
                        this.AddMoveToOp(OperatorName.rmoveto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.hmoveto:
                        this.AddMoveToOp(OperatorName.hmoveto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.vmoveto:
                        this.AddMoveToOp(OperatorName.vmoveto);
                        this.StopStemCount();
                        break;
                    //---------------------------------------------------------------------------
                    case (byte)Type2Operator1.rlineto:
                        this._insts.AddOp(OperatorName.rlineto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.hlineto:
                        this._insts.AddOp(OperatorName.hlineto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.vlineto:
                        this._insts.AddOp(OperatorName.vlineto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.rrcurveto:
                        this._insts.AddOp(OperatorName.rrcurveto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.hhcurveto:
                        this._insts.AddOp(OperatorName.hhcurveto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.hvcurveto:
                        this._insts.AddOp(OperatorName.hvcurveto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.rcurveline:
                        this._insts.AddOp(OperatorName.rcurveline);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.rlinecurve:
                        this._insts.AddOp(OperatorName.rlinecurve);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.vhcurveto:
                        this._insts.AddOp(OperatorName.vhcurveto);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.vvcurveto:
                        this._insts.AddOp(OperatorName.vvcurveto);
                        this.StopStemCount();
                        break;
                    //-------------------------------------------------------------------
                    // 4.3 Hint Operators
                    case (byte)Type2Operator1.hstem:
                        this.AddStemToList(OperatorName.hstem);
                        break;
                    case (byte)Type2Operator1.vstem:
                        this.AddStemToList(OperatorName.vstem);
                        break;
                    case (byte)Type2Operator1.vstemhm:
                        this.AddStemToList(OperatorName.vstemhm);
                        break;
                    case (byte)Type2Operator1.hstemhm:
                        this.AddStemToList(OperatorName.hstemhm);
                        break;
                    //-------------------------------------------------------------------
                    case (byte)Type2Operator1.hintmask:
                        this.AddHintMaskToList(ref reader);
                        this.StopStemCount();
                        break;
                    case (byte)Type2Operator1.cntrmask:
                        this.AddCounterMaskToList(ref reader);
                        this.StopStemCount();
                        break;
                    //-------------------------------------------------------------------
                    // 4.7: Subroutine Operators
                    case (byte)Type2Operator1._return:
#if DEBUG
                        if (!reader.IsEnd())
                        {
                            throw new NotSupportedException();
                        }

#endif
                        return;
                    //-------------------------------------------------------------------
                    case (byte)Type2Operator1.callsubr:

                        // get local subr proc
                        if (this._currentCff1Font != null)
                        {
                            Type2Instruction inst = this._insts.RemoveLast();
                            if (!inst.IsLoadInt)
                            {
                                throw new NotSupportedException();
                            }
                            if (this._doStemCount)
                            {
                                this._current_integer_count--;
                            }
                            // subr_no must be adjusted with proper bias value
                            if (this._currentCff1Font._localSubrRawBufferList != null)
                            {
                                this.ParseType2CharStringBuffer(this._currentCff1Font._localSubrRawBufferList[inst.Value + this._localSubrBias]);
                            }
                            else if (this._currentFontDict != null)
                            {
                                // use private dict
                                this.ParseType2CharStringBuffer(this._currentFontDict.LocalSubr[inst.Value + this._localSubrBias]);
                            }
                            else
                            {
                                throw new NotSupportedException();
                            }
                        }

                        break;
                    case (byte)Type2Operator1.callgsubr:

                        if (this._currentCff1Font != null)
                        {
                            Type2Instruction inst = this._insts.RemoveLast();
                            if (!inst.IsLoadInt)
                            {
                                throw new NotSupportedException();
                            }
                            if (this._doStemCount)
                            {
                                this._current_integer_count--;
                            }
                            // subr_no must be adjusted with proper bias value
                            // load global subr
                            this.ParseType2CharStringBuffer(this._currentCff1Font._globalSubrRawBufferList[inst.Value + this._globalSubrBias]);
                        }

                        break;
                }
            }
        }

#if DEBUG
        public ushort dbugCurrentGlyphIndex;
#endif
        FontDict _currentFontDict;

        public void SetCidFontDict(FontDict fontdic)
        {
#if DEBUG
            if (fontdic == null)
            {
                throw new NotSupportedException();
            }
#endif

            this._currentFontDict = fontdic;
            if (fontdic.LocalSubr != null)
            {
                this._localSubrBias = CalculateBias(this._currentFontDict.LocalSubr.Count);
            }
            else
            {
                this._localSubrBias = 0;
            }
        }

        public Type2GlyphInstructionList ParseType2CharString(byte[] buffer)
        {
            // reset
            this._hintStemCount = 0;
            this._current_integer_count = 0;
            this._foundSomeStem = false;
            this._enterPathConstructionSeq = false;
            this._doStemCount = true;

            this._insts = new Type2GlyphInstructionList();
            //--------------------
#if DEBUG
            this._dbugInstructionListMark++;
            if (this._currentCff1Font == null)
            {
                throw new NotSupportedException();
            }
            //
            this._insts.dbugGlyphIndex = this.dbugCurrentGlyphIndex;

            if (this.dbugCurrentGlyphIndex == 496)
            {

            }
#endif
            this.ParseType2CharStringBuffer(buffer);

#if DEBUG
            if (this.dbugCurrentGlyphIndex == 496)
            {
                // _insts.dbugDumpInstructionListToFile("glyph_496.txt");
            }
#endif
            return this._insts;
        }

        void StopStemCount()
        {
            this._current_integer_count = 0;
            this._doStemCount = false;
        }

        OperatorName _latestOpName = OperatorName.Unknown;

        void AddEndCharOp()
        {
            // from https://www.adobe.com/content/dam/acom/en/devnet/font/pdfs/5177.Type2.pdf
            // Note 4 The first stack - clearing operator, which must be one of
            // hstem, hstemhm, vstem, vstemhm,
            // cntrmask, hintmask,
            // hmoveto, vmoveto, rmoveto,
            // or endchar,
            // takes an additional argument — the width(as described earlier), which may be expressed as zero or one numeric argument

            if (!this._foundSomeStem && !this._enterPathConstructionSeq)
            {
                if (this._insts.Count > 0)
                {
                    this._insts.ChangeFirstInstToGlyphWidthValue();
                }
            }
            // takes an additional argument — the width(as described earlier), which may be expressed as zero or one numeric argument
            this._insts.AddOp(OperatorName.endchar);
        }



        /// <summary>
        /// for hmoveto, vmoveto, rmoveto
        /// </summary>
        /// <param name="op"></param>
        void AddMoveToOp(OperatorName op)
        {
            // from https://www.adobe.com/content/dam/acom/en/devnet/font/pdfs/5177.Type2.pdf
            // Note 4 The first stack - clearing operator, which must be one of
            // hstem, hstemhm, vstem, vstemhm,
            // cntrmask, hintmask,
            // hmoveto, vmoveto, rmoveto,
            // or endchar,
            // takes an additional argument — the width(as described earlier), which may be expressed as zero or one numeric argument
            // just add

            if (!this._foundSomeStem && !this._enterPathConstructionSeq)
            {
                if (op == OperatorName.rmoveto)
                {
                    if ((this._insts.Count % 2) != 0)
                    {
                        this._insts.ChangeFirstInstToGlyphWidthValue();
                    }
                }
                else
                {
                    // vmoveto, hmoveto
                    if (this._insts.Count > 1)
                    {
                        // ...
                        this._insts.ChangeFirstInstToGlyphWidthValue();
                    }
                }
            }
            this._enterPathConstructionSeq = true;
            this._insts.AddOp(op);
        }

        /// <summary>
        /// for hstem, hstemhm, vstem, vstemhm
        /// </summary>
        /// <param name="stemName"></param>
        void AddStemToList(OperatorName stemName)
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

            if ((this._current_integer_count % 2) != 0)
            {
                // all kind has even number of stem
                if (this._foundSomeStem)
                {
#if DEBUG
                    this._insts.dbugDumpInstructionListToFile("test_type2_" + (this._dbugInstructionListMark - 1) + ".txt");
#endif
                    throw new NotSupportedException();
                }
                else
                {
                    // the first one is 'width'
                    this._insts.ChangeFirstInstToGlyphWidthValue();
                    this._current_integer_count--;
                }
            }
            this._hintStemCount += (this._current_integer_count / 2); // save a snapshot of stem count
            this._insts.AddOp(stemName);
            this._current_integer_count = 0;// clear
            this._foundSomeStem = true;
            this._latestOpName = stemName;
        }
        /// <summary>
        /// add hintmask
        /// </summary>
        /// <param name="reader"></param>
        void AddHintMaskToList(ref SimpleBinaryReader reader)
        {
            if (this._foundSomeStem && this._current_integer_count > 0)
            {

                // type2 5177.pdf
                // ...
                // If hstem and vstem hints are both declared at the beginning of
                // a charstring, and this sequence is followed directly by the
                // hintmask or cntrmask operators, ...
                // the vstem hint operator need not be included ***

#if DEBUG
                if ((this._current_integer_count % 2) != 0)
                {
                    throw new NotSupportedException();
                }
                else
                {

                }
#endif
                if (this._doStemCount)
                {
                    switch (this._latestOpName)
                    {
                        case OperatorName.hstem:
                            // add vstem  ***( from reason above)

                            this._hintStemCount += (this._current_integer_count / 2); // save a snapshot of stem count
                            this._insts.AddOp(OperatorName.vstem);
                            this._latestOpName = OperatorName.vstem;
                            this._current_integer_count = 0; // clear
                            break;
                        case OperatorName.hstemhm:
                            // add vstem  ***( from reason above) ??
                            this._hintStemCount += (this._current_integer_count / 2); // save a snapshot of stem count
                            this._insts.AddOp(OperatorName.vstem);
                            this._latestOpName = OperatorName.vstem;
                            this._current_integer_count = 0;// clear
                            break;
                        case OperatorName.vstemhm:
                            //-------
                            // TODO: review here?
                            // found this in xits.otf
                            this._hintStemCount += (this._current_integer_count / 2); // save a snapshot of stem count
                            this._insts.AddOp(OperatorName.vstem);
                            this._latestOpName = OperatorName.vstem;
                            this._current_integer_count = 0;// clear
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }
                else
                {

                }
            }

            if (this._hintStemCount == 0)
            {
                if (!this._foundSomeStem)
                {
                    this._hintStemCount = (this._current_integer_count / 2);
                    if (this._hintStemCount == 0)
                    {
                        return;
                    }
                    this._foundSomeStem = true;// ?
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            //----------------------
            // this is my hintmask extension, => to fit with our Evaluation stack
            int properNumberOfMaskBytes = (this._hintStemCount + 7) / 8;

            if (reader.Position + properNumberOfMaskBytes >= reader.BufferLength)
            {
                throw new NotSupportedException();
            }
            if (properNumberOfMaskBytes > 4)
            {
                int remaining = properNumberOfMaskBytes;

                for (; remaining > 3;)
                {
                    this._insts.AddInt((
                       (reader.ReadByte() << 24) |
                       (reader.ReadByte() << 16) |
                       (reader.ReadByte() << 8) |
                       (reader.ReadByte())
                       ));
                    remaining -= 4; // ***
                }
                switch (remaining)
                {
                    case 0:
                        // do nothing
                        break;
                    case 1:
                        this._insts.AddInt(reader.ReadByte() << 24);
                        break;
                    case 2:
                        this._insts.AddInt(
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16));

                        break;
                    case 3:
                        this._insts.AddInt(
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16) |
                            (reader.ReadByte() << 8));
                        break;
                    default:
                        throw new NotSupportedException();// should not occur !
                }

                this._insts.AddOp(OperatorName.hintmask_bits, properNumberOfMaskBytes);
            }
            else
            {
                // last remaining <4 bytes
                switch (properNumberOfMaskBytes)
                {
                    case 0:
                    default:
                        throw new NotSupportedException();// should not occur !
                    case 1:
                        this._insts.AddOp(OperatorName.hintmask1, (reader.ReadByte() << 24));
                        break;
                    case 2:
                        this._insts.AddOp(OperatorName.hintmask2,
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16)
                            );
                        break;
                    case 3:
                        this._insts.AddOp(OperatorName.hintmask3,
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16) |
                            (reader.ReadByte() << 8)
                            );
                        break;
                    case 4:
                        this._insts.AddOp(OperatorName.hintmask4,
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16) |
                            (reader.ReadByte() << 8) |
                            (reader.ReadByte())
                            );
                        break;
                }
            }
        }
        /// <summary>
        /// cntrmask
        /// </summary>
        /// <param name="reader"></param>
        void AddCounterMaskToList(ref SimpleBinaryReader reader)
        {
            if (this._hintStemCount == 0)
            {
                if (!this._foundSomeStem)
                {
                    // ????
                    this._hintStemCount = (this._current_integer_count / 2);
                    this._foundSomeStem = true;// ?
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else
            {
                this._hintStemCount += (this._current_integer_count / 2);
            }
            //----------------------
            // this is my hintmask extension, => to fit with our Evaluation stack
            int properNumberOfMaskBytes = (this._hintStemCount + 7) / 8;
            if (reader.Position + properNumberOfMaskBytes >= reader.BufferLength)
            {
                throw new NotSupportedException();
            }

            if (properNumberOfMaskBytes > 4)
            {
                int remaining = properNumberOfMaskBytes;

                for (; remaining > 3;)
                {
                    this._insts.AddInt((
                       (reader.ReadByte() << 24) |
                       (reader.ReadByte() << 16) |
                       (reader.ReadByte() << 8) |
                       (reader.ReadByte())
                       ));
                    remaining -= 4; // ***
                }
                switch (remaining)
                {
                    case 0:
                        // do nothing
                        break;
                    case 1:
                        this._insts.AddInt(reader.ReadByte() << 24);
                        break;
                    case 2:
                        this._insts.AddInt(
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16));

                        break;
                    case 3:
                        this._insts.AddInt(
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16) |
                            (reader.ReadByte() << 8));
                        break;
                    default:
                        throw new NotSupportedException();// should not occur !
                }

                this._insts.AddOp(OperatorName.cntrmask_bits, properNumberOfMaskBytes);
            }
            else
            {
                // last remaining <4 bytes
                switch (properNumberOfMaskBytes)
                {
                    case 0:
                    default:
                        throw new NotSupportedException();// should not occur !
                    case 1:
                        this._insts.AddOp(OperatorName.cntrmask1, (reader.ReadByte() << 24));
                        break;
                    case 2:
                        this._insts.AddOp(OperatorName.cntrmask2,
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16)
                            );
                        break;
                    case 3:
                        this._insts.AddOp(OperatorName.cntrmask3,
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16) |
                            (reader.ReadByte() << 8)
                            );
                        break;
                    case 4:
                        this._insts.AddOp(OperatorName.cntrmask4,
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16) |
                            (reader.ReadByte() << 8) |
                            (reader.ReadByte())
                            );
                        break;
                }
            }
        }

        static int ReadIntegerNumber(ref SimpleBinaryReader _reader, byte b0)
        {
            if (b0 >= 32 && b0 <= 246)
            {
                return b0 - 139;
            }
            else if (b0 <= 250)  // && b0 >= 247 , *** if-else sequence is important! ***
            {
                byte b1 = _reader.ReadByte();
                return (b0 - 247) * 256 + b1 + 108;
            }
            else if (b0 <= 254)  // &&  b0 >= 251 ,*** if-else sequence is important! ***
            {
                byte b1 = _reader.ReadByte();
                return -(b0 - 251) * 256 - b1 - 108;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
