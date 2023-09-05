// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff
{
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
        VsIndex,
        Blend,
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
}