// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Enumerates potential line break opportunities for a span of text.
/// This is the engine behind the Unicode Line Breaking Algorithm as defined by
/// Unicode Standard Annex #14 (UAX #14):
/// <see href="https://www.unicode.org/reports/tr14/"/>.
/// The implementation keeps a three-code-point window over the input and applies
/// the LB rules in specification order. Each rule method is named after the
/// corresponding UAX #14 rule so the code can be reviewed against the standard.
/// </summary>
internal ref struct LineBreakEnumerator
{
    /// <summary>
    /// Sentinel value representing start of text for LB2 and context checks.
    /// </summary>
    private const int StartOfText = -1;

    /// <summary>
    /// Sentinel value representing end of text for LB3.
    /// </summary>
    private const int EndOfText = -2;

    /// <summary>
    /// U+25CC DOTTED CIRCLE. LB28a treats it as an aksara base for Indic conjunct handling.
    /// </summary>
    private const int DottedCircle = 0x25CC;

    /// <summary>
    /// U+002F SOLIDUS. Used by layout-level URL tailoring around slash-separated path segments.
    /// </summary>
    private const int Solidus = 0x002F;

    /// <summary>
    /// U+003A COLON. Used to recognize URI scheme markers.
    /// </summary>
    private const int Colon = 0x003A;

    /// <summary>
    /// U+002E FULL STOP. Used to recognize <c>www.</c> host prefixes and URI scheme characters.
    /// </summary>
    private const int FullStop = 0x002E;

    /// <summary>
    /// U+002D HYPHEN-MINUS. Valid inside URI schemes and host labels.
    /// </summary>
    private const int HyphenMinus = 0x002D;

    /// <summary>
    /// U+002B PLUS SIGN. Valid inside URI schemes.
    /// </summary>
    private const int PlusSign = 0x002B;

    /// <summary>
    /// U+0057 LATIN CAPITAL LETTER W. Used by the ASCII <c>www.</c> recognizer.
    /// </summary>
    private const int UppercaseW = 0x0057;

    /// <summary>
    /// U+0077 LATIN SMALL LETTER W. Used by the ASCII <c>www.</c> recognizer.
    /// </summary>
    private const int LowercaseW = 0x0077;

    /// <summary>
    /// U+0022 QUOTATION MARK. Treated as a hard boundary while recognizing URL-like runs.
    /// </summary>
    private const int QuotationMark = 0x0022;

    /// <summary>
    /// U+0027 APOSTROPHE. Treated as a hard boundary while recognizing URL-like runs.
    /// </summary>
    private const int Apostrophe = 0x0027;

    /// <summary>
    /// U+003C LESS-THAN SIGN. Treated as a hard boundary while recognizing URL-like runs.
    /// </summary>
    private const int LessThanSign = 0x003C;

    /// <summary>
    /// U+003E GREATER-THAN SIGN. Treated as a hard boundary while recognizing URL-like runs.
    /// </summary>
    private const int GreaterThanSign = 0x003E;

    /// <summary>
    /// The UTF-16 source being inspected. The ref struct keeps this span without allocating.
    /// </summary>
    private readonly ReadOnlySpan<char> source;

    /// <summary>
    /// Enables layout-only URL tailoring. The public constructor leaves this disabled so Unicode
    /// conformance tests see the un-tailored UAX #14 result.
    /// </summary>
    private readonly bool tailorUrls;

    /// <summary>
    /// UTF-16 offset of the next code point to decode from <see cref="source"/>.
    /// </summary>
    private int charPosition;

    /// <summary>
    /// Code point index immediately after the last decoded code point.
    /// </summary>
    private int pointPosition;

    /// <summary>
    /// Tracks whether the artificial end-of-text sentinel has been pushed into the rule window.
    /// </summary>
    private bool endOfTextPushed;

    /// <summary>
    /// The last emitted wrap position. This prevents LB3 from emitting a duplicate final break.
    /// </summary>
    private int previousBreakPosition;

    /// <summary>
    /// The code point immediately before <see cref="current"/> in the rule window.
    /// </summary>
    private LineBreakCodePoint previous;

    /// <summary>
    /// The left side of the boundary currently being evaluated.
    /// </summary>
    private LineBreakCodePoint current;

    /// <summary>
    /// The right side of the boundary currently being evaluated.
    /// </summary>
    private LineBreakCodePoint next;

    /// <summary>
    /// State for LB8. A zero width space followed by spaces permits a break after the space run.
    /// </summary>
    private bool lb8;

    /// <summary>
    /// State shared by rules that suppress breaks across a following run of spaces.
    /// </summary>
    private bool spaces;

    /// <summary>
    /// Count of consecutive regional-indicator pairs used by LB30a.
    /// </summary>
    private int regionalIndicatorCount;

    /// <summary>
    /// Streaming recognizer state used only when <see cref="tailorUrls"/> is enabled.
    /// </summary>
    private UrlTailoringState urlTailoringState;

    /// <summary>
    /// Initializes a new instance of the <see cref="LineBreakEnumerator"/> struct.
    /// </summary>
    /// <param name="source">The source text to inspect for UAX #14 break opportunities.</param>
    public LineBreakEnumerator(ReadOnlySpan<char> source)
        : this(source, false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LineBreakEnumerator"/> struct.
    /// </summary>
    /// <param name="source">The source text to inspect for line break opportunities.</param>
    /// <param name="tailorUrls">Whether to apply layout-level URL solidus tailoring.</param>
    internal LineBreakEnumerator(ReadOnlySpan<char> source, bool tailorUrls)
        : this()
    {
        this.source = source;
        this.tailorUrls = tailorUrls;
        this.previous = LineBreakCodePoint.CreateSentinel(StartOfText, 0, 0);
        this.current = LineBreakCodePoint.CreateSentinel(StartOfText, 0, 0);
        this.next = LineBreakCodePoint.CreateSentinel(StartOfText, 0, 0);
    }

    private enum BreakAction
    {
        /// <summary>
        /// The rule did not apply; continue evaluating later rules.
        /// </summary>
        Pass,

        /// <summary>
        /// The rule forbids a break at the current boundary.
        /// </summary>
        NoBreak,

        /// <summary>
        /// The rule permits an optional break at the current boundary.
        /// </summary>
        MayBreak,

        /// <summary>
        /// The rule requires a break at the current boundary.
        /// </summary>
        MustBreak
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
        while (true)
        {
            if (this.charPosition < this.source.Length)
            {
                this.Push(this.ReadNext());
            }
            else if (!this.endOfTextPushed)
            {
                this.Push(LineBreakCodePoint.CreateSentinel(EndOfText, this.next.Length, this.next.CharEnd));
                this.endOfTextPushed = true;
            }
            else
            {
                this.Current = default;
                return false;
            }

            BreakAction action = this.GetBreakAction();
            if (this.tailorUrls)
            {
                action = this.ApplyUrlTailoring(action);
            }

            switch (action)
            {
                case BreakAction.NoBreak:
                case BreakAction.Pass:
                    break;

                case BreakAction.MayBreak:
                case BreakAction.MustBreak:
                    this.Current = new LineBreak(
                        this.FindPriorNonWhitespace(this.current),
                        this.current.Length,
                        action == BreakAction.MustBreak);
                    this.previousBreakPosition = this.current.Length;
                    return true;

                default:
                    throw new InvalidOperationException($"Invalid line break action {action}.");
            }
        }
    }

    /// <summary>
    /// Decodes the next UTF-16 code point, maps its line break class according to LB1,
    /// and packages the additional context needed by later rules.
    /// </summary>
    private LineBreakCodePoint ReadNext()
    {
        int charStart = this.charPosition;
        CodePoint codePoint = CodePoint.DecodeFromUtf16At(this.source, charStart, out int charsConsumed);
        UnicodeCategory category = CodePoint.GetGeneralCategory(codePoint);
        LineBreakClass cls = MapClass(CodePoint.GetLineBreakClass(codePoint), category);
        bool isUrlLikeRun = this.tailorUrls && this.urlTailoringState.Update(codePoint);

        this.charPosition += charsConsumed;
        this.pointPosition++;

        return new LineBreakCodePoint(
            codePoint,
            cls,
            category,
            this.pointPosition,
            charStart,
            this.charPosition,
            isUrlLikeRun);
    }

    /// <summary>
    /// Applies the LB1 class remapping required before any rule decisions are made.
    /// </summary>
    /// <remarks>
    /// LB1 resolves ambiguous, surrogate, unknown, complex-context, and conditional Japanese
    /// starter classes before the rest of the rule chain observes them:
    /// AI/SG/XX to AL, SA to CM or AL based on general category, and CJ to NS.
    /// </remarks>
    private static LineBreakClass MapClass(LineBreakClass c, UnicodeCategory category)
        => c switch
        {
            LineBreakClass.Ambiguous or LineBreakClass.Surrogate or LineBreakClass.Unknown => LineBreakClass.Alphabetic,
            LineBreakClass.ComplexContext => category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark
                ? LineBreakClass.CombiningMark
                : LineBreakClass.Alphabetic,
            LineBreakClass.ConditionalJapaneseStarter => LineBreakClass.Nonstarter,
            _ => c
        };

    /// <summary>
    /// Applies the layout URL tailoring from UAX #14 section 8 while preserving the default
    /// enumerator behavior for callers that need strict Unicode conformance.
    /// </summary>
    /// <remarks>
    /// The tailoring suppresses ordinary layout breaks at a solidus unless the current token has
    /// already been recognized as URL-like. It also adds the URL numeric path case that default
    /// LB25 intentionally blocks, for example the boundary after <c>2024/</c> in
    /// <c>https://example/2024/05</c>.
    /// </remarks>
    private readonly BreakAction ApplyUrlTailoring(BreakAction action)
    {
        if (action == BreakAction.MustBreak || this.current.IsSentinel)
        {
            return action;
        }

        if (this.next.HasValue(Solidus))
        {
            return BreakAction.NoBreak;
        }

        if (!this.current.HasValue(Solidus))
        {
            return action;
        }

        if (!this.current.IsUrlLikeRun)
        {
            return BreakAction.NoBreak;
        }

        if (action == BreakAction.NoBreak
            && !this.previous.IsSentinel
            && !this.next.IsSentinel
            && CodePoint.IsDigit(this.previous.CodePoint)
            && CodePoint.IsDigit(this.next.CodePoint))
        {
            return BreakAction.MayBreak;
        }

        return action;
    }

    /// <summary>
    /// Advances the three-code-point rule window. Ignored combining marks and zero width joiners
    /// from LB9 are folded into the current position instead of becoming a new boundary.
    /// </summary>
    private void Push(LineBreakCodePoint codePoint)
    {
        if (this.next.Ignored)
        {
            this.current.Length = this.next.Length;
            this.current.CharEnd = this.next.CharEnd;
        }
        else
        {
            this.previous = this.current;
            this.current = this.next;
        }

        this.next = codePoint;
    }

    /// <summary>
    /// Evaluates the UAX #14 rules in order for the boundary between
    /// <see cref="current"/> and <see cref="next"/>.
    /// </summary>
    /// <remarks>
    /// The first rule to return anything other than <see cref="BreakAction.Pass"/> decides the
    /// boundary. If no rule prevents a break, LB31 is represented by the final
    /// <see cref="BreakAction.MayBreak"/> return.
    /// </remarks>
    private BreakAction GetBreakAction()
    {
        BreakAction action;

        action = this.LB02();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB03();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB04();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB05();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB06();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LBSpacesStop();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB07();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB08();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB08a();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB09();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        this.LB10();

        action = this.LB11();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB12();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB12a();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB13();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB14();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB15a();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB15b();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB15c();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB15d();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB16();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB17();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB18();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB19();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB19a();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB20();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB20a();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB21a();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB21();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB21b();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB22();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB23();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB23a();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB24();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB25();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB26();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB27();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB28();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB28a();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB29();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB30();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB30a();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        action = this.LB30b();
        if (action != BreakAction.Pass)
        {
            return action;
        }

        // LB31: Break everywhere else.
        return BreakAction.MayBreak;
    }

    /// <summary>
    /// LB2: Never break at the start of text.
    /// </summary>
    private readonly BreakAction LB02()
        => this.current.IsStartOfText && !this.next.IsEndOfText
            ? BreakAction.NoBreak
            : BreakAction.Pass;

    /// <summary>
    /// LB3: Always break at the end of text.
    /// </summary>
    private readonly BreakAction LB03()
        => this.next.IsEndOfText && (this.current.Length == 0 || this.current.Length != this.previousBreakPosition)
            ? BreakAction.MayBreak
            : BreakAction.Pass;

    /// <summary>
    /// LB4: Always break after a mandatory break character.
    /// </summary>
    private readonly BreakAction LB04()
        => this.current.Is(LineBreakClass.MandatoryBreak) ? BreakAction.MustBreak : BreakAction.Pass;

    /// <summary>
    /// LB5: Treat CR followed by LF as an indivisible newline; otherwise break after CR, LF, and NL.
    /// </summary>
    private readonly BreakAction LB05()
    {
        if (this.current.Is(LineBreakClass.CarriageReturn))
        {
            return this.next.Is(LineBreakClass.LineFeed) ? BreakAction.NoBreak : BreakAction.MustBreak;
        }

        return this.current.Is(LineBreakClass.LineFeed) || this.current.Is(LineBreakClass.NextLine)
            ? BreakAction.MustBreak
            : BreakAction.Pass;
    }

    /// <summary>
    /// LB6: Do not break before mandatory break characters.
    /// </summary>
    private readonly BreakAction LB06()
        => this.next.Is(LineBreakClass.MandatoryBreak)
            || this.next.Is(LineBreakClass.CarriageReturn)
            || this.next.Is(LineBreakClass.LineFeed)
            || this.next.Is(LineBreakClass.NextLine)
            ? BreakAction.NoBreak
            : BreakAction.Pass;

    /// <summary>
    /// Internal space-run handling for rules that suppress a break until after spaces have been consumed.
    /// </summary>
    /// <remarks>
    /// This is not a standalone UAX rule. It carries the "do not break inside the intervening spaces"
    /// part of LB8, LB14, LB15a, LB16, and LB17 after the rule that started the space run has fired.
    /// It also resets the LB30a regional-indicator count whenever the current code point is not RI.
    /// </remarks>
    private BreakAction LBSpacesStop()
    {
        if (!this.current.Is(LineBreakClass.RegionalIndicator))
        {
            this.regionalIndicatorCount = 0;
        }

        if (this.spaces)
        {
            if (!this.next.Is(LineBreakClass.Space))
            {
                this.spaces = false;
            }

            return BreakAction.NoBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB7: Do not break before spaces or zero width space.
    /// </summary>
    /// <remarks>
    /// The exceptions for ZW, OP, QU, CL, CP, and B2 are handled by their later dedicated rules.
    /// </remarks>
    private readonly BreakAction LB07()
    {
        if (this.next.Is(LineBreakClass.ZeroWidthSpace))
        {
            return BreakAction.NoBreak;
        }

        if (this.next.Is(LineBreakClass.Space)
            && !this.current.Is(LineBreakClass.ZeroWidthSpace)
            && !this.current.Is(LineBreakClass.OpenPunctuation)
            && !this.current.Is(LineBreakClass.Quotation)
            && !this.current.Is(LineBreakClass.ClosePunctuation)
            && !this.current.Is(LineBreakClass.CloseParenthesis)
            && !this.current.Is(LineBreakClass.BreakBeforeAndAfter))
        {
            return BreakAction.NoBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB8: Break before any character following a zero width space, even if spaces intervene.
    /// </summary>
    private BreakAction LB08()
    {
        if (this.lb8)
        {
            this.lb8 = false;
            return BreakAction.MayBreak;
        }

        if (this.current.Is(LineBreakClass.ZeroWidthSpace))
        {
            if (this.next.Is(LineBreakClass.Space))
            {
                this.lb8 = true;
                return BreakAction.NoBreak;
            }

            return BreakAction.MayBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB8a: Do not break after a zero width joiner.
    /// </summary>
    private readonly BreakAction LB08a()
        => this.current.Is(LineBreakClass.ZeroWidthJoiner) ? BreakAction.NoBreak : BreakAction.Pass;

    /// <summary>
    /// LB9: Do not break a combining mark or zero width joiner away from its base character.
    /// </summary>
    /// <remarks>
    /// When LB9 applies, the right-side code point is marked as ignored so <see cref="Push"/>
    /// folds it into the current logical position and later boundaries see the combined item.
    /// </remarks>
    private BreakAction LB09()
    {
        if (!IsBkCrLfNlSpZw(this.current)
            && (this.next.Is(LineBreakClass.CombiningMark) || this.next.Is(LineBreakClass.ZeroWidthJoiner)))
        {
            this.next.Ignored = true;
            return BreakAction.NoBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB10: Treat any remaining combining marks or zero width joiners as alphabetic.
    /// </summary>
    /// <remarks>
    /// LB10 is a class rewrite rather than a boundary decision, so it does not return a
    /// <see cref="BreakAction"/>.
    /// </remarks>
    private void LB10()
    {
        if (this.current.Is(LineBreakClass.CombiningMark) || this.current.Is(LineBreakClass.ZeroWidthJoiner))
        {
            this.current.Class = LineBreakClass.Alphabetic;
        }

        if (this.next.Is(LineBreakClass.CombiningMark) || this.next.Is(LineBreakClass.ZeroWidthJoiner))
        {
            this.next.Class = LineBreakClass.Alphabetic;
        }
    }

    /// <summary>
    /// LB11: Do not break before or after word joiner.
    /// </summary>
    private readonly BreakAction LB11()
        => this.next.Is(LineBreakClass.WordJoiner) || this.current.Is(LineBreakClass.WordJoiner)
            ? BreakAction.NoBreak
            : BreakAction.Pass;

    /// <summary>
    /// LB12: Do not break after a glue character.
    /// </summary>
    private readonly BreakAction LB12()
        => this.current.Is(LineBreakClass.Glue) ? BreakAction.NoBreak : BreakAction.Pass;

    /// <summary>
    /// LB12a: Do not break before a glue character except after spaces, break-after, hyphen, or Hebrew hyphen.
    /// </summary>
    private readonly BreakAction LB12a()
    {
        if (this.next.Is(LineBreakClass.Glue)
            && !this.current.Is(LineBreakClass.Space)
            && !this.current.Is(LineBreakClass.BreakAfter)
            && !this.current.Is(LineBreakClass.Hyphen)
            && !this.current.Is(LineBreakClass.UnambiguousHyphen))
        {
            return BreakAction.NoBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB13: Do not break before closing punctuation, closing parenthesis, exclamation/interrogation,
    /// or inseparable symbols.
    /// </summary>
    private readonly BreakAction LB13()
        => this.next.Is(LineBreakClass.ClosePunctuation)
            || this.next.Is(LineBreakClass.CloseParenthesis)
            || this.next.Is(LineBreakClass.Exclamation)
            || this.next.Is(LineBreakClass.BreakSymbols)
            ? BreakAction.NoBreak
            : BreakAction.Pass;

    /// <summary>
    /// LB14: Do not break after an opening punctuation, even after intervening spaces.
    /// </summary>
    private BreakAction LB14()
    {
        if (this.current.Is(LineBreakClass.OpenPunctuation))
        {
            if (this.next.Is(LineBreakClass.Space))
            {
                this.spaces = true;
            }

            return BreakAction.NoBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB15a: Do not break after an initial quotation mark following a start-like context,
    /// even after intervening spaces.
    /// </summary>
    private BreakAction LB15a()
    {
        if (IsSotBkCrLfNlOpQuGlSpZw(this.previous)
            && this.current.Is(LineBreakClass.Quotation)
            && this.current.Category == UnicodeCategory.InitialQuotePunctuation)
        {
            this.spaces = true;
            return BreakAction.NoBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB15b: Do not break before a final quotation mark when it closes a quotation-like run.
    /// </summary>
    private readonly BreakAction LB15b()
    {
        if (this.next.Is(LineBreakClass.Quotation)
            && this.next.Category == UnicodeCategory.FinalQuotePunctuation)
        {
            if (!this.TryGetAfterNext(out LineBreakCodePoint after) || IsSpGlWjClQuCpExIsSyBkCrLfNlZw(after))
            {
                return BreakAction.NoBreak;
            }
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB15c: Permit a break between a space and an inseparable separator before a number.
    /// </summary>
    private readonly BreakAction LB15c()
    {
        if (this.current.Is(LineBreakClass.Space)
            && this.next.Is(LineBreakClass.InfixNumeric)
            && this.TryGetAfterNext(out LineBreakCodePoint after)
            && after.Is(LineBreakClass.Numeric))
        {
            return BreakAction.MayBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB15d: Do not break before inseparable separators in other contexts.
    /// </summary>
    private readonly BreakAction LB15d()
        => this.next.Is(LineBreakClass.InfixNumeric) ? BreakAction.NoBreak : BreakAction.Pass;

    /// <summary>
    /// LB16: Do not break between closing punctuation or closing parenthesis and a nonstarter,
    /// even with intervening spaces.
    /// </summary>
    private BreakAction LB16()
    {
        if (this.current.Is(LineBreakClass.ClosePunctuation) || this.current.Is(LineBreakClass.CloseParenthesis))
        {
            if (this.ClassAfterSpacesIs(this.current.CharEnd, LineBreakClass.Nonstarter))
            {
                if (this.next.Is(LineBreakClass.Space))
                {
                    this.spaces = true;
                }

                return BreakAction.NoBreak;
            }

            if (this.next.Is(LineBreakClass.Space))
            {
                return BreakAction.NoBreak;
            }
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB17: Do not break within balanced punctuation pairs, even with intervening spaces.
    /// </summary>
    private BreakAction LB17()
    {
        if (this.current.Is(LineBreakClass.BreakBeforeAndAfter))
        {
            if (this.ClassAfterSpacesIs(this.current.CharEnd, LineBreakClass.BreakBeforeAndAfter))
            {
                if (!this.next.Is(LineBreakClass.Space))
                {
                    return BreakAction.NoBreak;
                }

                this.spaces = true;
                return BreakAction.NoBreak;
            }

            if (this.next.Is(LineBreakClass.Space))
            {
                return BreakAction.NoBreak;
            }
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB18: Break after spaces.
    /// </summary>
    private readonly BreakAction LB18()
        => this.current.Is(LineBreakClass.Space) ? BreakAction.MayBreak : BreakAction.Pass;

    /// <summary>
    /// LB19: Do not break before or after quotation marks.
    /// </summary>
    /// <remarks>
    /// Initial and final quotation categories are handled by LB15a and LB15b where the standard
    /// gives them more specific behavior.
    /// </remarks>
    private readonly BreakAction LB19()
    {
        if (this.next.Is(LineBreakClass.Quotation) && this.next.Category != UnicodeCategory.InitialQuotePunctuation)
        {
            return BreakAction.NoBreak;
        }

        if (this.current.Is(LineBreakClass.Quotation) && this.current.Category != UnicodeCategory.FinalQuotePunctuation)
        {
            return BreakAction.NoBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB19a: Applies the East Asian quotation mark tailoring used by the Unicode line break tests.
    /// </summary>
    private readonly BreakAction LB19a()
    {
        if (!IsEastAsian(this.current) && this.next.Is(LineBreakClass.Quotation))
        {
            return BreakAction.NoBreak;
        }

        if (this.next.Is(LineBreakClass.Quotation)
            && (!this.TryGetAfterNext(out LineBreakCodePoint after) || !IsEastAsian(after)))
        {
            return BreakAction.NoBreak;
        }

        if (this.current.Is(LineBreakClass.Quotation) && !IsEastAsian(this.next))
        {
            return BreakAction.NoBreak;
        }

        if ((this.previous.IsStartOfText || !IsEastAsian(this.previous))
            && this.current.Is(LineBreakClass.Quotation))
        {
            return BreakAction.NoBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB20: Break before and after contingent break characters.
    /// </summary>
    private readonly BreakAction LB20()
        => this.current.Is(LineBreakClass.ContingentBreak) || this.next.Is(LineBreakClass.ContingentBreak)
            ? BreakAction.MayBreak
            : BreakAction.Pass;

    /// <summary>
    /// LB20a: Do not break after a leading hyphen or Hebrew hyphen before alphabetic text.
    /// </summary>
    private readonly BreakAction LB20a()
    {
        if (IsSotBkCrLfNlSpZwCbGl(this.previous)
            && (this.current.Is(LineBreakClass.Hyphen) || this.current.Is(LineBreakClass.UnambiguousHyphen))
            && (this.next.Is(LineBreakClass.Alphabetic) || this.next.Is(LineBreakClass.HebrewLetter)))
        {
            return BreakAction.NoBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB21: Do not break before break-after, hyphen, Hebrew hyphen, or nonstarter;
    /// do not break after break-before.
    /// </summary>
    private readonly BreakAction LB21()
    {
        if (this.current.Is(LineBreakClass.BreakBefore)
            || this.next.Is(LineBreakClass.BreakAfter)
            || this.next.Is(LineBreakClass.UnambiguousHyphen)
            || this.next.Is(LineBreakClass.Hyphen)
            || this.next.Is(LineBreakClass.Nonstarter))
        {
            return BreakAction.NoBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB21a: Do not break after Hebrew letters followed by hyphen or Hebrew hyphen.
    /// </summary>
    private readonly BreakAction LB21a()
    {
        if (this.previous.Is(LineBreakClass.HebrewLetter)
            && (this.current.Is(LineBreakClass.Hyphen) || this.current.Is(LineBreakClass.UnambiguousHyphen))
            && !this.next.Is(LineBreakClass.HebrewLetter))
        {
            return BreakAction.NoBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB21b: Do not break between solidus-like symbols and Hebrew letters.
    /// </summary>
    private readonly BreakAction LB21b()
        => this.current.Is(LineBreakClass.BreakSymbols) && this.next.Is(LineBreakClass.HebrewLetter)
            ? BreakAction.NoBreak
            : BreakAction.Pass;

    /// <summary>
    /// LB22: Do not break before ellipses and other inseparable characters.
    /// </summary>
    private readonly BreakAction LB22()
        => this.next.Is(LineBreakClass.Inseparable) ? BreakAction.NoBreak : BreakAction.Pass;

    /// <summary>
    /// LB23: Do not break between letters and numbers.
    /// </summary>
    private readonly BreakAction LB23()
    {
        if ((this.current.Is(LineBreakClass.Alphabetic) || this.current.Is(LineBreakClass.HebrewLetter))
            && this.next.Is(LineBreakClass.Numeric))
        {
            return BreakAction.NoBreak;
        }

        if (this.current.Is(LineBreakClass.Numeric)
            && (this.next.Is(LineBreakClass.Alphabetic) || this.next.Is(LineBreakClass.HebrewLetter)))
        {
            return BreakAction.NoBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB23a: Do not break between numeric prefixes or postfixes and ideographs, emoji bases,
    /// or emoji modifiers.
    /// </summary>
    private readonly BreakAction LB23a()
    {
        if (this.current.Is(LineBreakClass.PrefixNumeric) && IsIdEbEm(this.next))
        {
            return BreakAction.NoBreak;
        }

        if (this.next.Is(LineBreakClass.PostfixNumeric) && IsIdEbEm(this.current))
        {
            return BreakAction.NoBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB24: Do not break between numeric prefixes or postfixes and alphabetic letters.
    /// </summary>
    private readonly BreakAction LB24()
    {
        if ((this.current.Is(LineBreakClass.PrefixNumeric) || this.current.Is(LineBreakClass.PostfixNumeric))
            && (this.next.Is(LineBreakClass.Alphabetic) || this.next.Is(LineBreakClass.HebrewLetter)))
        {
            return BreakAction.NoBreak;
        }

        if ((this.current.Is(LineBreakClass.Alphabetic) || this.current.Is(LineBreakClass.HebrewLetter))
            && (this.next.Is(LineBreakClass.PrefixNumeric) || this.next.Is(LineBreakClass.PostfixNumeric)))
        {
            return BreakAction.NoBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB25: Do not break within numeric expressions.
    /// </summary>
    /// <remarks>
    /// This implementation covers the multi-code-point contexts from the rule by looking backward
    /// across SY/IS separators and forward across optional opening punctuation. The layout URL
    /// tailoring can later reintroduce a narrow solidus break inside recognized URL path segments.
    /// </remarks>
    private readonly BreakAction LB25()
    {
        bool hasNumericScanEnd = false;
        int numericScanCharEnd = 0;
        if (this.next.Is(LineBreakClass.PostfixNumeric) || this.next.Is(LineBreakClass.PrefixNumeric))
        {
            numericScanCharEnd = this.current.Is(LineBreakClass.ClosePunctuation) || this.current.Is(LineBreakClass.CloseParenthesis)
                ? this.previous.CharEnd
                : this.current.CharEnd;
            hasNumericScanEnd = true;
        }
        else if (this.next.Is(LineBreakClass.Numeric))
        {
            numericScanCharEnd = this.current.CharEnd;
            hasNumericScanEnd = true;
        }

        if (hasNumericScanEnd)
        {
            int scanCharEnd = numericScanCharEnd;
            while (this.TryReadBackward(scanCharEnd, out LineBreakCodePoint codePoint))
            {
                if (codePoint.Is(LineBreakClass.BreakSymbols) || codePoint.Is(LineBreakClass.InfixNumeric))
                {
                    scanCharEnd = codePoint.CharStart;
                    continue;
                }

                if (codePoint.Is(LineBreakClass.Numeric))
                {
                    return BreakAction.NoBreak;
                }

                break;
            }
        }

        if (this.current.Is(LineBreakClass.PostfixNumeric) || this.current.Is(LineBreakClass.PrefixNumeric))
        {
            if (this.next.Is(LineBreakClass.OpenPunctuation))
            {
                if (this.TryGetAfterNext(out LineBreakCodePoint after))
                {
                    if (after.Is(LineBreakClass.Numeric))
                    {
                        return BreakAction.NoBreak;
                    }

                    if (after.Is(LineBreakClass.InfixNumeric)
                        && this.TryGetAfterNext(out LineBreakCodePoint afterAfter, 2)
                        && afterAfter.Is(LineBreakClass.Numeric))
                    {
                        return BreakAction.NoBreak;
                    }
                }
            }
            else if (this.next.Is(LineBreakClass.Numeric))
            {
                return BreakAction.NoBreak;
            }
        }

        if (this.current.Is(LineBreakClass.Hyphen) && this.next.Is(LineBreakClass.Numeric))
        {
            return BreakAction.NoBreak;
        }

        if (this.current.Is(LineBreakClass.InfixNumeric) && this.next.Is(LineBreakClass.Numeric))
        {
            return BreakAction.NoBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB26: Do not break a Korean syllable block.
    /// </summary>
    private readonly BreakAction LB26()
    {
        if (this.current.Is(LineBreakClass.HangulLeadJamo) && IsJlJvH2H3(this.next))
        {
            return BreakAction.NoBreak;
        }

        if ((this.current.Is(LineBreakClass.HangulVowelJamo) || this.current.Is(LineBreakClass.HangulLeadVowelSyllable))
            && (this.next.Is(LineBreakClass.HangulVowelJamo) || this.next.Is(LineBreakClass.HangulTailJamo)))
        {
            return BreakAction.NoBreak;
        }

        if ((this.current.Is(LineBreakClass.HangulTailJamo) || this.current.Is(LineBreakClass.HangulLeadVowelTailSyllable))
            && this.next.Is(LineBreakClass.HangulTailJamo))
        {
            return BreakAction.NoBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB27: Treat Korean syllable blocks like ideographs for numeric prefix and postfix handling.
    /// </summary>
    private readonly BreakAction LB27()
    {
        if (IsJlJvJtH2H3(this.current) && this.next.Is(LineBreakClass.PostfixNumeric))
        {
            return BreakAction.NoBreak;
        }

        if (this.current.Is(LineBreakClass.PrefixNumeric) && IsJlJvJtH2H3(this.next))
        {
            return BreakAction.NoBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB28: Do not break between alphabetic letters.
    /// </summary>
    private readonly BreakAction LB28()
        => (this.current.Is(LineBreakClass.Alphabetic) || this.current.Is(LineBreakClass.HebrewLetter))
            && (this.next.Is(LineBreakClass.Alphabetic) || this.next.Is(LineBreakClass.HebrewLetter))
            ? BreakAction.NoBreak
            : BreakAction.Pass;

    /// <summary>
    /// LB28a: Do not break inside orthographic syllables for Brahmic scripts.
    /// </summary>
    /// <remarks>
    /// This is the Unicode 15+ aksara rule family. It keeps aksara bases, viramas, invisible
    /// stackers, and following bases together, with U+25CC DOTTED CIRCLE treated as a base.
    /// </remarks>
    private readonly BreakAction LB28a()
    {
        if (this.current.Is(LineBreakClass.AksaraPrebase) && IsAksaraBase(this.next))
        {
            return BreakAction.NoBreak;
        }

        if (IsAksaraBase(this.current)
            && (this.next.Is(LineBreakClass.ViramaFinal) || this.next.Is(LineBreakClass.Virama)))
        {
            return BreakAction.NoBreak;
        }

        if (IsAksaraBase(this.previous)
            && this.current.Is(LineBreakClass.Virama)
            && (this.next.Is(LineBreakClass.Aksara) || this.next.CodePoint.Value == DottedCircle))
        {
            return BreakAction.NoBreak;
        }

        if (IsAksaraBase(this.current)
            && IsAksaraBase(this.next)
            && this.TryGetAfterNext(out LineBreakCodePoint after)
            && after.Is(LineBreakClass.ViramaFinal))
        {
            return BreakAction.NoBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB29: Do not break between numeric punctuation and alphabetic letters.
    /// </summary>
    private readonly BreakAction LB29()
        => this.current.Is(LineBreakClass.InfixNumeric)
            && (this.next.Is(LineBreakClass.Alphabetic) || this.next.Is(LineBreakClass.HebrewLetter))
            ? BreakAction.NoBreak
            : BreakAction.Pass;

    /// <summary>
    /// LB30: Do not break between letters or numbers and non-East-Asian opening or closing punctuation.
    /// </summary>
    private readonly BreakAction LB30()
    {
        if ((this.current.Is(LineBreakClass.Alphabetic)
            || this.current.Is(LineBreakClass.HebrewLetter)
            || this.current.Is(LineBreakClass.Numeric))
            && this.next.Is(LineBreakClass.OpenPunctuation)
            && !IsEastAsian(this.next))
        {
            return BreakAction.NoBreak;
        }

        if (this.current.Is(LineBreakClass.CloseParenthesis)
            && !IsEastAsian(this.current)
            && (this.next.Is(LineBreakClass.Alphabetic)
                || this.next.Is(LineBreakClass.HebrewLetter)
                || this.next.Is(LineBreakClass.Numeric)))
        {
            return BreakAction.NoBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB30a: Break between regional indicator symbols only at even boundaries.
    /// </summary>
    /// <remarks>
    /// This keeps flag emoji pairs together by forbidding the first RI/RI boundary in each run and
    /// allowing the next one.
    /// </remarks>
    private BreakAction LB30a()
    {
        if (this.current.Is(LineBreakClass.RegionalIndicator) && this.next.Is(LineBreakClass.RegionalIndicator))
        {
            this.regionalIndicatorCount++;
            if (this.regionalIndicatorCount % 2 != 0)
            {
                return BreakAction.NoBreak;
            }
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// LB30b: Do not break between emoji base characters and emoji modifiers.
    /// </summary>
    private readonly BreakAction LB30b()
    {
        if (this.current.Is(LineBreakClass.EmojiBase) && this.next.Is(LineBreakClass.EmojiModifier))
        {
            return BreakAction.NoBreak;
        }

        if (this.next.Is(LineBreakClass.EmojiModifier)
            && this.current.Category == UnicodeCategory.OtherNotAssigned
            && CodePoint.GetGraphemeClusterClass(this.current.CodePoint) == GraphemeClusterClass.ExtendedPictographic)
        {
            return BreakAction.NoBreak;
        }

        return BreakAction.Pass;
    }

    /// <summary>
    /// Scans forward from <paramref name="charIndex"/> over spaces and checks the first non-space class.
    /// </summary>
    /// <remarks>
    /// LB16 and LB17 both have "with intervening spaces" forms. This helper performs that lookahead
    /// without advancing the streaming enumerator state.
    /// </remarks>
    private readonly bool ClassAfterSpacesIs(int charIndex, LineBreakClass cls)
    {
        int scanChar = charIndex;
        int scanLength = this.current.Length;

        while (this.TryReadForward(scanChar, scanLength, out LineBreakCodePoint codePoint))
        {
            if (!codePoint.Is(LineBreakClass.Space))
            {
                return codePoint.Is(cls);
            }

            scanChar = codePoint.CharEnd;
            scanLength = codePoint.Length;
        }

        return false;
    }

    /// <summary>
    /// Reads a code point after <see cref="next"/> without advancing the enumerator.
    /// </summary>
    /// <param name="codePoint">The decoded lookahead code point.</param>
    /// <param name="offset">The number of code points after <see cref="next"/> to inspect.</param>
    /// <returns><see langword="true"/> when the requested lookahead exists.</returns>
    private readonly bool TryGetAfterNext(out LineBreakCodePoint codePoint, int offset = 1)
    {
        codePoint = default;
        int scanChar = this.next.CharEnd;
        int scanLength = this.next.Length;

        for (int i = 0; i < offset; i++)
        {
            if (!this.TryReadForward(scanChar, scanLength, out codePoint))
            {
                return false;
            }

            scanChar = codePoint.CharEnd;
            scanLength = codePoint.Length;
        }

        return true;
    }

    /// <summary>
    /// Decodes a code point at <paramref name="charIndex"/> without advancing the enumerator.
    /// </summary>
    /// <param name="charIndex">The UTF-16 index to decode from.</param>
    /// <param name="length">The code point length to assign to the decoded lookahead item.</param>
    /// <param name="codePoint">The decoded lookahead code point.</param>
    /// <returns><see langword="true"/> when a code point was available.</returns>
    private readonly bool TryReadForward(int charIndex, int length, out LineBreakCodePoint codePoint)
    {
        if (!this.next.IsSentinel && charIndex == this.next.CharStart)
        {
            codePoint = this.next;
            return true;
        }

        if (charIndex >= this.source.Length)
        {
            codePoint = default;
            return false;
        }

        CodePoint cp = CodePoint.DecodeFromUtf16At(this.source, charIndex, out int charsConsumed);
        UnicodeCategory category = CodePoint.GetGeneralCategory(cp);
        LineBreakClass cls = MapClass(CodePoint.GetLineBreakClass(cp), category);

        codePoint = new LineBreakCodePoint(
            cp,
            cls,
            category,
            length + 1,
            charIndex,
            charIndex + charsConsumed);
        return true;
    }

    /// <summary>
    /// Decodes the code point ending at <paramref name="charEnd"/> without moving the stream.
    /// </summary>
    /// <remarks>
    /// Most callers hit the already-buffered <see cref="previous"/>, <see cref="current"/>, or
    /// <see cref="next"/> entries. Decoding from source is the fallback for longer LB25 or trimming
    /// scans and still does not allocate.
    /// </remarks>
    /// <param name="charEnd">The UTF-16 index immediately after the code point to read.</param>
    /// <param name="codePoint">The decoded lookbehind code point.</param>
    /// <returns><see langword="true"/> when a code point was available.</returns>
    private readonly bool TryReadBackward(int charEnd, out LineBreakCodePoint codePoint)
    {
        if (!this.current.IsSentinel && charEnd == this.current.CharEnd)
        {
            codePoint = this.current;
            return true;
        }

        if (!this.previous.IsSentinel && charEnd == this.previous.CharEnd)
        {
            codePoint = this.previous;
            return true;
        }

        if (!this.next.IsSentinel && charEnd == this.next.CharEnd)
        {
            codePoint = this.next;
            return true;
        }

        if (charEnd <= 0)
        {
            codePoint = default;
            return false;
        }

        int charStart = charEnd - 1;
        if (charStart > 0
            && char.IsLowSurrogate(this.source[charStart])
            && char.IsHighSurrogate(this.source[charStart - 1]))
        {
            charStart--;
        }

        CodePoint cp = CodePoint.DecodeFromUtf16At(this.source, charStart, out int _);
        UnicodeCategory category = CodePoint.GetGeneralCategory(cp);
        LineBreakClass cls = MapClass(CodePoint.GetLineBreakClass(cp), category);

        codePoint = new LineBreakCodePoint(cp, cls, category, 0, charStart, charEnd);
        return true;
    }

    /// <summary>
    /// Walks backward from a wrap position to the nearest non-breaking trailing content so that
    /// measurement excludes trailing spaces and hard line terminators while wrapping still occurs
    /// at the original boundary.
    /// </summary>
    private readonly int FindPriorNonWhitespace(LineBreakCodePoint from)
    {
        int measure = from.Length;
        int charEnd = from.CharEnd;

        if (this.TryReadBackward(charEnd, out LineBreakCodePoint codePoint)
            && (codePoint.Is(LineBreakClass.MandatoryBreak) || codePoint.Is(LineBreakClass.LineFeed) || codePoint.Is(LineBreakClass.CarriageReturn)))
        {
            measure--;
            charEnd = codePoint.CharStart;
        }

        while (this.TryReadBackward(charEnd, out codePoint))
        {
            if (codePoint.Is(LineBreakClass.Space))
            {
                measure--;
                charEnd = codePoint.CharStart;
            }
            else
            {
                break;
            }
        }

        return measure;
    }

    /// <summary>
    /// Checks the class exclusions used by LB9 before combining marks are folded into their base.
    /// </summary>
    private static bool IsBkCrLfNlSpZw(LineBreakCodePoint codePoint)
        => codePoint.Is(LineBreakClass.MandatoryBreak)
        || codePoint.Is(LineBreakClass.CarriageReturn)
        || codePoint.Is(LineBreakClass.LineFeed)
        || codePoint.Is(LineBreakClass.NextLine)
        || codePoint.Is(LineBreakClass.Space)
        || codePoint.Is(LineBreakClass.ZeroWidthSpace);

    /// <summary>
    /// Checks the start-like contexts that allow LB15a initial quotation handling.
    /// </summary>
    private static bool IsSotBkCrLfNlOpQuGlSpZw(LineBreakCodePoint codePoint)
        => codePoint.IsStartOfText
        || codePoint.Is(LineBreakClass.MandatoryBreak)
        || codePoint.Is(LineBreakClass.CarriageReturn)
        || codePoint.Is(LineBreakClass.LineFeed)
        || codePoint.Is(LineBreakClass.NextLine)
        || codePoint.Is(LineBreakClass.OpenPunctuation)
        || codePoint.Is(LineBreakClass.Quotation)
        || codePoint.Is(LineBreakClass.Glue)
        || codePoint.Is(LineBreakClass.Space)
        || codePoint.Is(LineBreakClass.ZeroWidthSpace);

    /// <summary>
    /// Checks the classes that may follow a final quotation mark for LB15b.
    /// </summary>
    private static bool IsSpGlWjClQuCpExIsSyBkCrLfNlZw(LineBreakCodePoint codePoint)
        => codePoint.Is(LineBreakClass.Space)
        || codePoint.Is(LineBreakClass.Glue)
        || codePoint.Is(LineBreakClass.WordJoiner)
        || codePoint.Is(LineBreakClass.ClosePunctuation)
        || codePoint.Is(LineBreakClass.Quotation)
        || codePoint.Is(LineBreakClass.CloseParenthesis)
        || codePoint.Is(LineBreakClass.Exclamation)
        || codePoint.Is(LineBreakClass.InfixNumeric)
        || codePoint.Is(LineBreakClass.BreakSymbols)
        || codePoint.Is(LineBreakClass.MandatoryBreak)
        || codePoint.Is(LineBreakClass.CarriageReturn)
        || codePoint.Is(LineBreakClass.LineFeed)
        || codePoint.Is(LineBreakClass.NextLine)
        || codePoint.Is(LineBreakClass.ZeroWidthSpace);

    /// <summary>
    /// Checks the leading contexts used by LB20a for hyphenated words.
    /// </summary>
    private static bool IsSotBkCrLfNlSpZwCbGl(LineBreakCodePoint codePoint)
        => codePoint.IsStartOfText
        || codePoint.Is(LineBreakClass.MandatoryBreak)
        || codePoint.Is(LineBreakClass.CarriageReturn)
        || codePoint.Is(LineBreakClass.LineFeed)
        || codePoint.Is(LineBreakClass.NextLine)
        || codePoint.Is(LineBreakClass.Space)
        || codePoint.Is(LineBreakClass.ZeroWidthSpace)
        || codePoint.Is(LineBreakClass.ContingentBreak)
        || codePoint.Is(LineBreakClass.Glue);

    /// <summary>
    /// Checks the ideographic and emoji classes that participate in LB23a and LB27.
    /// </summary>
    private static bool IsIdEbEm(LineBreakCodePoint codePoint)
        => codePoint.Is(LineBreakClass.Ideographic)
        || codePoint.Is(LineBreakClass.EmojiBase)
        || codePoint.Is(LineBreakClass.EmojiModifier);

    /// <summary>
    /// Checks the Hangul classes allowed after a leading jamo for LB26.
    /// </summary>
    private static bool IsJlJvH2H3(LineBreakCodePoint codePoint)
        => codePoint.Is(LineBreakClass.HangulLeadJamo)
        || codePoint.Is(LineBreakClass.HangulVowelJamo)
        || codePoint.Is(LineBreakClass.HangulLeadVowelSyllable)
        || codePoint.Is(LineBreakClass.HangulLeadVowelTailSyllable);

    /// <summary>
    /// Checks the Hangul syllable-block classes used by LB27.
    /// </summary>
    private static bool IsJlJvJtH2H3(LineBreakCodePoint codePoint)
        => codePoint.Is(LineBreakClass.HangulLeadJamo)
        || codePoint.Is(LineBreakClass.HangulVowelJamo)
        || codePoint.Is(LineBreakClass.HangulTailJamo)
        || codePoint.Is(LineBreakClass.HangulLeadVowelSyllable)
        || codePoint.Is(LineBreakClass.HangulLeadVowelTailSyllable);

    /// <summary>
    /// Checks whether a code point is an aksara base for LB28a.
    /// </summary>
    private static bool IsAksaraBase(LineBreakCodePoint codePoint)
        => codePoint.Is(LineBreakClass.Aksara)
        || codePoint.Is(LineBreakClass.AksaraStart)
        || codePoint.CodePoint.Value == DottedCircle;

    /// <summary>
    /// Checks whether a code point has East Asian width for LB19a and LB30 punctuation behavior.
    /// </summary>
    private static bool IsEastAsian(LineBreakCodePoint codePoint)
    {
        if (codePoint.IsSentinel)
        {
            return false;
        }

        EastAsianWidthClass width = CodePoint.GetEastAsianWidthClass(codePoint.CodePoint);
        return width is EastAsianWidthClass.Fullwidth or EastAsianWidthClass.Halfwidth or EastAsianWidthClass.Wide;
    }

    /// <summary>
    /// Determines where a plain-text run should stop while looking for URL markers.
    /// </summary>
    private static bool IsUrlRunBoundary(CodePoint codePoint)
    {
        if (CodePoint.IsWhiteSpace(codePoint))
        {
            return true;
        }

        return codePoint.Value is QuotationMark or Apostrophe or LessThanSign or GreaterThanSign;
    }

    /// <summary>
    /// Determines whether <paramref name="codePoint"/> is valid after the first URI scheme character.
    /// </summary>
    private static bool IsUrlSchemeCharacter(CodePoint codePoint)
        => (codePoint.IsAscii && CodePoint.IsLetterOrDigit(codePoint))
        || codePoint.Value is PlusSign or HyphenMinus or FullStop;

    /// <summary>
    /// Determines whether <paramref name="codePoint"/> is valid as the first URI scheme character.
    /// </summary>
    private static bool IsUrlSchemeStartCharacter(CodePoint codePoint)
        => codePoint.IsAscii && CodePoint.IsLetter(codePoint);

    /// <summary>
    /// Determines whether <paramref name="codePoint"/> may be part of the host prefix check.
    /// </summary>
    private static bool IsUrlHostCharacter(CodePoint codePoint)
        => (codePoint.IsAscii && CodePoint.IsLetterOrDigit(codePoint))
        || codePoint.Value is HyphenMinus or FullStop;

    /// <summary>
    /// Determines whether <paramref name="codePoint"/> is ASCII <c>W</c> or <c>w</c>.
    /// </summary>
    private static bool IsAsciiW(CodePoint codePoint)
        => codePoint.Value is UppercaseW or LowercaseW;

    /// <summary>
    /// Streaming recognizer for the URL-shaped tokens needed by UAX #14 section 8 tailoring.
    /// </summary>
    /// <remarks>
    /// This is deliberately not a URI parser. It recognizes two common plain-text signals while the
    /// main line-break stream is already decoding the source: a valid ASCII URI scheme followed by
    /// <c>://</c>, or a <c>www.</c> prefix at a host-label boundary. Once a run is URL-like, later
    /// solidus boundaries in that run can use the tailored behavior without rescanning the text.
    /// </remarks>
    private struct UrlTailoringState
    {
        /// <summary>
        /// Length of the current ASCII URI-scheme candidate, or zero when no scheme is active.
        /// </summary>
        private int schemeLength;

        /// <summary>
        /// Number of consecutive ASCII <c>w</c> or <c>W</c> characters in a possible <c>www.</c> prefix.
        /// </summary>
        private int wwwPrefixLength;

        /// <summary>
        /// Indicates that the current non-boundary run has already matched a URL signal.
        /// </summary>
        private bool isUrlLikeRun;

        /// <summary>
        /// Indicates that the previous code point was <c>:</c> ending a valid scheme candidate.
        /// </summary>
        private bool previousWasColonAfterValidScheme;

        /// <summary>
        /// Indicates that the previous code point was the first slash in a <c>://</c> marker.
        /// </summary>
        private bool previousWasFirstSchemeSlash;

        /// <summary>
        /// Indicates that the previous code point could be part of an ASCII host label.
        /// </summary>
        private bool previousWasHostCharacter;

        /// <summary>
        /// Blocks scheme recognition until a non-scheme character resets the candidate.
        /// </summary>
        /// <remarks>
        /// URI schemes must start with an ASCII letter. A run such as <c>1http:</c> should not
        /// become valid just because later characters are allowed inside a scheme.
        /// </remarks>
        private bool schemeBlocked;

        /// <summary>
        /// Updates the recognizer with the next decoded code point.
        /// </summary>
        /// <param name="codePoint">The code point from the main line-break stream.</param>
        /// <returns><see langword="true"/> when this code point belongs to a URL-like run.</returns>
        public bool Update(CodePoint codePoint)
        {
            if (IsUrlRunBoundary(codePoint))
            {
                this = default;
                return false;
            }

            bool currentIsUrlLike = this.isUrlLikeRun;
            bool currentWasColonAfterValidScheme = false;
            bool currentWasFirstSchemeSlash = false;

            if (codePoint.Value == Solidus)
            {
                if (this.previousWasFirstSchemeSlash)
                {
                    this.isUrlLikeRun = true;
                    currentIsUrlLike = true;
                }

                currentWasFirstSchemeSlash = this.previousWasColonAfterValidScheme;
                this.schemeLength = 0;
                this.schemeBlocked = false;
                this.wwwPrefixLength = 0;
            }
            else
            {
                this.UpdateSchemeState(codePoint, out currentWasColonAfterValidScheme);
                this.UpdateWwwPrefixState(codePoint, ref currentIsUrlLike);
            }

            this.previousWasColonAfterValidScheme = currentWasColonAfterValidScheme;
            this.previousWasFirstSchemeSlash = currentWasFirstSchemeSlash;
            this.previousWasHostCharacter = IsUrlHostCharacter(codePoint);

            return currentIsUrlLike;
        }

        /// <summary>
        /// Updates the ASCII URI-scheme candidate state.
        /// </summary>
        /// <param name="codePoint">The code point from the main line-break stream.</param>
        /// <param name="currentWasColonAfterValidScheme">
        /// Set to <see langword="true"/> when <paramref name="codePoint"/> is the colon after
        /// a valid URI scheme candidate.
        /// </param>
        private void UpdateSchemeState(CodePoint codePoint, out bool currentWasColonAfterValidScheme)
        {
            currentWasColonAfterValidScheme = false;

            if (codePoint.Value == Colon)
            {
                currentWasColonAfterValidScheme = this.schemeLength > 0;
                this.schemeLength = 0;
                this.schemeBlocked = false;
                return;
            }

            if (IsUrlSchemeCharacter(codePoint))
            {
                if (this.schemeLength > 0)
                {
                    this.schemeLength++;
                }
                else if (!this.schemeBlocked && IsUrlSchemeStartCharacter(codePoint))
                {
                    this.schemeLength = 1;
                }
                else
                {
                    this.schemeBlocked = true;
                }

                return;
            }

            this.schemeLength = 0;
            this.schemeBlocked = false;
        }

        /// <summary>
        /// Updates the <c>www.</c> prefix recognizer.
        /// </summary>
        /// <param name="codePoint">The code point from the main line-break stream.</param>
        /// <param name="currentIsUrlLike">
        /// The URL-like status to return for the current code point, updated when the prefix completes.
        /// </param>
        private void UpdateWwwPrefixState(CodePoint codePoint, ref bool currentIsUrlLike)
        {
            if (this.isUrlLikeRun)
            {
                currentIsUrlLike = true;
                return;
            }

            if (this.wwwPrefixLength == 3 && codePoint.Value == FullStop)
            {
                this.isUrlLikeRun = true;
                currentIsUrlLike = true;
                this.wwwPrefixLength = 0;
                return;
            }

            if (!IsAsciiW(codePoint))
            {
                this.wwwPrefixLength = 0;
                return;
            }

            if (!this.previousWasHostCharacter)
            {
                this.wwwPrefixLength = 1;
            }
            else if (this.wwwPrefixLength is 1 or 2)
            {
                this.wwwPrefixLength++;
            }
            else
            {
                this.wwwPrefixLength = 0;
            }
        }
    }

    /// <summary>
    /// The decoded code point plus the UAX #14 state needed to evaluate a boundary.
    /// </summary>
    /// <remarks>
    /// The struct stores both code point and UTF-16 positions so the enumerator can stream over the
    /// original span, trim trailing whitespace for measurement, and perform bounded lookahead/lookbehind
    /// without allocating intermediate collections.
    /// </remarks>
    private struct LineBreakCodePoint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LineBreakCodePoint"/> struct for a real code point.
        /// </summary>
        /// <param name="codePoint">The decoded Unicode scalar or replacement character.</param>
        /// <param name="cls">The LB1-resolved line break class.</param>
        /// <param name="category">The general category for quote and emoji-specific rules.</param>
        /// <param name="length">The one-based code point index immediately after this item.</param>
        /// <param name="charStart">The UTF-16 index where this code point starts.</param>
        /// <param name="charEnd">The UTF-16 index immediately after this code point.</param>
        /// <param name="isUrlLikeRun">Whether this item belongs to a URL-like run for layout tailoring.</param>
        public LineBreakCodePoint(
            CodePoint codePoint,
            LineBreakClass cls,
            UnicodeCategory category,
            int length,
            int charStart,
            int charEnd,
            bool isUrlLikeRun = false)
        {
            this.CodePoint = codePoint;
            this.Class = cls;
            this.Category = category;
            this.Length = length;
            this.CharStart = charStart;
            this.CharEnd = charEnd;
            this.SentinelValue = 0;
            this.Ignored = false;
            this.IsUrlLikeRun = isUrlLikeRun;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineBreakCodePoint"/> struct as a sentinel
        /// representing start or end of text.
        /// </summary>
        /// <param name="sentinel">The sentinel value.</param>
        /// <param name="length">The code point length associated with the sentinel boundary.</param>
        /// <param name="charEnd">The UTF-16 boundary associated with the sentinel.</param>
        private LineBreakCodePoint(int sentinel, int length, int charEnd)
        {
            this.CodePoint = default;
            this.Class = default;
            this.Category = default;
            this.Length = length;
            this.CharStart = charEnd;
            this.CharEnd = charEnd;
            this.SentinelValue = sentinel;
            this.Ignored = false;
            this.IsUrlLikeRun = false;
        }

        /// <summary>
        /// Gets the decoded code point.
        /// </summary>
        public CodePoint CodePoint { get; }

        /// <summary>
        /// Gets or sets the LB1-resolved line break class.
        /// </summary>
        /// <remarks>
        /// LB10 can rewrite a remaining CM or ZWJ to AL after LB9 has handled attached marks.
        /// </remarks>
        public LineBreakClass Class { get; set; }

        /// <summary>
        /// Gets the Unicode general category for quote and emoji-context checks.
        /// </summary>
        public UnicodeCategory Category { get; }

        /// <summary>
        /// Gets or sets the one-based code point index immediately after this item.
        /// </summary>
        /// <remarks>
        /// LB9 ignored marks extend the current item, so <see cref="Push"/> updates this value when
        /// a mark is folded into its base.
        /// </remarks>
        public int Length { get; set; }

        /// <summary>
        /// Gets the UTF-16 index where this item starts.
        /// </summary>
        public int CharStart { get; }

        /// <summary>
        /// Gets or sets the UTF-16 index immediately after this item.
        /// </summary>
        /// <remarks>
        /// LB9 ignored marks extend the current item, so <see cref="Push"/> updates this value when
        /// a mark is folded into its base.
        /// </remarks>
        public int CharEnd { get; set; }

        /// <summary>
        /// Gets the sentinel value, or zero for a real code point.
        /// </summary>
        public int SentinelValue { get; }

        /// <summary>
        /// Gets or sets a value indicating whether LB9 folded this item into the previous base.
        /// </summary>
        public bool Ignored { get; set; }

        /// <summary>
        /// Gets a value indicating whether this item belongs to a URL-like run for layout tailoring.
        /// </summary>
        public bool IsUrlLikeRun { get; }

        /// <summary>
        /// Gets a value indicating whether this item is a start or end sentinel.
        /// </summary>
        public readonly bool IsSentinel => this.SentinelValue != 0;

        /// <summary>
        /// Gets a value indicating whether this item is the start-of-text sentinel.
        /// </summary>
        public readonly bool IsStartOfText => this.SentinelValue == StartOfText;

        /// <summary>
        /// Gets a value indicating whether this item is the end-of-text sentinel.
        /// </summary>
        public readonly bool IsEndOfText => this.SentinelValue == EndOfText;

        /// <summary>
        /// Creates a start-of-text or end-of-text sentinel.
        /// </summary>
        /// <param name="sentinel">The sentinel value to assign.</param>
        /// <param name="length">The code point boundary associated with the sentinel.</param>
        /// <param name="charEnd">The UTF-16 boundary associated with the sentinel.</param>
        /// <returns>The sentinel item.</returns>
        public static LineBreakCodePoint CreateSentinel(int sentinel, int length, int charEnd) => new(sentinel, length, charEnd);

        /// <summary>
        /// Checks whether this item has the given line break class.
        /// </summary>
        /// <param name="cls">The class to compare.</param>
        /// <returns><see langword="true"/> when this is a real item with the requested class.</returns>
        public readonly bool Is(LineBreakClass cls) => !this.IsSentinel && this.Class == cls;

        /// <summary>
        /// Checks whether this item has the given scalar value.
        /// </summary>
        /// <param name="value">The scalar value to compare.</param>
        /// <returns><see langword="true"/> when this is a real item with the requested value.</returns>
        public readonly bool HasValue(int value) => !this.IsSentinel && this.CodePoint.Value == value;
    }
}
