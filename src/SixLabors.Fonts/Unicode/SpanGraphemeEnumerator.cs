// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// An enumerator for retrieving Grapheme instances from a <see cref="ReadOnlySpan{Char}"/>.
/// <br/>
/// Implements the Unicode Grapheme Cluster Algorithm. UAX:29
/// <see href="https://www.unicode.org/reports/tr29/tr29-37.html"/>
/// <br/>
/// Methods are pattern-matched by compiler to allow using foreach pattern.
/// </summary>
public ref struct SpanGraphemeEnumerator
{
    private ReadOnlySpan<char> source;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpanGraphemeEnumerator"/> struct.
    /// </summary>
    /// <param name="source">The buffer to read from.</param>
    public SpanGraphemeEnumerator(ReadOnlySpan<char> source)
    {
        this.source = source;
        this.Current = default;
    }

    /// <summary>
    /// Gets the element in the collection at the current position of the enumerator.
    /// </summary>
    public ReadOnlySpan<char> Current { get; private set; }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that iterates through the collection.</returns>
    public readonly SpanGraphemeEnumerator GetEnumerator() => this;

    /// <summary>
    /// Advances the enumerator to the next element of the collection.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the enumerator was successfully advanced to the next element;
    /// <see langword="false"/> if the enumerator has passed the end of the collection.
    /// </returns>
    public bool MoveNext()
    {
        // GB9c (Indic conjuncts) requires some script-specific state.
        // This implementation uses existing IndicSyllabicCategory data as a pragmatic approximation
        // for the InCB tailoring required by UAX#29. It is sufficient to prevent the common
        // "dead consonant" split for sequences like: RA   VIRAMA   KA   ... (eg: "\u0930\u094D\u0915\u093F").
        bool indicLinkerJustConsumed;

        static bool IsIndicLinker(in CodePoint cp)
        {
            // In practice, this is primarily VIRAMA (and in some scripts, "pure killer").
            // We intentionally keep this tight to avoid accidental over-aggregation.
            IndicSyllabicCategory isc = CodePoint.GetIndicSyllabicCategory(cp);
            return isc is IndicSyllabicCategory.Virama or IndicSyllabicCategory.PureKiller;
        }

        static bool IsIndicConsonant(in CodePoint cp)
        {
            IndicSyllabicCategory isc = CodePoint.GetIndicSyllabicCategory(cp);
            return isc is IndicSyllabicCategory.Consonant
                or IndicSyllabicCategory.ConsonantDead
                or IndicSyllabicCategory.ConsonantWithStacker
                or IndicSyllabicCategory.ConsonantSubjoined
                or IndicSyllabicCategory.ConsonantFinal
                or IndicSyllabicCategory.ConsonantMedial
                or IndicSyllabicCategory.ConsonantHeadLetter
                or IndicSyllabicCategory.ConsonantPlaceholder
                or IndicSyllabicCategory.ConsonantInitialPostfixed
                or IndicSyllabicCategory.ConsonantKiller
                or IndicSyllabicCategory.ConsonantPrefixed
                or IndicSyllabicCategory.ConsonantPrecedingRepha
                or IndicSyllabicCategory.ConsonantSucceedingRepha;
        }

        // Accept the current scalar into the cluster and advance to the next scalar.
        // IMPORTANT: Processor.Current* represents the next scalar not yet included in CharsConsumed.
        void ConsumeCurrentAndAdvance(ref Processor p)
        {
            // Update Indic state based on the scalar being consumed into the cluster.
            indicLinkerJustConsumed = IsIndicLinker(p.CurrentCodePoint);
            p.MoveNext();
        }

        // Drain trailers per GB9/GB9a, plus GB9c-style Indic conjunct tailoring:
        // If we just consumed a Linker (eg Virama) and the next scalar is an Indic consonant,
        // do not break, instead consume that consonant into the same grapheme cluster.
        void DrainTrailersAndIndicConjuncts(ref Processor p)
        {
            while (true)
            {
                // rules GB9, GB9a
                while (p.CurrentType is GraphemeClusterClass.Extend
                    or GraphemeClusterClass.ZeroWidthJoiner
                    or GraphemeClusterClass.SpacingMark)
                {
                    ConsumeCurrentAndAdvance(ref p);
                }

                // rule GB9c (tailoring): ... Linker x Consonant
                if (indicLinkerJustConsumed && IsIndicConsonant(p.CurrentCodePoint))
                {
                    ConsumeCurrentAndAdvance(ref p);
                    continue;
                }

                break;
            }
        }

        if (this.source.IsEmpty)
        {
            return false;
        }

        // Algorithm given at https://www.unicode.org/reports/tr29/#Grapheme_Cluster_Boundary_Rules.
        Processor processor = new(this.source);

        processor.MoveNext();

        // First, consume as many Prepend scalars as we can (rule GB9b).
        while (processor.CurrentType == GraphemeClusterClass.Prepend)
        {
            ConsumeCurrentAndAdvance(ref processor);
        }

        // Next, make sure we're not about to violate control character restrictions.
        // Essentially, if we saw Prepend data, we can't have Control | CR | LF data afterward (rule GB5).
        if (processor.CharsConsumed > 0)
        {
            if (processor.CurrentType is GraphemeClusterClass.Control
                or GraphemeClusterClass.CarriageReturn
                or GraphemeClusterClass.LineFeed)
            {
                goto Return;
            }
        }

        // Now begin the main state machine.
        GraphemeClusterClass previousClusterBreakType = processor.CurrentType;
        ConsumeCurrentAndAdvance(ref processor);

        switch (previousClusterBreakType)
        {
            case GraphemeClusterClass.CarriageReturn:
                if (processor.CurrentType != GraphemeClusterClass.LineFeed)
                {
                    goto Return; // rules GB3 & GB4 (only <LF> can follow <CR>)
                }

                ConsumeCurrentAndAdvance(ref processor);
                goto case GraphemeClusterClass.LineFeed;

            case GraphemeClusterClass.Control:
            case GraphemeClusterClass.LineFeed:
                goto Return; // rule GB4 (no data after Control | LF)

            case GraphemeClusterClass.HangulLead:
                if (processor.CurrentType == GraphemeClusterClass.HangulLead)
                {
                    ConsumeCurrentAndAdvance(ref processor); // rule GB6 (L x L)
                    goto case GraphemeClusterClass.HangulLead;
                }
                else if (processor.CurrentType == GraphemeClusterClass.HangulVowel)
                {
                    ConsumeCurrentAndAdvance(ref processor); // rule GB6 (L x V)
                    goto case GraphemeClusterClass.HangulVowel;
                }
                else if (processor.CurrentType == GraphemeClusterClass.HangulLeadVowel)
                {
                    ConsumeCurrentAndAdvance(ref processor); // rule GB6 (L x LV)
                    goto case GraphemeClusterClass.HangulLeadVowel;
                }
                else if (processor.CurrentType == GraphemeClusterClass.HangulLeadVowelTail)
                {
                    ConsumeCurrentAndAdvance(ref processor); // rule GB6 (L x LVT)
                    goto case GraphemeClusterClass.HangulLeadVowelTail;
                }
                else
                {
                    break;
                }

            case GraphemeClusterClass.HangulLeadVowel:
            case GraphemeClusterClass.HangulVowel:
                if (processor.CurrentType == GraphemeClusterClass.HangulVowel)
                {
                    ConsumeCurrentAndAdvance(ref processor); // rule GB7 (LV | V x V)
                    goto case GraphemeClusterClass.HangulVowel;
                }
                else if (processor.CurrentType == GraphemeClusterClass.HangulTail)
                {
                    ConsumeCurrentAndAdvance(ref processor); // rule GB7 (LV | V x T)
                    goto case GraphemeClusterClass.HangulTail;
                }
                else
                {
                    break;
                }

            case GraphemeClusterClass.HangulLeadVowelTail:
            case GraphemeClusterClass.HangulTail:
                if (processor.CurrentType == GraphemeClusterClass.HangulTail)
                {
                    ConsumeCurrentAndAdvance(ref processor); // rule GB8 (LVT | T x T)
                    goto case GraphemeClusterClass.HangulTail;
                }
                else
                {
                    break;
                }

            case GraphemeClusterClass.ExtendedPictographic:
                // Attempt processing extended pictographic (rules GB11, GB9).
                // First, drain any Extend scalars that might exist
                while (processor.CurrentType == GraphemeClusterClass.Extend)
                {
                    ConsumeCurrentAndAdvance(ref processor);
                }

                // Now see if there's a ZWJ + extended pictograph again.
                if (processor.CurrentType != GraphemeClusterClass.ZeroWidthJoiner)
                {
                    break;
                }

                ConsumeCurrentAndAdvance(ref processor);
                if (processor.CurrentType != GraphemeClusterClass.ExtendedPictographic)
                {
                    break;
                }

                ConsumeCurrentAndAdvance(ref processor);
                goto case GraphemeClusterClass.ExtendedPictographic;

            case GraphemeClusterClass.RegionalIndicator:
                // We've consumed a single RI scalar. Try to consume another (to make it a pair).
                if (processor.CurrentType == GraphemeClusterClass.RegionalIndicator)
                {
                    ConsumeCurrentAndAdvance(ref processor);
                }

                // Standalone RI scalars (or a single pair of RI scalars) can only be followed by trailers.
                break; // nothing but trailers after the final RI

            default:
                break;
        }

        DrainTrailersAndIndicConjuncts(ref processor);

        Return:

        this.Current = this.source[..processor.CharsConsumed];
        this.source = this.source[processor.CharsConsumed..];

        return true; // rules GB2, GB999
    }

    private ref struct Processor
    {
        private readonly ReadOnlySpan<char> source;
        private int charsConsumed;

        public Processor(ReadOnlySpan<char> source)
        {
            this.source = source;
            this.CurrentType = GraphemeClusterClass.Any;
            this.CurrentCodePoint = CodePoint.ReplacementChar;
            this.charsConsumed = 0;
            this.CharsConsumed = 0;
        }

        public GraphemeClusterClass CurrentType { get; private set; }

        public CodePoint CurrentCodePoint { get; private set; }

        public int CharsConsumed { get; private set; }

        public void MoveNext()
        {
            this.CharsConsumed += this.charsConsumed;
            CodePoint codePoint = CodePoint.DecodeFromUtf16At(this.source, this.CharsConsumed, out this.charsConsumed);
            this.CurrentCodePoint = codePoint;
            this.CurrentType = CodePoint.GetGraphemeClusterClass(codePoint);
        }
    }
}
