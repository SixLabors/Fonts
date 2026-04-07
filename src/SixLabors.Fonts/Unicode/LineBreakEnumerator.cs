// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Enumerates potential line break opportunities for a span of text.
/// This is the engine behind the Unicode Line Breaking Algorithm as defined by
/// Unicode Standard Annex #14 (UAX #14):
/// <see href="https://www.unicode.org/reports/tr14/"/>.
///
/// The Unicode rules are the source of truth for the logic below. Comments intentionally
/// call out the UAX rule numbers so readers can cross-check the implementation against the
/// specification instead of treating the local state flags as standalone behavior.
///
/// The implementation walks one code point at a time, keeps a two-code-point window
/// (<see cref="currentClass"/> on the left and <see cref="nextClass"/> on the right), and
/// carries a small amount of extra state for rules that depend on more than the current pair.
/// Methods are pattern-matched by compiler to allow using foreach pattern.
/// </summary>
internal ref struct LineBreakEnumerator
{
    // Iteration state:
    // - charPosition is the UTF-16 offset into the source span.
    // - position is the code point index after the most recently consumed code point.
    // - lastPosition is the candidate break boundary between currentClass and nextClass.
    private readonly ReadOnlySpan<char> source;
    private int charPosition;
    private readonly int pointsLength;
    private int position;
    private int lastPosition;

    // The active pair under consideration. currentClass is the left side of the boundary and
    // nextClass is the right side. Most of UAX #14 reduces to deciding whether that pair breaks.
    private LineBreakClass currentClass;
    private LineBreakClass nextClass;
    private bool first;

    // Tracks whether we are inside an AL/HL/NU run, including trailing combining marks.
    // This is used by LB30, which needs more context than the pair table alone can express.
    private int alphaNumericCount;

    // Stateful rule flags for the UAX #14 rules that cannot be represented by a simple
    // currentClass/nextClass lookup. The field names intentionally mirror the rule numbers
    // so the implementation can be read side by side with the spec.
    private bool lb8a;
    private bool lb21a;
    private bool lb22ex;
    private bool lb24ex;
    private bool lb25ex;
    private bool lb30;
    private int lb30a;
    private bool lb31;

    /// <summary>
    /// Initializes a new line-break enumerator over the supplied UTF-16 text.
    /// </summary>
    /// <param name="source">The source text to inspect for UAX #14 break opportunities.</param>
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

    /// <summary>
    /// Gets the most recently discovered line break opportunity.
    /// </summary>
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
        // Prime the left side of the pair window. After this block the loop below always
        // decides the boundary between currentClass (left) and nextClass (right).
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

            // Required breaks from BK/CR/LF are emitted before any pair-table logic.
            // This matches the UAX handling for explicit line terminators.
            switch (this.currentClass)
            {
                case LineBreakClass.BK:
                case LineBreakClass.CR when this.nextClass != LineBreakClass.LF:
                    this.currentClass = MapFirst(this.nextClass);
                    this.Current = new LineBreak(this.FindPriorNonWhitespace(this.lastPosition), this.lastPosition, true);
                    return true;
            }

            // Handle the classes that have bespoke UAX behavior first, then fall back to the
            // pair table plus the stateful exceptions tracked on this enumerator.
            bool? shouldBreak = this.GetSimpleBreak() ?? (bool?)this.GetPairTableBreak(lastClass);

            // LB8a suppresses breaks after a ZWJ. We record it after processing the current
            // boundary so it applies to the next boundary instead of the one we just decided.
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

            // UAX also exposes an end-of-text boundary. Callers use that final boundary to
            // finalize the trailing line even when no earlier break opportunity was taken.
            this.Current = new LineBreak(this.FindPriorNonWhitespace(this.pointsLength), this.lastPosition, required);
            return true;
        }

        this.Current = default;
        return false;
    }

    /// <summary>
    /// Applies the LB1 class remapping required before any pair-table decisions are made.
    /// </summary>
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

    /// <summary>
    /// Applies the start-of-text normalization required for the first class in the stream.
    /// </summary>
    private static LineBreakClass MapFirst(LineBreakClass c)
        => c switch
        {
            LineBreakClass.LF or LineBreakClass.NL => LineBreakClass.BK,
            LineBreakClass.SP => LineBreakClass.WJ,
            _ => c,
        };

    /// <summary>
    /// Returns <see langword="true"/> for the classes treated as alphanumeric by the
    /// stateful LB30 handling.
    /// </summary>
    private static bool IsAlphaNumeric(LineBreakClass cls)
        => cls is LineBreakClass.AL
        or LineBreakClass.HL
        or LineBreakClass.NU;

    /// <summary>
    /// Reads the next class without advancing the enumerator. This is only used by rules such as
    /// LB25 that need one-code-point lookahead to confirm a numeric punctuation sequence.
    /// </summary>
    private readonly LineBreakClass PeekNextCharClass()
    {
        CodePoint cp = CodePoint.DecodeFromUtf16At(this.source, this.charPosition);
        return MapClass(cp, CodePoint.GetLineBreakClass(cp));
    }

    /// <summary>
    /// Consumes the next code point, applies LB1 class resolution, and updates any stateful
    /// rule flags that depend on more than the current pair.
    /// </summary>
    private LineBreakClass NextCharClass()
    {
        CodePoint cp = CodePoint.DecodeFromUtf16At(this.source, this.charPosition, out int count);
        LineBreakClass cls = MapClass(cp, CodePoint.GetLineBreakClass(cp));
        this.charPosition += count;
        this.position++;

        // Track an alphanumeric run together with any trailing combining marks. LB30 needs to
        // know whether an opening punctuation follows such a run, but once currentClass advances
        // we would otherwise lose that earlier context.
        if (IsAlphaNumeric(this.currentClass) || (this.alphaNumericCount > 0 && cls == LineBreakClass.CM))
        {
            this.alphaNumericCount++;
        }

        // LB22 distinguishes between "CM after one of the explicitly allowed classes" and
        // "CM after anything else" when the next class is IN. Record that now before currentClass
        // collapses to CM on the next iteration.
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

        // LB31 is another context-sensitive rule. We record the contexts that permit a break
        // before an opening punctuation and defer the final decision until OP is nextClass.
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

        // Seed the multi-code-point context used later by LB24.
        if (this.first && (cls == LineBreakClass.CL || cls == LineBreakClass.CP))
        {
            this.lb24ex = true;
        }

        // Seed the multi-code-point context used later by LB25.
        if (this.first
            && (cls == LineBreakClass.CL || cls == LineBreakClass.IS || cls == LineBreakClass.SY))
        {
            this.lb25ex = true;
        }

        // LB25 spans punctuation, spaces, and surrounding numeric runs. Look ahead one code point
        // so punctuation after a space or letter can still participate in the same numeric context.
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

    /// <summary>
    /// Handles the UAX rules for spaces and explicit line terminators before falling back to the
    /// pair table for the ordinary pair-based decisions.
    /// </summary>
    private bool? GetSimpleBreak()
    {
        // These classes are easier to express directly than through the pair table:
        // - spaces never break immediately before themselves,
        // - hard line terminators update currentClass so the next iteration emits a required break.
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

    /// <summary>
    /// Applies the pair-table result and then layers on the stateful UAX exceptions that require
    /// additional context beyond the current pair.
    /// </summary>
    private bool GetPairTableBreak(LineBreakClass lastClass)
    {
        // The pair table is the baseline answer from UAX #14. The rule-specific flags below
        // then tighten or loosen that answer where the spec requires extra context.
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

                    if (lastClass == LineBreakClass.SP
                        && this.nextClass is LineBreakClass.AL or LineBreakClass.HL)
                    {
                        // Once we have already broken at the punctuation-adjacent space, keep the
                        // LB25 state alive only if the following letters immediately continue into
                        // another CL/IS/SY sequence. Otherwise the punctuation context has ended
                        // and must not leak forward to a later AL|NU pair.
                        LineBreakClass ahead = this.PeekNextCharClass();
                        if (ahead is not LineBreakClass.CL and not LineBreakClass.IS and not LineBreakClass.SY)
                        {
                            this.lb25ex = false;
                        }
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

        // Apply the remaining non-pair-table rules in the same place every time so the
        // interaction between pair-table output and rule-specific overrides stays obvious.
        // LB22
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

        // LB8a suppresses a break after ZWJ regardless of the pair-table answer.
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

        // LB30b depends on the Extended_Pictographic property, but Mahjong tiles are a special case:
        // their Line_Break class is ID even though they live in an Extended_Pictographic-adjacent
        // block, so we need an explicit guard here to mirror the Unicode test data.
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

    /// <summary>
    /// Walks backward from a wrap position to the nearest non-breaking trailing content so that
    /// measurement excludes trailing spaces and hard line terminators while wrapping still occurs
    /// at the original boundary.
    /// </summary>
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
