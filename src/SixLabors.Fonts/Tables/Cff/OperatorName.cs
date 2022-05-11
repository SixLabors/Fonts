// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.Cff
{
    /// <summary>
    /// Merged ccf operators,(op1 and op2, note on attribute of each field)
    /// </summary>
    internal enum OperatorName : byte
    {
        Unknown,
        LoadInt,
        LoadFloat,
        GlyphWidth,

        LoadSbyte4, // my extension, 4 sbyte in an int32
        LoadSbyte3, // my extension, 3 sbytes in an int32
        LoadShort2, // my extension, 2 short in an int32

        //---------------------
        // type2Operator1
        //---------------------
        [OriginalType2Operator(Type2Operator1.hstem)]
        hstem,
        [OriginalType2Operator(Type2Operator1.vstem)]
        vstem,
        [OriginalType2Operator(Type2Operator1.vmoveto)]
        vmoveto,
        [OriginalType2Operator(Type2Operator1.rlineto)]
        rlineto,
        [OriginalType2Operator(Type2Operator1.hlineto)]
        hlineto,
        [OriginalType2Operator(Type2Operator1.vlineto)]
        vlineto,
        [OriginalType2Operator(Type2Operator1.rrcurveto)]
        rrcurveto,
        [OriginalType2Operator(Type2Operator1.callsubr)]
        callsubr,
        [OriginalType2Operator(Type2Operator1._return)]
        _return,

        // [OriginalType2Operator(Type2Operator1.escape)] escape, //not used!
        [OriginalType2Operator(Type2Operator1.endchar)]
        endchar,
        [OriginalType2Operator(Type2Operator1.hstemhm)]
        hstemhm,
        [OriginalType2Operator(Type2Operator1.hintmask)]
        hintmask1, // my hint-mask extension, contains 1 byte hint
        [OriginalType2Operator(Type2Operator1.hintmask)]
        hintmask2, // my hint-mask extension, contains 2 bytes hint
        [OriginalType2Operator(Type2Operator1.hintmask)]
        hintmask3, // my hint-mask extension, contains 3 bytes hint
        [OriginalType2Operator(Type2Operator1.hintmask)]
        hintmask4, // my hint-mask extension, contains 4 bytes hint
        [OriginalType2Operator(Type2Operator1.hintmask)]
        hintmask_bits,// my hint-mask extension, contains n bits of hint

        [OriginalType2Operator(Type2Operator1.cntrmask)]
        cntrmask1, // my counter-mask extension, contains 1 byte hint
        [OriginalType2Operator(Type2Operator1.cntrmask)]
        cntrmask2, // my counter-mask extension, contains 2 bytes hint
        [OriginalType2Operator(Type2Operator1.cntrmask)]
        cntrmask3, // my counter-mask extension, contains 3 bytes hint
        [OriginalType2Operator(Type2Operator1.cntrmask)]
        cntrmask4, // my counter-mask extension, contains 4 bytes hint
        [OriginalType2Operator(Type2Operator1.cntrmask)]
        cntrmask_bits, // my counter-mask extension, contains n bits of hint

        [OriginalType2Operator(Type2Operator1.rmoveto)]
        rmoveto,
        [OriginalType2Operator(Type2Operator1.hmoveto)]
        hmoveto,
        [OriginalType2Operator(Type2Operator1.vstemhm)]
        vstemhm,
        [OriginalType2Operator(Type2Operator1.rcurveline)]
        rcurveline,
        [OriginalType2Operator(Type2Operator1.rlinecurve)]
        rlinecurve,
        [OriginalType2Operator(Type2Operator1.vvcurveto)]
        vvcurveto,
        [OriginalType2Operator(Type2Operator1.hhcurveto)]
        hhcurveto,
        [OriginalType2Operator(Type2Operator1.shortint)]
        shortint,
        [OriginalType2Operator(Type2Operator1.callgsubr)]
        callgsubr,
        [OriginalType2Operator(Type2Operator1.vhcurveto)]
        vhcurveto,
        [OriginalType2Operator(Type2Operator1.hvcurveto)]
        hvcurveto,

        // Two-byte Type 2 Operators
        [OriginalType2Operator(Type2Operator2.and)]
        and,
        [OriginalType2Operator(Type2Operator2.or)]
        or,
        [OriginalType2Operator(Type2Operator2.not)]
        not,
        [OriginalType2Operator(Type2Operator2.abs)]
        abs,
        [OriginalType2Operator(Type2Operator2.add)]
        add,
        [OriginalType2Operator(Type2Operator2.sub)]
        sub,
        [OriginalType2Operator(Type2Operator2.div)]
        div,
        [OriginalType2Operator(Type2Operator2.neg)]
        neg,
        [OriginalType2Operator(Type2Operator2.eq)]
        eq,
        [OriginalType2Operator(Type2Operator2.drop)]
        drop,
        [OriginalType2Operator(Type2Operator2.put)]
        put,
        [OriginalType2Operator(Type2Operator2.get)]
        get,
        [OriginalType2Operator(Type2Operator2.ifelse)]
        ifelse,
        [OriginalType2Operator(Type2Operator2.random)]
        random,
        [OriginalType2Operator(Type2Operator2.mul)]
        mul,
        [OriginalType2Operator(Type2Operator2.sqrt)]
        sqrt,
        [OriginalType2Operator(Type2Operator2.dup)]
        dup,
        [OriginalType2Operator(Type2Operator2.exch)]
        exch,
        [OriginalType2Operator(Type2Operator2.index)]
        index,
        [OriginalType2Operator(Type2Operator2.roll)]
        roll,
        [OriginalType2Operator(Type2Operator2.hflex)]
        hflex,
        [OriginalType2Operator(Type2Operator2.flex)]
        flex,
        [OriginalType2Operator(Type2Operator2.hflex1)]
        hflex1,
        [OriginalType2Operator(Type2Operator2.flex1)]
        flex1
    }

    internal enum Type2Operator1 : byte
    {
        // Appendix A Type 2 Charstring Command Codes
        _Reserved0_ = 0,
        hstem, // 1
        _Reserved2_,// 2
        vstem, // 3
        vmoveto,// 4
        rlineto, // 5
        hlineto, // 6
        vlineto,// 7,
        rrcurveto,// 8
        _Reserved9_, // 9
        callsubr, // 10
        _return, // 11
        escape,// 12
        _Reserved13_,
        endchar,// 14
        _Reserved15_,
        _Reserved16_,
        _Reserved17_,
        hstemhm,// 18
        hintmask,// 19
        cntrmask,// 20
        rmoveto,// 21
        hmoveto,// 22
        vstemhm,// 23
        rcurveline, // 24
        rlinecurve,// 25
        vvcurveto,// 26
        hhcurveto, // 27
        shortint, // 28
        callgsubr, // 29
        vhcurveto, // 30
        hvcurveto, // 31
    }

    internal enum Type2Operator2 : byte
    {
        // Two-byte Type 2 Operators
        _Reserved0_ = 0,
        _Reserved1_,
        _Reserved2_,
        and, // 3
        or, // 4
        not, // 5
        _Reserved6_,
        _Reserved7_,
        _Reserved8_,
        abs,// 9
        add,// 10
        sub,// 11
        div,// 12
        _Reserved13_,
        neg,// 14
        eq, // 15
        _Reserved16_,
        _Reserved17_,
        drop,// 18
        _Reserved19_,
        put,// 20
        get, // 21
        ifelse,// 22
        random,// 23
        mul, // 24,
        _Reserved25_,
        sqrt,// 26
        dup,// 27
        exch,// 28 , exchanges the top two elements on the argument stack
        index,// 29
        roll,// 30
        _Reserved31_,
        _Reserved32_,
        _Reserved33_,
        hflex,// 34
        flex, // 35
        hflex1,// 36
        flex1// 37
    }
}
