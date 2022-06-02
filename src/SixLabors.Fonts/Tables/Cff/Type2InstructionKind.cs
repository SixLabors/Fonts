// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.Cff
{
    /// <summary>
    /// Contains the combined collection of Type2 Charstring Operators plus custom instructions.
    /// <see href="https://adobe-type-tools.github.io/font-tech-notes/pdfs/5177.Type2.pdf"/>
    /// </summary>
    internal enum Type2InstructionKind : byte
    {
        Unknown,
        LoadInt,
        LoadFloat,
        GlyphWidth,

        // Custom Instructions
        // TODO: What are they used for?

        /// <summary>
        /// Load 4 consecutive signed bytes.
        /// </summary>
        LoadSbyte4, // my extension, 4 sbyte in an int32

        /// <summary>
        /// Load 3 consecutive signed bytes.
        /// </summary>
        LoadSbyte3, // my extension, 3 sbytes in an int32

        /// <summary>
        /// Load 2 consecutive signed short.
        /// </summary>
        LoadShort2, // my extension, 2 short in an int32

        //---------------------
        // type2Operator1
        //---------------------
        [OriginalType2Operator(Type2Operator1.Hstem)]
        Hstem,
        [OriginalType2Operator(Type2Operator1.Vstem)]
        Vstem,
        [OriginalType2Operator(Type2Operator1.Vmoveto)]
        Vmoveto,
        [OriginalType2Operator(Type2Operator1.Rlineto)]
        Rlineto,
        [OriginalType2Operator(Type2Operator1.Hlineto)]
        Hlineto,
        [OriginalType2Operator(Type2Operator1.Vlineto)]
        Vlineto,
        [OriginalType2Operator(Type2Operator1.Rrcurveto)]
        Rrcurveto,
        [OriginalType2Operator(Type2Operator1.Callsubr)]
        Callsubr,
        [OriginalType2Operator(Type2Operator1.Return)]
        Return,

        // [OriginalType2Operator(Type2Operator1.escape)] escape, //not used!
        [OriginalType2Operator(Type2Operator1.Endchar)]
        Endchar,
        [OriginalType2Operator(Type2Operator1.Hstemhm)]
        Hstemhm,
        [OriginalType2Operator(Type2Operator1.Hintmask)]
        Hintmask1, // my hint-mask extension, contains 1 byte hint
        [OriginalType2Operator(Type2Operator1.Hintmask)]
        Hintmask2, // my hint-mask extension, contains 2 bytes hint
        [OriginalType2Operator(Type2Operator1.Hintmask)]
        Hintmask3, // my hint-mask extension, contains 3 bytes hint
        [OriginalType2Operator(Type2Operator1.Hintmask)]
        Hintmask4, // my hint-mask extension, contains 4 bytes hint
        [OriginalType2Operator(Type2Operator1.Hintmask)]
        Hintmask_bits, // my hint-mask extension, contains n bits of hint

        [OriginalType2Operator(Type2Operator1.Cntrmask)]
        Cntrmask1, // my counter-mask extension, contains 1 byte hint
        [OriginalType2Operator(Type2Operator1.Cntrmask)]
        Cntrmask2, // my counter-mask extension, contains 2 bytes hint
        [OriginalType2Operator(Type2Operator1.Cntrmask)]
        Cntrmask3, // my counter-mask extension, contains 3 bytes hint
        [OriginalType2Operator(Type2Operator1.Cntrmask)]
        Cntrmask4, // my counter-mask extension, contains 4 bytes hint
        [OriginalType2Operator(Type2Operator1.Cntrmask)]
        Cntrmask_bits, // my counter-mask extension, contains n bits of hint

        [OriginalType2Operator(Type2Operator1.Rmoveto)]
        Rmoveto,
        [OriginalType2Operator(Type2Operator1.Hmoveto)]
        Hmoveto,
        [OriginalType2Operator(Type2Operator1.Vstemhm)]
        Vstemhm,
        [OriginalType2Operator(Type2Operator1.Rcurveline)]
        Rcurveline,
        [OriginalType2Operator(Type2Operator1.Rlinecurve)]
        Rlinecurve,
        [OriginalType2Operator(Type2Operator1.Vvcurveto)]
        Vvcurveto,
        [OriginalType2Operator(Type2Operator1.Hhcurveto)]
        Hhcurveto,
        [OriginalType2Operator(Type2Operator1.Shortint)]
        Shortint,
        [OriginalType2Operator(Type2Operator1.Callgsubr)]
        Callgsubr,
        [OriginalType2Operator(Type2Operator1.Vhcurveto)]
        Vhcurveto,
        [OriginalType2Operator(Type2Operator1.Hvcurveto)]
        Hvcurveto,

        // Two-byte Type 2 Operators
        [OriginalType2Operator(Type2Operator2.And)]
        And,
        [OriginalType2Operator(Type2Operator2.Or)]
        Or,
        [OriginalType2Operator(Type2Operator2.Not)]
        Not,
        [OriginalType2Operator(Type2Operator2.Abs)]
        Abs,
        [OriginalType2Operator(Type2Operator2.Add)]
        Add,
        [OriginalType2Operator(Type2Operator2.Sub)]
        Sub,
        [OriginalType2Operator(Type2Operator2.Div)]
        Div,
        [OriginalType2Operator(Type2Operator2.Neg)]
        Neg,
        [OriginalType2Operator(Type2Operator2.Eq)]
        Eq,
        [OriginalType2Operator(Type2Operator2.Drop)]
        Drop,
        [OriginalType2Operator(Type2Operator2.Put)]
        Put,
        [OriginalType2Operator(Type2Operator2.Get)]
        Get,
        [OriginalType2Operator(Type2Operator2.Ifelse)]
        Ifelse,
        [OriginalType2Operator(Type2Operator2.Random)]
        Random,
        [OriginalType2Operator(Type2Operator2.Mul)]
        Mul,
        [OriginalType2Operator(Type2Operator2.Sqrt)]
        Sqrt,
        [OriginalType2Operator(Type2Operator2.Dup)]
        Dup,
        [OriginalType2Operator(Type2Operator2.Exch)]
        Exch,
        [OriginalType2Operator(Type2Operator2.Index)]
        Index,
        [OriginalType2Operator(Type2Operator2.Roll)]
        Roll,
        [OriginalType2Operator(Type2Operator2.Hflex)]
        Hflex,
        [OriginalType2Operator(Type2Operator2.Flex)]
        Flex,
        [OriginalType2Operator(Type2Operator2.Hflex1)]
        Hflex1,
        [OriginalType2Operator(Type2Operator2.Flex1)]
        Flex1
    }

    internal enum Type2Operator1 : byte
    {
        // Appendix A Type 2 Charstring Command Codes
        Reserved0_ = 0,
        Hstem, // 1
        Reserved2_, // 2
        Vstem, // 3
        Vmoveto, // 4
        Rlineto, // 5
        Hlineto, // 6
        Vlineto, // 7,
        Rrcurveto, // 8
        Reserved9_, // 9
        Callsubr, // 10
        Return, // 11
        Escape, // 12
        Reserved13_,
        Endchar, // 14
        Reserved15_,
        Reserved16_,
        Reserved17_,
        Hstemhm, // 18
        Hintmask, // 19
        Cntrmask, // 20
        Rmoveto, // 21
        Hmoveto, // 22
        Vstemhm, // 23
        Rcurveline, // 24
        Rlinecurve, // 25
        Vvcurveto, // 26
        Hhcurveto, // 27
        Shortint, // 28
        Callgsubr, // 29
        Vhcurveto, // 30
        Hvcurveto, // 31
    }

    internal enum Type2Operator2 : byte
    {
        // Two-byte Type 2 Operators
        Reserved0_ = 0,
        Reserved1_,
        Reserved2_,
        And, // 3
        Or, // 4
        Not, // 5
        Reserved6_,
        Reserved7_,
        Reserved8_,
        Abs, // 9
        Add, // 10
        Sub, // 11
        Div, // 12
        Reserved13_,
        Neg, // 14
        Eq, // 15
        Reserved16_,
        Reserved17_,
        Drop, // 18
        Reserved19_,
        Put, // 20
        Get, // 21
        Ifelse, // 22
        Random, // 23
        Mul, // 24,
        Reserved25_,
        Sqrt, // 26
        Dup, // 27
        Exch, // 28 , exchanges the top two elements on the argument stack
        Index, // 29
        Roll, // 30
        Reserved31_,
        Reserved32_,
        Reserved33_,
        Hflex, // 34
        Flex, // 35
        Hflex1, // 36
        Flex1// 37
    }
}
