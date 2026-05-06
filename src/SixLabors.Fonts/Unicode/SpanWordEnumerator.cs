// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// An enumerator for retrieving word-boundary segments from a <see cref="ReadOnlySpan{Char}"/>.
/// <br/>
/// Implements the Unicode Word Boundary Algorithm. UAX #29
/// <see href="https://www.unicode.org/reports/tr29/"/>
/// <br/>
/// Methods are pattern-matched by compiler to allow using foreach pattern.
/// </summary>
public ref struct SpanWordEnumerator
{
    private ReadOnlySpan<char> source;
    private int sourceOffset;
    private int codePointOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpanWordEnumerator"/> struct.
    /// </summary>
    /// <param name="source">The buffer to read from.</param>
    public SpanWordEnumerator(ReadOnlySpan<char> source)
    {
        this.source = source;
        this.sourceOffset = 0;
        this.codePointOffset = 0;
        this.Current = default;
    }

    /// <summary>
    /// Gets the element in the collection at the current position of the enumerator.
    /// </summary>
    public WordSegment Current { get; private set; }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that iterates through the collection.</returns>
    public readonly SpanWordEnumerator GetEnumerator() => this;

    /// <summary>
    /// Advances the enumerator to the next element of the collection.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the enumerator was successfully advanced to the next element;
    /// <see langword="false"/> if the enumerator has passed the end of the collection.
    /// </returns>
    public bool MoveNext()
    {
        if (this.source.IsEmpty)
        {
            return false;
        }

        int segmentStart = this.sourceOffset;
        int segmentCodePointStart = this.codePointOffset;
        WordBreakCodePoint current = this.ReadForward(this.sourceOffset);
        int currentEnd = current.Utf16End;
        int boundaryCodePoint = this.codePointOffset + 1;

        while (currentEnd < this.source.Length)
        {
            WordBreakCodePoint next = this.ReadForward(currentEnd);
            if (this.IsBoundary(current, next))
            {
                break;
            }

            current = next;
            currentEnd = current.Utf16End;
            boundaryCodePoint++;
        }

        this.Current = new WordSegment(
            this.source[segmentStart..currentEnd],
            segmentStart,
            segmentCodePointStart,
            boundaryCodePoint - segmentCodePointStart);

        this.sourceOffset = currentEnd;
        this.codePointOffset = boundaryCodePoint;
        return true;
    }

    private readonly bool IsBoundary(in WordBreakCodePoint current, in WordBreakCodePoint next)
    {
        // WB3, WB3a, WB3b, and WB3c are evaluated before the ignore rule, so they
        // use the adjacent code points exactly as they appear in the source.
        if (current.Is(WordBreakClass.CarriageReturn) && next.Is(WordBreakClass.LineFeed))
        {
            return false;
        }

        if (IsNewline(current) || IsNewline(next))
        {
            return true;
        }

        if (current.Is(WordBreakClass.ZeroWidthJoiner)
            && CodePoint.GetGraphemeClusterClass(next.CodePoint) == GraphemeClusterClass.ExtendedPictographic)
        {
            return false;
        }

        if (current.Is(WordBreakClass.WSegSpace) && next.Is(WordBreakClass.WSegSpace))
        {
            return false;
        }

        if (IsIgnored(next))
        {
            return false;
        }

        WordBreakCodePoint left = this.GetEffectivePrevious(current);
        WordBreakClass right = next.Class;

        if (IsAHLetter(left.Class) && IsAHLetter(right))
        {
            return false;
        }

        if (IsAHLetter(left.Class)
            && IsMidLetterMidNumLetQ(right)
            && this.TryGetNextSignificant(next.Utf16End, out WordBreakCodePoint after)
            && IsAHLetter(after.Class))
        {
            return false;
        }

        if (IsAHLetter(right)
            && IsMidLetterMidNumLetQ(left.Class)
            && this.TryGetPreviousSignificant(left.Utf16Start, out WordBreakCodePoint before)
            && IsAHLetter(before.Class))
        {
            return false;
        }

        if (left.Is(WordBreakClass.HebrewLetter) && right == WordBreakClass.SingleQuote)
        {
            return false;
        }

        if (left.Is(WordBreakClass.HebrewLetter)
            && right == WordBreakClass.DoubleQuote
            && this.TryGetNextSignificant(next.Utf16End, out after)
            && after.Is(WordBreakClass.HebrewLetter))
        {
            return false;
        }

        if (right == WordBreakClass.HebrewLetter
            && left.Is(WordBreakClass.DoubleQuote)
            && this.TryGetPreviousSignificant(left.Utf16Start, out before)
            && before.Is(WordBreakClass.HebrewLetter))
        {
            return false;
        }

        if (left.Is(WordBreakClass.Numeric) && right == WordBreakClass.Numeric)
        {
            return false;
        }

        if (IsAHLetter(left.Class) && right == WordBreakClass.Numeric)
        {
            return false;
        }

        if (left.Is(WordBreakClass.Numeric) && IsAHLetter(right))
        {
            return false;
        }

        if (right == WordBreakClass.Numeric
            && IsMidNumMidNumLetQ(left.Class)
            && this.TryGetPreviousSignificant(left.Utf16Start, out before)
            && before.Is(WordBreakClass.Numeric))
        {
            return false;
        }

        if (left.Is(WordBreakClass.Numeric)
            && IsMidNumMidNumLetQ(right)
            && this.TryGetNextSignificant(next.Utf16End, out after)
            && after.Is(WordBreakClass.Numeric))
        {
            return false;
        }

        if (left.Is(WordBreakClass.Katakana) && right == WordBreakClass.Katakana)
        {
            return false;
        }

        if (IsAHLetterNumericKatakanaExtendNumLet(left.Class) && right == WordBreakClass.ExtendNumLet)
        {
            return false;
        }

        if (left.Is(WordBreakClass.ExtendNumLet) && IsAHLetterNumericKatakana(right))
        {
            return false;
        }

        if (left.Is(WordBreakClass.RegionalIndicator)
            && right == WordBreakClass.RegionalIndicator
            && (this.CountRegionalIndicatorsBefore(next.Utf16Start) & 1) == 1)
        {
            return false;
        }

        return true;
    }

    private readonly WordBreakCodePoint GetEffectivePrevious(in WordBreakCodePoint current)
    {
        if (!IsIgnored(current))
        {
            return current;
        }

        int scanEnd = current.Utf16Start;
        while (this.TryReadBackward(scanEnd, out WordBreakCodePoint previous))
        {
            if (!IsIgnored(previous))
            {
                // WB4 deliberately stops ignoring after sot and hard line breaks.
                return IsNewline(previous) ? current : previous;
            }

            scanEnd = previous.Utf16Start;
        }

        return current;
    }

    private readonly int CountRegionalIndicatorsBefore(int utf16End)
    {
        int count = 0;
        int scanEnd = utf16End;
        while (this.TryGetPreviousSignificant(scanEnd, out WordBreakCodePoint previous))
        {
            if (!previous.Is(WordBreakClass.RegionalIndicator))
            {
                break;
            }

            count++;
            scanEnd = previous.Utf16Start;
        }

        return count;
    }

    private readonly bool TryGetPreviousSignificant(int utf16End, out WordBreakCodePoint codePoint)
    {
        int scanEnd = utf16End;
        while (this.TryReadBackward(scanEnd, out codePoint))
        {
            if (!IsIgnored(codePoint))
            {
                return true;
            }

            scanEnd = codePoint.Utf16Start;
        }

        codePoint = default;
        return false;
    }

    private readonly bool TryGetNextSignificant(int utf16Start, out WordBreakCodePoint codePoint)
    {
        int scanStart = utf16Start;
        while (this.TryReadForward(scanStart, out codePoint))
        {
            if (!IsIgnored(codePoint))
            {
                return true;
            }

            scanStart = codePoint.Utf16End;
        }

        codePoint = default;
        return false;
    }

    private readonly WordBreakCodePoint ReadForward(int utf16Start)
    {
        CodePoint codePoint = CodePoint.DecodeFromUtf16At(this.source, utf16Start, out int charsConsumed);
        return new WordBreakCodePoint(codePoint, CodePoint.GetWordBreakClass(codePoint), utf16Start, utf16Start + charsConsumed);
    }

    private readonly bool TryReadForward(int utf16Start, out WordBreakCodePoint codePoint)
    {
        if (utf16Start >= this.source.Length)
        {
            codePoint = default;
            return false;
        }

        codePoint = this.ReadForward(utf16Start);
        return true;
    }

    private readonly bool TryReadBackward(int utf16End, out WordBreakCodePoint codePoint)
    {
        if (utf16End <= 0)
        {
            codePoint = default;
            return false;
        }

        int utf16Start = utf16End - 1;
        if (utf16Start > 0
            && char.IsLowSurrogate(this.source[utf16Start])
            && char.IsHighSurrogate(this.source[utf16Start - 1]))
        {
            utf16Start--;
        }

        codePoint = this.ReadForward(utf16Start);
        return true;
    }

    private static bool IsAHLetter(WordBreakClass cls)
        => cls is WordBreakClass.ALetter or WordBreakClass.HebrewLetter;

    private static bool IsAHLetterNumericKatakana(WordBreakClass cls)
        => IsAHLetter(cls) || cls is WordBreakClass.Numeric or WordBreakClass.Katakana;

    private static bool IsAHLetterNumericKatakanaExtendNumLet(WordBreakClass cls)
        => IsAHLetterNumericKatakana(cls) || cls == WordBreakClass.ExtendNumLet;

    private static bool IsIgnored(in WordBreakCodePoint codePoint) => IsIgnored(codePoint.Class);

    private static bool IsIgnored(WordBreakClass cls)
        => cls is WordBreakClass.Extend or WordBreakClass.Format or WordBreakClass.ZeroWidthJoiner;

    private static bool IsMidLetterMidNumLetQ(WordBreakClass cls)
        => cls is WordBreakClass.MidLetter or WordBreakClass.MidNumLet or WordBreakClass.SingleQuote;

    private static bool IsMidNumMidNumLetQ(WordBreakClass cls)
        => cls is WordBreakClass.MidNum or WordBreakClass.MidNumLet or WordBreakClass.SingleQuote;

    private static bool IsNewline(in WordBreakCodePoint codePoint)
        => codePoint.Class is WordBreakClass.CarriageReturn or WordBreakClass.LineFeed or WordBreakClass.Newline;

    private readonly struct WordBreakCodePoint
    {
        public WordBreakCodePoint(CodePoint codePoint, WordBreakClass cls, int utf16Start, int utf16End)
        {
            this.CodePoint = codePoint;
            this.Class = cls;
            this.Utf16Start = utf16Start;
            this.Utf16End = utf16End;
        }

        public CodePoint CodePoint { get; }

        public WordBreakClass Class { get; }

        public int Utf16Start { get; }

        public int Utf16End { get; }

        public bool Is(WordBreakClass cls) => this.Class == cls;
    }
}
