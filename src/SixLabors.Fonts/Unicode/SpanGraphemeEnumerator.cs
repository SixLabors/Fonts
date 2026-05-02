// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// An enumerator for retrieving Grapheme instances from a <see cref="ReadOnlySpan{Char}"/>.
/// <br/>
/// Implements the Unicode Grapheme Cluster Algorithm. UAX:29
/// <see href="https://www.unicode.org/reports/tr29/"/>
/// <br/>
/// Supports the UAX #29 extended grapheme cluster rule for Indic conjunct sequences
/// (GB9c) using the <see cref="IndicConjunctBreakClass"/> property.
/// <br/>
/// Methods are pattern-matched by compiler to allow using foreach pattern.
/// </summary>
public ref struct SpanGraphemeEnumerator
{
    private ReadOnlySpan<char> source;
    private readonly TerminalWidthOptions terminalWidthOptions;
    private int sourceOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpanGraphemeEnumerator"/> struct.
    /// </summary>
    /// <param name="source">The buffer to read from.</param>
    public SpanGraphemeEnumerator(ReadOnlySpan<char> source)
        : this(source, TerminalWidthOptions.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpanGraphemeEnumerator"/> struct.
    /// </summary>
    /// <param name="source">The buffer to read from.</param>
    /// <param name="terminalWidthOptions">The terminal width options to apply while enumerating.</param>
    public SpanGraphemeEnumerator(ReadOnlySpan<char> source, TerminalWidthOptions terminalWidthOptions)
    {
        this.source = source;
        this.terminalWidthOptions = terminalWidthOptions;
        this.sourceOffset = 0;
        this.Current = default;
    }

    /// <summary>
    /// Gets the element in the collection at the current position of the enumerator.
    /// </summary>
    public GraphemeCluster Current { get; private set; }

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
        // GB9c is a stateful rule: whether the next consonant can join depends on
        // the InCB classes already consumed into the current cluster. Keep that state
        // outside Processor so Processor remains a simple UTF-16/code-point reader.
        IndicConjunctState indicConjunctState = default;
        TerminalWidthState terminalWidthState = new(this.terminalWidthOptions);
        int utf16Offset = this.sourceOffset;

        // Accept the current scalar into the cluster and advance to the next scalar.
        // IMPORTANT: Processor.Current* represents the next scalar not yet included in CharsConsumed.
        void ConsumeCurrentAndAdvance(ref Processor p)
        {
            indicConjunctState.Consume(p.CurrentCodePoint);
            terminalWidthState.Consume(p.CurrentCodePoint, p.CurrentType);
            p.MoveNext();
        }

        // Drain trailers per GB9/GB9a, plus GB9c-style Indic conjunct tailoring.
        // GB9 and GB9a always keep Extend, ZWJ, and SpacingMark with the preceding
        // cluster. GB9c additionally keeps an Indic consonant with the same cluster
        // when the cluster so far matches:
        // InCB=Consonant [InCB=Extend InCB=Linker]* InCB=Linker [InCB=Extend InCB=Linker]*
        // x InCB=Consonant
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

                // Rule GB9c only fires when the already-consumed cluster has seen
                // a consonant and a following linker. Extend values preserve that
                // state, so they are consumed above before this check runs.
                if (indicConjunctState.CanLinkConsonant
                    && CodePoint.GetIndicConjunctBreakClass(p.CurrentCodePoint) == IndicConjunctBreakClass.Consonant)
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

        terminalWidthState.Complete();
        ReadOnlySpan<char> grapheme = this.source[..processor.CharsConsumed];
        this.Current = new GraphemeCluster(
            grapheme,
            utf16Offset,
            terminalWidthState.CodePointCount,
            terminalWidthState.TerminalCellWidth,
            terminalWidthState.Flags,
            terminalWidthState.FirstCodePoint);

        this.source = this.source[processor.CharsConsumed..];
        this.sourceOffset += processor.CharsConsumed;

        return true; // rules GB2, GB999
    }

    /// <summary>
    /// Tracks terminal width metadata for the grapheme cluster currently being enumerated.
    /// </summary>
    /// <remarks>
    /// This state is updated as each scalar is accepted into the current UAX #29 cluster, so width,
    /// flags, and scalar counts are produced without slicing and re-reading the completed cluster.
    /// </remarks>
    private struct TerminalWidthState
    {
        private readonly TerminalWidthOptions options;

        /// <summary>
        /// Stores the maximum advancing scalar width before cluster-level overrides are applied.
        /// </summary>
        private int terminalCellWidth;

        /// <summary>
        /// Indicates that control policy must determine the final cluster width.
        /// </summary>
        private bool containsControl;

        /// <summary>
        /// Indicates that the cluster contains emoji-related data, even when that data is zero-width.
        /// </summary>
        private bool containsEmoji;

        /// <summary>
        /// Indicates that terminal practice should treat this emoji-shaped cluster as two cells.
        /// </summary>
        private bool containsEmojiWideOverride;

        /// <summary>
        /// Stores the first scalar's emoji properties for sequence checks that complete later in the cluster.
        /// </summary>
        private EmojiProperties firstEmojiProperties;

        /// <summary>
        /// Stores the previous scalar's emoji properties so variation selectors can validate their base.
        /// </summary>
        private EmojiProperties previousEmojiProperties;

        /// <summary>
        /// Indicates that the cluster contains a valid U+FE0F emoji presentation selector.
        /// </summary>
        private bool containsEmojiPresentationSelector;

        /// <summary>
        /// Indicates that the cluster contains a valid U+FE0E text presentation selector.
        /// </summary>
        private bool containsTextPresentationSelector;

        /// <summary>
        /// Initializes a new instance of the <see cref="TerminalWidthState"/> struct.
        /// </summary>
        /// <param name="options">The terminal width options to apply to the current cluster.</param>
        public TerminalWidthState(TerminalWidthOptions options)
        {
            this.options = options;
            this.terminalCellWidth = 0;
            this.containsControl = false;
            this.containsEmoji = false;
            this.containsEmojiWideOverride = false;
            this.firstEmojiProperties = EmojiProperties.None;
            this.previousEmojiProperties = EmojiProperties.None;
            this.containsEmojiPresentationSelector = false;
            this.containsTextPresentationSelector = false;
            this.CodePointCount = 0;
            this.FirstCodePoint = CodePoint.ReplacementChar;
            this.Flags = GraphemeClusterFlags.AllZeroWidth;
        }

        /// <summary>
        /// Gets the number of scalar values consumed into the current cluster.
        /// </summary>
        public int CodePointCount { get; private set; }

        /// <summary>
        /// Gets the first scalar value consumed into the current cluster.
        /// </summary>
        public CodePoint FirstCodePoint { get; private set; }

        /// <summary>
        /// Gets the flags derived from the scalars consumed into the current cluster.
        /// </summary>
        public GraphemeClusterFlags Flags { get; private set; }

        /// <summary>
        /// Gets the policy-resolved terminal cell width of the current cluster.
        /// </summary>
        public readonly int TerminalCellWidth
        {
            get
            {
                if (this.containsControl)
                {
                    return this.options.ControlCharacterWidth switch
                    {
                        TerminalControlCharacterWidth.Zero => 0,
                        TerminalControlCharacterWidth.Narrow => 1,
                        _ => -1,
                    };
                }

                if (this.containsEmojiWideOverride
                    && !this.containsTextPresentationSelector
                    && this.options.EmojiWidth == TerminalEmojiWidth.Wide)
                {
                    return 2;
                }

                return this.terminalCellWidth;
            }
        }

        /// <summary>
        /// Adds a scalar value to the current cluster metadata.
        /// </summary>
        /// <param name="codePoint">The scalar value accepted into the current cluster.</param>
        /// <param name="graphemeClusterClass">The grapheme break class for <paramref name="codePoint"/>.</param>
        public void Consume(in CodePoint codePoint, GraphemeClusterClass graphemeClusterClass)
        {
            EmojiProperties emojiProperties = CodePoint.GetEmojiProperties(codePoint);
            if (this.CodePointCount == 0)
            {
                this.FirstCodePoint = codePoint;
                this.firstEmojiProperties = emojiProperties;
            }

            this.CodePointCount++;

            if (codePoint.Value == 0)
            {
                return;
            }

            if (CodePoint.IsControl(codePoint))
            {
                this.containsControl = true;
                this.Flags = (this.Flags & ~GraphemeClusterFlags.AllZeroWidth) | GraphemeClusterFlags.ContainsControl;
                return;
            }

            if (CodePoint.IsVariationSelector(codePoint))
            {
                this.Flags |= GraphemeClusterFlags.ContainsVariationSelector;

                // U+FE0F VARIATION SELECTOR-16 requests emoji presentation.
                // Only honor it for bases listed by Unicode as emoji-presentation sequence bases.
                if (codePoint.Value == 0xFE0F
                    && (this.previousEmojiProperties & EmojiProperties.EmojiPresentationSequenceBase) != 0)
                {
                    this.containsEmoji = true;
                    this.containsEmojiPresentationSelector = true;
                    this.containsEmojiWideOverride = true;
                }

                // U+FE0E VARIATION SELECTOR-15 requests text presentation, which suppresses
                // the terminal emoji-wide override even when the base is emoji-capable.
                else if (codePoint.Value == 0xFE0E
                    && (this.previousEmojiProperties & EmojiProperties.TextPresentationSequenceBase) != 0)
                {
                    this.containsEmoji = true;
                    this.containsTextPresentationSelector = true;
                }

                this.previousEmojiProperties = emojiProperties;

                return;
            }

            if (graphemeClusterClass == GraphemeClusterClass.ZeroWidthJoiner)
            {
                this.Flags |= GraphemeClusterFlags.ContainsZwjSequence;
                if (this.containsEmoji)
                {
                    this.containsEmojiWideOverride = true;
                }

                this.previousEmojiProperties = emojiProperties;
                return;
            }

            if ((emojiProperties & EmojiProperties.EmojiModifier) != 0)
            {
                this.containsEmoji = true;
                this.previousEmojiProperties = emojiProperties;
                return;
            }

            if ((emojiProperties & EmojiProperties.Emoji) != 0)
            {
                this.containsEmoji = true;
                this.Flags |= GraphemeClusterFlags.ContainsEmoji;
            }

            if ((emojiProperties & EmojiProperties.EmojiPresentation) != 0 && !this.containsTextPresentationSelector)
            {
                this.containsEmojiWideOverride = true;
            }

            // U+20E3 COMBINING ENCLOSING KEYCAP completes keycap emoji sequences
            // such as "#\uFE0F\u20E3" when the cluster started from a valid keycap base.
            if (codePoint.Value == 0x20E3
                && this.containsEmojiPresentationSelector
                && (this.firstEmojiProperties & EmojiProperties.EmojiKeycapSequenceBase) != 0)
            {
                this.containsEmoji = true;
                this.containsEmojiWideOverride = true;
            }

            if (IsZeroWidthGraphemeExtension(graphemeClusterClass))
            {
                this.previousEmojiProperties = emojiProperties;
                return;
            }

            if (graphemeClusterClass == GraphemeClusterClass.ExtendedPictographic)
            {
                this.containsEmoji = true;
                this.Flags |= GraphemeClusterFlags.ContainsEmoji;
            }
            else if (graphemeClusterClass == GraphemeClusterClass.RegionalIndicator)
            {
                this.containsEmoji = true;
                this.containsEmojiWideOverride = true;
                this.Flags |= GraphemeClusterFlags.ContainsEmoji;
            }

            int scalarWidth = this.GetScalarWidth(codePoint);
            if (scalarWidth == 0)
            {
                return;
            }

            this.Flags &= ~GraphemeClusterFlags.AllZeroWidth;
            if (scalarWidth == 2)
            {
                this.Flags |= GraphemeClusterFlags.ContainsWide;
            }

            if (scalarWidth > this.terminalCellWidth)
            {
                this.terminalCellWidth = scalarWidth;
            }

            this.previousEmojiProperties = emojiProperties;
        }

        /// <summary>
        /// Finalizes metadata that depends on the completed cluster.
        /// </summary>
        public void Complete()
        {
            if (this.CodePointCount == 1)
            {
                this.Flags |= GraphemeClusterFlags.IsSingleCodePoint;
            }

            if (this.containsEmoji)
            {
                this.Flags |= GraphemeClusterFlags.ContainsEmoji;
            }
        }

        /// <summary>
        /// Gets the terminal cell width contribution for a non-zero-width scalar.
        /// </summary>
        /// <param name="codePoint">The scalar value to measure.</param>
        /// <returns>The scalar width after applying East Asian Width and ambiguous-width policy.</returns>
        private int GetScalarWidth(in CodePoint codePoint)
        {
            EastAsianWidthClass width = CodePoint.GetEastAsianWidthClass(codePoint);
            if (width == EastAsianWidthClass.Ambiguous)
            {
                this.Flags |= GraphemeClusterFlags.ContainsAmbiguous;
                return this.options.AmbiguousWidth == TerminalAmbiguousWidth.Wide ? 2 : 1;
            }

            return width is EastAsianWidthClass.Fullwidth or EastAsianWidthClass.Wide ? 2 : 1;
        }

        /// <summary>
        /// Returns a value indicating whether the grapheme break class contributes no terminal advance.
        /// </summary>
        /// <param name="graphemeClusterClass">The grapheme break class to inspect.</param>
        /// <returns><see langword="true"/> if the class is zero-width for terminal measurement.</returns>
        private static bool IsZeroWidthGraphemeExtension(GraphemeClusterClass graphemeClusterClass)
            => graphemeClusterClass is GraphemeClusterClass.Extend
            or GraphemeClusterClass.SpacingMark
            or GraphemeClusterClass.Prepend;
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

    /// <summary>
    /// Tracks the already-consumed part of the UAX #29 GB9c Indic conjunct rule.
    /// </summary>
    /// <remarks>
    /// GB9c prevents a grapheme break before an Indic consonant when the current cluster already
    /// contains an Indic consonant followed by at least one linker, with optional extend/linker
    /// code points in between. This state machine consumes the same code points as the main
    /// grapheme enumerator and remembers only the minimum information needed for that decision.
    /// </remarks>
    private struct IndicConjunctState
    {
        /// <summary>
        /// Indicates that the current cluster contains an <c>InCB=Consonant</c> starter.
        /// </summary>
        private bool hasConsonant;

        /// <summary>
        /// Indicates that a linker has been consumed after the current consonant starter.
        /// </summary>
        private bool hasLinker;

        /// <summary>
        /// Gets a value indicating whether GB9c should suppress a break before the next consonant.
        /// </summary>
        /// <remarks>
        /// This becomes true only after a consonant and a following linker have both been consumed.
        /// <c>InCB=Extend</c> values leave the state unchanged, so combining marks can appear
        /// between the linker and the next consonant.
        /// </remarks>
        public readonly bool CanLinkConsonant => this.hasConsonant && this.hasLinker;

        /// <summary>
        /// Updates the GB9c state with a code point that has just been consumed into the cluster.
        /// </summary>
        /// <param name="codePoint">The consumed code point.</param>
        public void Consume(in CodePoint codePoint)
        {
            switch (CodePoint.GetIndicConjunctBreakClass(codePoint))
            {
                case IndicConjunctBreakClass.Consonant:
                    // A consonant starts or restarts the GB9c candidate. It cannot link a
                    // following consonant until a linker has also been consumed.
                    this.hasConsonant = true;
                    this.hasLinker = false;
                    break;

                case IndicConjunctBreakClass.Linker:
                    // Linkers only matter after a consonant starter. Leading linkers cannot
                    // create a GB9c sequence by themselves.
                    if (this.hasConsonant)
                    {
                        this.hasLinker = true;
                    }

                    break;

                case IndicConjunctBreakClass.Extend:
                    // Extend values are transparent for GB9c and preserve the current candidate.
                    break;

                default:
                    // Any other class ends the candidate conjunct sequence.
                    this.hasConsonant = false;
                    this.hasLinker = false;
                    break;
            }
        }
    }
}
