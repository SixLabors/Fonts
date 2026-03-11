// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Two-byte Type 2 charstring operators (preceded by the escape byte 12).
/// <see href="https://adobe-type-tools.github.io/font-tech-notes/pdfs/5177.Type2.pdf"/>
/// </summary>
internal enum Type2Operator2 : byte
{
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
