// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Supports a simple iteration over a linebreak collection.
/// Implementation of the Unicode Line Break Algorithm. UAX:14
/// <see href="https://www.unicode.org/reports/tr14/tr14-37.html"/>
/// Methods are pattern-matched by compiler to allow using foreach pattern.
/// </summary>
internal ref struct LineBreakEnumerator
{
    private readonly ReadOnlySpan<char> source;
    private int charPosition;
    private readonly int pointsLength;
    private int position;
    private int lastPosition;
    private LineBreakClass currentClass;
    private LineBreakClass nextClass;
    private bool first;
    private int alphaNumericCount;
    private bool lb8a;
    private bool lb21a;
    private bool lb22ex;
    private bool lb24ex;
    private bool lb25ex;
    private bool lb30;
    private int lb30a;
    private bool lb31;

    public LineBreakEnumerator(ReadOnlySpan<char> source)
        : this()
    {
        this.source = source;
        this.pointsLength = CodePoint.GetCodePointCount(source);
        this.charPosition = 0;
        this.position = 0;
        this.lastPosition = 0;
        this.currentClass = LineBreakClass.XX;
        this.nextClass = LineBreakClass.XX;
        this.first = true;
        this.lb8a = false;
        this.lb21a = false;
        this.lb22ex = false;
        this.lb24ex = false;
        this.lb25ex = false;
        this.alphaNumericCount = 0;
        this.lb31 = false;
        this.lb30 = false;
        this.lb30a = 0;
    }

    public LineBreak Current { get; private set; }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that iterates through the collection.</returns>
    public readonly LineBreakEnumerator GetEnumerator() => this;

    /// <summary>
    /// Advances the enumerator to the next element of the collection.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the enumerator was successfully advanced to the next element;
    /// <see langword="false"/> if the enumerator has passed the end of the collection.
    /// </returns>
    public bool MoveNext()
    {
        // Get the first char if we're at the beginning of the string.
        if (this.first)
        {
            LineBreakClass firstClass = this.NextCharClass();
            this.first = false;
            this.currentClass = MapFirst(firstClass);
            this.nextClass = firstClass;
            this.lb8a = firstClass == LineBreakClass.ZWJ;
            this.lb30a = 0;
        }

        while (this.position < this.pointsLength)
        {
            this.lastPosition = this.position;
            LineBreakClass lastClass = this.nextClass;
            this.nextClass = this.NextCharClass();

            // Explicit newline
            switch (this.currentClass)
            {
                case LineBreakClass.BK:
                case LineBreakClass.CR when this.nextClass != LineBreakClass.LF:
                    this.currentClass = MapFirst(this.nextClass);
                    this.Current = new LineBreak(this.FindPriorNonWhitespace(this.lastPosition), this.lastPosition, true);
                    return true;
            }

            bool? shouldBreak = this.GetSimpleBreak() ?? (bool?)this.GetPairTableBreak(lastClass);

            // Rule LB8a
            this.lb8a = this.nextClass == LineBreakClass.ZWJ;

            if (shouldBreak.Value)
            {
                this.Current = new LineBreak(this.FindPriorNonWhitespace(this.lastPosition), this.lastPosition, false);
                return true;
            }
        }

        if (this.position >= this.pointsLength && this.lastPosition < this.pointsLength)
        {
            this.lastPosition = this.pointsLength;
            bool required = false;
            switch (this.currentClass)
            {
                case LineBreakClass.BK:
                case LineBreakClass.CR when this.nextClass != LineBreakClass.LF:
                    required = true;
                    break;
            }

            this.Current = new LineBreak(this.FindPriorNonWhitespace(this.pointsLength), this.lastPosition, required);
            return true;
        }

        this.Current = default;
        return false;
    }

    private static LineBreakClass MapClass(CodePoint cp, LineBreakClass c)
    {
        // LB 1
        // ==========================================
        // Resolved Original    General_Category
        // ==========================================
        // AL       AI, SG, XX  Any
        // CM       SA          Only Mn or Mc
        // AL       SA          Any except Mn and Mc
        // NS       CJ          Any
        switch (c)
        {
            case LineBreakClass.AI:
            case LineBreakClass.SG:
            case LineBreakClass.XX:
                return LineBreakClass.AL;

            case LineBreakClass.SA:
                UnicodeCategory category = CodePoint.GetGeneralCategory(cp);
                return (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark)
                    ? LineBreakClass.CM
                    : LineBreakClass.AL;

            case LineBreakClass.CJ:
                return LineBreakClass.NS;

            default:
                return c;
        }
    }

    private static LineBreakClass MapFirst(LineBreakClass c)
        => c switch
        {
            LineBreakClass.LF or LineBreakClass.NL => LineBreakClass.BK,
            LineBreakClass.SP => LineBreakClass.WJ,
            _ => c,
        };

    private static bool IsAlphaNumeric(LineBreakClass cls)
        => cls is LineBreakClass.AL
        or LineBreakClass.HL
        or LineBreakClass.NU;

    private readonly LineBreakClass PeekNextCharClass()
    {
        CodePoint cp = CodePoint.DecodeFromUtf16At(this.source, this.charPosition);
        return MapClass(cp, CodePoint.GetLineBreakClass(cp));
    }

    // Get the next character class
    private LineBreakClass NextCharClass()
    {
        CodePoint cp = CodePoint.DecodeFromUtf16At(this.source, this.charPosition, out int count);
        LineBreakClass cls = MapClass(cp, CodePoint.GetLineBreakClass(cp));
        this.charPosition += count;
        this.position++;

        // Keep track of alphanumeric + any combining marks.
        // This is used for LB22 and LB30.
        if (IsAlphaNumeric(this.currentClass) || (this.alphaNumericCount > 0 && cls == LineBreakClass.CM))
        {
            this.alphaNumericCount++;
        }

        // Track combining mark exceptions. LB22
        if (cls == LineBreakClass.CM)
        {
            switch (this.currentClass)
            {
                case LineBreakClass.BK:
                case LineBreakClass.CB:
                case LineBreakClass.EX:
                case LineBreakClass.LF:
                case LineBreakClass.NL:
                case LineBreakClass.SP:
                case LineBreakClass.ZW:
                case LineBreakClass.CR:
                    this.lb22ex = true;
                    break;
            }
        }

        // Track combining mark exceptions. LB31
        if (this.first && cls == LineBreakClass.CM)
        {
            this.lb31 = true;
        }

        if (cls == LineBreakClass.CM)
        {
            switch (this.currentClass)
            {
                case LineBreakClass.BK:
                case LineBreakClass.CB:
                case LineBreakClass.EX:
                case LineBreakClass.LF:
                case LineBreakClass.NL:
                case LineBreakClass.SP:
                case LineBreakClass.ZW:
                case LineBreakClass.CR:
                case LineBreakClass.ZWJ:
                    this.lb31 = true;
                    break;
            }
        }

        if (this.first
            && (cls == LineBreakClass.PO || cls == LineBreakClass.PR || cls == LineBreakClass.SP))
        {
            this.lb31 = true;
        }

        if (this.currentClass == LineBreakClass.AL
            && (cls == LineBreakClass.PO || cls == LineBreakClass.PR || cls == LineBreakClass.SP))
        {
            this.lb31 = true;
        }

        // Reset LB31 if next is U+0028 (Left Opening Parenthesis)
        if (this.lb31
            && this.currentClass != LineBreakClass.PO
            && this.currentClass != LineBreakClass.PR
            && cls == LineBreakClass.OP && cp.Value == 0x0028)
        {
            this.lb31 = false;
        }

        // Rule LB24
        if (this.first && (cls == LineBreakClass.CL || cls == LineBreakClass.CP))
        {
            this.lb24ex = true;
        }

        // Rule LB25
        if (this.first
            && (cls == LineBreakClass.CL || cls == LineBreakClass.IS || cls == LineBreakClass.SY))
        {
            this.lb25ex = true;
        }

        if (cls is LineBreakClass.SP or LineBreakClass.WJ or LineBreakClass.AL)
        {
            LineBreakClass next = this.PeekNextCharClass();
            if (next is LineBreakClass.CL or LineBreakClass.IS or LineBreakClass.SY)
            {
                this.lb25ex = true;
            }
        }

        // AlphaNumeric + and combining marks can break for OP except.
        // - U+0028 (Left Opening Parenthesis)
        // - U+005B (Opening Square Bracket)
        // - U+007B (Left Curly Bracket)
        // See custom columns|rules in the text pair table.
        // https://www.unicode.org/Public/13.0.0/ucd/auxiliary/LineBreakTest.html
        this.lb30 = this.alphaNumericCount > 0
            && cls == LineBreakClass.OP
            && cp.Value != 0x0028
            && cp.Value != 0x005B
            && cp.Value != 0x007B;

        return cls;
    }

    private bool? GetSimpleBreak()
    {
        // handle classes not handled by the pair table
        switch (this.nextClass)
        {
            case LineBreakClass.SP:
                return false;

            case LineBreakClass.BK:
            case LineBreakClass.LF:
            case LineBreakClass.NL:
                this.currentClass = LineBreakClass.BK;
                return false;

            case LineBreakClass.CR:
                this.currentClass = LineBreakClass.CR;
                return false;
        }

        return null;
    }

    private bool GetPairTableBreak(LineBreakClass lastClass)
    {
        // If not handled already, use the pair table
        bool shouldBreak = false;
        switch (LineBreakPairTable.Table[(int)this.currentClass][(int)this.nextClass])
        {
            case LineBreakPairTable.DIBRK: // Direct break
                shouldBreak = true;
                break;

            // TODO: Rewrite this so that it defaults to true and rules are set as exceptions.
            case LineBreakPairTable.INBRK: // Possible indirect break

                // LB31
                if (this.lb31 && this.nextClass == LineBreakClass.OP)
                {
                    shouldBreak = true;
                    this.lb31 = false;
                    break;
                }

                // LB30
                if (this.lb30)
                {
                    shouldBreak = true;
                    this.lb30 = false;
                    this.alphaNumericCount = 0;
                    break;
                }

                // LB25
                if (this.lb25ex)
                {
                    switch (lastClass)
                    {
                        case LineBreakClass.PO:
                        case LineBreakClass.PR:
                            if (this.currentClass == LineBreakClass.NU)
                            {
                                this.lb25ex = false;
                                return false;
                            }

                            LineBreakClass ahead = this.PeekNextCharClass();
                            if (ahead == LineBreakClass.NU && this.nextClass is LineBreakClass.OP or LineBreakClass.HY)
                            {
                                this.lb25ex = false;
                                return false;
                            }

                            break;
                        case LineBreakClass.HY:
                        case LineBreakClass.OP:
                            if (this.currentClass == LineBreakClass.NU)
                            {
                                this.lb25ex = false;
                                return false;
                            }

                            break;

                        case LineBreakClass.NU:
                            if (this.currentClass is LineBreakClass.PO or LineBreakClass.PR or LineBreakClass.NU)
                            {
                                this.lb25ex = false;
                                return false;
                            }

                            break;
                    }

                    if (this.nextClass is LineBreakClass.PR or LineBreakClass.NU)
                    {
                        shouldBreak = true;
                        this.lb25ex = false;
                        break;
                    }
                }

                // LB24
                if (this.lb24ex && (this.nextClass == LineBreakClass.PO || this.nextClass == LineBreakClass.PR))
                {
                    shouldBreak = true;
                    this.lb24ex = false;
                    break;
                }

                // LB18
                shouldBreak = lastClass == LineBreakClass.SP;
                break;

            case LineBreakPairTable.CIBRK:
                shouldBreak = lastClass == LineBreakClass.SP;
                if (!shouldBreak)
                {
                    return false;
                }

                break;

            case LineBreakPairTable.CPBRK: // prohibited for combining marks
                if (lastClass != LineBreakClass.SP)
                {
                    return false;
                }

                break;

            case LineBreakPairTable.PRBRK:
                break;
        }

        // Rule LB22
        if (this.nextClass == LineBreakClass.IN)
        {
            switch (lastClass)
            {
                case LineBreakClass.BK:
                case LineBreakClass.CB:
                case LineBreakClass.EX:
                case LineBreakClass.LF:
                case LineBreakClass.NL:
                case LineBreakClass.SP:
                case LineBreakClass.ZW:

                    // Allow break
                    break;
                case LineBreakClass.CM:
                    if (this.lb22ex)
                    {
                        // Allow break
                        this.lb22ex = false;
                        break;
                    }

                    shouldBreak = false;
                    break;
                default:
                    shouldBreak = false;
                    break;
            }
        }

        if (this.lb8a)
        {
            shouldBreak = false;
        }

        // Rule LB21a
        if (this.lb21a && (this.currentClass == LineBreakClass.HY || this.currentClass == LineBreakClass.BA))
        {
            shouldBreak = false;
            this.lb21a = false;
        }
        else
        {
            this.lb21a = this.currentClass == LineBreakClass.HL;
        }

        // Rule LB30a
        if (this.currentClass == LineBreakClass.RI)
        {
            this.lb30a++;
            if (this.lb30a == 2 && (this.nextClass == LineBreakClass.RI))
            {
                shouldBreak = true;
                this.lb30a = 0;
            }
        }
        else
        {
            this.lb30a = 0;
        }

        // Rule LB30b
        if (this.nextClass == LineBreakClass.EM && this.lastPosition > 0)
        {
            // Mahjong Tiles (Unicode block) are extended pictographics but have a class of ID
            // Unassigned codepoints with Line_Break=ID in some blocks are also assigned the Extended_Pictographic property.
            // Those blocks are intended for future allocation of emoji characters.
            CodePoint cp = CodePoint.DecodeFromUtf16At(this.source, this.lastPosition - 1, out int _);
            if (UnicodeUtility.IsInRangeInclusive((uint)cp.Value, 0x1F000, 0x1F02F))
            {
                shouldBreak = false;
            }
        }

        this.currentClass = this.nextClass;

        return shouldBreak;
    }

    private readonly int FindPriorNonWhitespace(int from)
    {
        if (from > 0)
        {
            CodePoint cp = CodePoint.DecodeFromUtf16At(this.source, from - 1, out int count);
            LineBreakClass cls = CodePoint.GetLineBreakClass(cp);

            if (cls is LineBreakClass.BK or LineBreakClass.LF or LineBreakClass.CR)
            {
                from -= count;
            }
        }

        while (from > 0)
        {
            CodePoint cp = CodePoint.DecodeFromUtf16At(this.source, from - 1, out int count);
            LineBreakClass cls = CodePoint.GetLineBreakClass(cp);

            if (cls == LineBreakClass.SP)
            {
                from -= count;
            }
            else
            {
                break;
            }
        }

        return from;
    }
}
