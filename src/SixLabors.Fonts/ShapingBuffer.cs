// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using System.Runtime.CompilerServices;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Stores live glyphs in one array. The buffer fills the metrics and position arrays
/// after substitution. A reset clears the live count and keeps the arrays for reuse.
/// </summary>
/// <remarks>
/// Use the indexer or a <see langword="ref"/> local to change a stored record.
/// A value local holds a copy. Get a new reference after an insertion or removal.
/// </remarks>
internal sealed class ShapingBuffer
{
    /// <summary>
    /// The single glyph array. A substitution pass can leave unused slots
    /// between two live ranges. All public glyph indices omit these slots.
    /// </summary>
    private GlyphShapingData[] data = new GlyphShapingData[64];

    /// <summary>
    /// The live index just after the unused slots, or <see cref="int.MaxValue"/>
    /// when the array is packed.
    /// </summary>
    private int gapIndex = int.MaxValue;

    /// <summary>
    /// The number of unused slots at <see cref="gapIndex"/>.
    /// </summary>
    private int gapLength;

    /// <summary>
    /// The resolved source runs and their cumulative input glyph counts.
    /// </summary>
    private ArrayBuilder<(int End, ScriptItemizer.ShapingRun Run)> inputRuns = new(4);

    /// <summary>
    /// The metrics by live glyph index. This array aligns with <see cref="data"/>
    /// after the substitution pass packs the glyph array.
    /// </summary>
    private GlyphMetricsEntry[] metrics = new GlyphMetricsEntry[64];

    /// <summary>
    /// The positioning data by live glyph index. The buffer fills this array
    /// with the metrics after substitution. This keeps the glyph records small.
    /// </summary>
    private GlyphShapingPosition[] positions = new GlyphShapingPosition[64];

    /// <summary>
    /// Shaper scratch storage handed out by <see cref="GetShaperScratch"/>, grown
    /// to the workload's high-water mark. Contents are undefined between passes.
    /// </summary>
    private byte[] shaperScratch = [];

    /// <summary>
    /// The approximate membership filter over every glyph id the buffer has ever
    /// contained. See <see cref="GlyphDigest"/> for the growth contract.
    /// </summary>
    private GlyphSetDigest glyphDigest;

    /// <summary>
    /// Validation tags for the direct-mapped glyph metrics cache. A slot's tag packs
    /// every key field of the font's own metrics cache above a marker bit, so a hit is
    /// one load and one compare. Zero marks a slot empty.
    /// </summary>
    private readonly ulong[] metricsCacheTags = new ulong[256];

    /// <summary>
    /// The resolved metrics for each slot of <see cref="metricsCacheTags"/>.
    /// </summary>
    private readonly FontGlyphMetrics?[] metricsCacheValues = new FontGlyphMetrics?[256];

    /// <summary>
    /// The font metrics instance the cache entries belong to. Seeding from a different
    /// font clears the cache before use.
    /// </summary>
    private FontMetrics? metricsCacheOwner;

    /// <summary>
    /// The palette selection the cache entries belong to. The palette is a reference the
    /// packed tag cannot encode, and a pooled buffer can be reset with new options while
    /// keeping the same font, so a selection change clears the cache before use.
    /// </summary>
    private FontPalette? metricsCachePalette;

    /// <summary>
    /// The bit offset of the encoded following codepoint in a glyph id cache entry.
    /// </summary>
    private const int GlyphIdCacheNextShift = 21;

    /// <summary>
    /// The bit offset of the resolved glyph id in a glyph id cache entry.
    /// </summary>
    private const int GlyphIdCacheGlyphShift = 43;

    /// <summary>
    /// The glyph id cache entry bit recording that the lookup found a glyph.
    /// </summary>
    private const ulong GlyphIdCacheFoundFlag = 1UL << 59;

    /// <summary>
    /// The glyph id cache entry bit recording that the following codepoint was
    /// consumed as part of a variation sequence.
    /// </summary>
    private const ulong GlyphIdCacheSkipFlag = 1UL << 60;

    /// <summary>
    /// The glyph id cache entry bit distinguishing a populated slot from an empty
    /// one, since a zero entry could otherwise read as a valid all-zero lookup.
    /// </summary>
    private const ulong GlyphIdCacheMarkerFlag = 1UL << 63;

    /// <summary>
    /// The fraction slash, U+2044, which forms fractions from the digit runs
    /// surrounding it. The solidus U+002F does not.
    /// </summary>
    private const uint FractionSlashCodePoint = 0x2044;

    /// <summary>
    /// The lowest character that can begin a constrained vowel sequence, which
    /// is the first Devanagari vowel letter. Text made only of characters below
    /// it cannot contain such a sequence.
    /// </summary>
    private const uint FirstVowelConstraintCharacter = 0x0905;

    /// <summary>
    /// The dotted circle, U+25CC, which stands in for a base a mark has no
    /// valid one to attach to.
    /// </summary>
    private const int DottedCircleCodePoint = 0x25CC;

    /// <summary>
    /// U+FE0F VARIATION SELECTOR-16, which requests emoji presentation for the preceding character.
    /// </summary>
    public const int EmojiPresentationSelector = 0xFE0F;

    /// <summary>
    /// U+FE0E VARIATION SELECTOR-15, which requests text presentation for the preceding character.
    /// </summary>
    public const int TextPresentationSelector = 0xFE0E;

    /// <summary>
    /// The multiplier used by the deterministic random sequence.
    /// </summary>
    private const uint RandomMultiplier = 48271;

    /// <summary>
    /// The modulus used by the deterministic random sequence.
    /// </summary>
    private const uint RandomModulus = 2147483647;

    /// <summary>
    /// The current deterministic random state.
    /// </summary>
    private uint randomState = 1;

    /// <summary>
    /// The glyph id cache entry bits forming the lookup key: the marker, the
    /// codepoint, and the encoded following codepoint.
    /// </summary>
    private const ulong GlyphIdCacheTagMask = GlyphIdCacheMarkerFlag | ((1UL << GlyphIdCacheGlyphShift) - 1);

    /// <summary>
    /// Direct-mapped codepoint-to-glyph cache: one word per slot packing the lookup
    /// key alongside the found flag, skip flag, and glyph id, so a repeat lookup is
    /// one load and one masked compare. Zero marks a slot empty.
    /// </summary>
    private readonly ulong[] glyphIdCacheEntries = new ulong[256];

    /// <summary>
    /// The font metrics instance the glyph id cache entries belong to. Populating
    /// from a different font clears the cache before use.
    /// </summary>
    private FontMetrics? glyphIdCacheOwner;

    /// <summary>
    /// The bit offset of the packed class props word in a shaping class cache entry.
    /// </summary>
    private const int ShapingClassCachePropsShift = 16;

    /// <summary>
    /// The shaping class cache entry bit distinguishing a populated slot from an
    /// empty one, since a zero entry could otherwise read as a valid all-zero lookup.
    /// </summary>
    private const ulong ShapingClassCacheMarkerFlag = 1UL << 63;

    /// <summary>
    /// The shaping class cache entry bits forming the lookup key: the marker and the
    /// glyph id.
    /// </summary>
    private const ulong ShapingClassCacheTagMask = ShapingClassCacheMarkerFlag | ((1UL << ShapingClassCachePropsShift) - 1);

    /// <summary>
    /// Direct-mapped glyph-id-to-class cache: one word per slot packing the glyph id
    /// key alongside the packed class props word, so a repeat classification is one
    /// load and one masked compare instead of a class definition table walk. Only
    /// table-derived classes enter the cache; the codepoint fallback classification
    /// depends on record state and stays out. Zero marks a slot empty.
    /// </summary>
    private readonly ulong[] shapingClassCacheEntries = new ulong[256];

    /// <summary>
    /// The font metrics instance the shaping class cache entries belong to.
    /// Populating from a different font clears the cache before use.
    /// </summary>
    private FontMetrics? shapingClassCacheOwner;

    /// <summary>
    /// The bidi runs recorded for inline placeholders, keyed by codepoint index.
    /// Placeholder state lives here rather than on every glyph record because only
    /// placeholders carry a bidi run of their own, and only the copy-out reads it.
    /// </summary>
    private readonly List<(int CodePointIndex, BidiRun Run)> placeholderBidiRuns = [];

    /// <summary>
    /// Shape plans reused across segments and passes, keyed by script, script tag,
    /// font, language, and effective feature list. Safe to reuse because the pooled
    /// buffer is exclusively owned and a plan's per-segment shaper state is
    /// reassigned at each pause invocation. Cleared when a reset changes an option
    /// value captured by the plans.
    /// </summary>
    private readonly List<(ScriptClass Script, Tag ScriptTag, FontMetrics FontMetrics, string Language, IReadOnlyList<Tag> FeatureTags, bool UsesMetamorphosis, ShapePlan Plan)> planCache = new(4);

    /// <summary>
    /// The language the cached language tags were resolved for.
    /// </summary>
    private string languageKey = string.Empty;

    /// <summary>
    /// The feature tags the cached plans were built for.
    /// </summary>
    private IReadOnlyList<Tag>? featureKey;

    /// <summary>
    /// The layout mode the cached plans were built for.
    /// </summary>
    private LayoutMode layoutModeKey;

    /// <summary>
    /// The kerning mode the cached plans were built for.
    /// </summary>
    private KerningMode kerningModeKey;

    /// <summary>
    /// Whether tracking was enabled when the cached plans were built.
    /// </summary>
    private bool hasTrackingKey;

    /// <summary>
    /// The depth of nested lookup application within contextual matches. Nested
    /// replacements mutate the input side in place regardless of their type.
    /// </summary>
    private int nestedApplicationDepth;

    private List<(int Index, int Owner, GlyphShapingData Glyph)>? deletedGlyphs;

    /// <summary>
    /// The number of glyphs after the active span. Edits within the span change
    /// its end and the list length by the same amount.
    /// </summary>
    private int spanTailCount;

    /// <summary>
    /// The lookup operations left for substitution across all spans.
    /// </summary>
    private int remainingSubstitutionOperations;

    /// <summary>
    /// The lookup operations left for positioning across all spans.
    /// </summary>
    private int remainingPositioningOperations;

    /// <summary>
    /// Stores matched glyph positions in a separate slice for each lookup depth.
    /// A nested lookup uses its own slice to preserve its parent's positions.
    /// </summary>
    private int[] contextMatchPositions = [];

    /// <summary>
    /// Whether the packed matcher flag bytes have been computed at least once;
    /// until then the stamped lookup state always rebuilds them.
    /// </summary>
    private bool packedFlagsValid;

    /// <summary>
    /// The role the packed matcher flag bytes were computed under; a role change
    /// invalidates them.
    /// </summary>
    private ShapingBufferRole packedFlagsRole;

    /// <summary>
    /// Whether the buffer held default ignorables when the packed matcher flag
    /// bytes were computed; ignorables appearing invalidates them.
    /// </summary>
    private bool packedFlagsHadIgnorables;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShapingBuffer"/> class.
    /// </summary>
    /// <param name="textOptions">The text options.</param>
    /// <param name="role">The shaping phase this buffer serves.</param>
    public ShapingBuffer(TextOptions textOptions, ShapingBufferRole role)
    {
        this.TextOptions = textOptions;
        this.Role = role;
    }

    /// <summary>
    /// Gets the shaping phase this buffer serves. Shapers gate phase-specific work,
    /// such as syllable analysis and reordering, on the substitution role.
    /// </summary>
    public ShapingBufferRole Role { get; private set; }

    /// <summary>
    /// Gets the number of live glyphs.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a substitution pass is active.
    /// </summary>
    public bool IsPassActive { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether a substitution advances the
    /// lookup cursor past its output. Contextual lookups move the cursor as
    /// each nested lookup is applied.
    /// </summary>
    public bool DirectConsume { get; set; }

    /// <summary>
    /// Gets the glyph index at the lookup cursor.
    /// </summary>
    public int ReadIndex { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether any record carries a default
    /// ignorable codepoint. Recorded as records enter the buffer so the
    /// hide-ignorables stage can skip plain text without a scan.
    /// </summary>
    public bool HasDefaultIgnorables { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether any record carries the fraction
    /// slash. Recorded as records enter the buffer so automatic fraction
    /// forming costs nothing for the text that does not use it.
    /// </summary>
    public bool HasFractionSlash { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether any record carries a character
    /// that can begin a constrained vowel sequence. Recorded as records enter
    /// the buffer so text that cannot contain one is never walked for it.
    /// </summary>
    public bool HasVowelConstraintCandidates { get; set; }

    /// <summary>
    /// Gets the union of every feature bit enabled on any record, accumulated as
    /// features are turned on. A lookup whose mask shares no bit with it cannot
    /// match any record, so the drivers skip it without walking the buffer. The
    /// union only ever grows within a pass, so it is a superset: it can cost a
    /// walk that finds nothing, never skip a lookup that would have applied.
    /// </summary>
    public uint EnabledFeatureMaskUnion { get; private set; } = ShapePlanFeatures.GlobalFeatureMask;

    /// <summary>
    /// Gets a value indicating whether the applying lookup skips the zero width
    /// non-joiner during context matching instead of matching it.
    /// </summary>
    public bool LookupAutoZwnj { get; private set; } = true;

    /// <summary>
    /// Gets a value indicating whether the applying lookup skips the zero width
    /// joiner during sequence matching instead of matching it.
    /// </summary>
    public bool LookupAutoZwj { get; private set; } = true;

    /// <summary>
    /// Gets a value indicating whether the applying lookup never matches across
    /// syllable boundaries.
    /// </summary>
    public bool LookupPerSyllable { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the applying lookup selects random alternates.
    /// </summary>
    public bool LookupRandom { get; private set; }

    /// <summary>
    /// Gets the applying lookup's combined mask. Nested lookups inside
    /// contextual matches apply under the outer lookup's mask, so the stamped
    /// value holds for the whole application.
    /// </summary>
    public uint LookupMask { get; private set; } = uint.MaxValue;

    /// <summary>
    /// Gets the packed matcher flags for input sequence matching under the
    /// applying lookup, precomputed when the lookup is stamped so every match
    /// attempt copies them instead of re-deriving them.
    /// </summary>
    public byte InputMatchFlags { get; private set; }

    /// <summary>
    /// Gets the packed matcher flags for backtrack and lookahead matching under
    /// the applying lookup, precomputed when the lookup is stamped.
    /// </summary>
    public byte ContextMatchFlags { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a contextual rule is applying its
    /// nested lookups.
    /// </summary>
    public bool IsNestedApplication => this.nestedApplicationDepth > 0;

    /// <summary>
    /// Gets the number of records ahead of the pass position, still to be read.
    /// </summary>
    public int PassLookaheadLength => this.SpanEnd - this.ReadIndex;

    /// <summary>
    /// Gets a value indicating whether a further nested lookup application would
    /// exceed the maximum nesting depth. A font may chain contextual lookups
    /// into each other without bound, so recursion is capped rather than trusted.
    /// </summary>
    public bool NestingLimitReached => this.nestedApplicationDepth >= AdvancedTypographicUtils.MaxNestingLevel;

    /// <summary>
    /// Gets the text options used by this buffer.
    /// </summary>
    public TextOptions TextOptions { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether synthesized vertical origins follow
    /// the public shaping contract rather than the browser layout contract.
    /// </summary>
    public bool UseShapingVerticalOrigin { get; set; }

    /// <summary>
    /// Gets the approximate membership filter over every glyph id the buffer has ever
    /// contained. The digest only grows: substituted-away ids remain, keeping a
    /// definitive negative from <see cref="GlyphSetDigest.MightIntersect"/> sound while
    /// lookups mutate the buffer mid-application.
    /// </summary>
    public GlyphSetDigest GlyphDigest => this.glyphDigest;

    /// <summary>
    /// Gets or sets the running id of any ligature glyphs contained within this buffer.
    /// </summary>
    public int LigatureId { get; set; } = 1;

    /// <summary>
    /// Gets the declared text runs. Glyph records store an index into this list.
    /// </summary>
    public IReadOnlyList<TextRun> TextRuns { get; private set; } = Array.Empty<TextRun>();

    /// <summary>
    /// Gets the resolved runs and their exclusive input glyph ends.
    /// Consumed selectors and placeholders do not contribute to these counts.
    /// </summary>
    public ReadOnlySpan<(int End, ScriptItemizer.ShapingRun Run)> InputRuns => this.inputRuns.AsSlice().Span;

    /// <summary>
    /// Gets the first glyph in the current shaping span.
    /// Earlier spans can use another font and are outside backtrack context.
    /// </summary>
    public int SpanStart { get; private set; }

    /// <summary>
    /// Gets the exclusive end of the current shaping span, including changes
    /// in length from substitution.
    /// </summary>
    public int SpanEnd => this.Count - this.spanTailCount;

    /// <summary>
    /// Gets or sets a value indicating whether positioning wrote an out-of-range advance in this span.
    /// </summary>
    public bool HasOutOfRangeAdvances { get; set; }

    /// <summary>
    /// Gets the glyph count limit for the entire list.
    /// </summary>
    public int MaxShapingGlyphCount { get; private set; }

    /// <summary>
    /// Gets the number of operations left in the current shaping phase.
    /// </summary>
    public int RemainingOperations => this.Role == ShapingBufferRole.Substitution
        ? this.remainingSubstitutionOperations
        : this.remainingPositioningOperations;

    /// <summary>
    /// Gets or sets the script's native direction for an explicitly right-to-left
    /// horizontal run. The value is <see langword="null"/> until the script is resolved.
    /// </summary>
    public ScriptHorizontalDirection? HorizontalScriptDirection { get; set; }

    /// <summary>
    /// Gets the deletion events recorded by the buffer.
    /// State table events have a live owner and stay at the front of the list.
    /// Other removals are appended with no owner.
    /// </summary>
    public List<(int Index, int Owner, GlyphShapingData Glyph)> DeletedGlyphs => this.deletedGlyphs ??= [];

    /// <summary>
    /// Gets an interior reference to the glyph shaping data at the specified index.
    /// The reference writes through to the buffer's storage and is invalidated by any
    /// operation that inserts or removes glyphs.
    /// </summary>
    /// <remarks>
    /// The index counts only live glyphs. At and after the gap, the array index
    /// is the live index plus the length of the gap.
    /// </remarks>
    /// <param name="index">The zero-based index of the record to get.</param>
    /// <returns>The <see cref="GlyphShapingData"/>.</returns>
    public ref GlyphShapingData this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref this.data[index < this.gapIndex ? index : index + this.gapLength];
    }

    /// <summary>
    /// Records a resolved run and its end in the mapped input.
    /// </summary>
    /// <param name="end">The index after the run in the mapped input.</param>
    /// <param name="run">The shaping settings for the run.</param>
    public void AddInputRun(int end, ScriptItemizer.ShapingRun run) => this.inputRuns.Add((end, run));

    /// <summary>
    /// Sets the limits and reserves space for the mapped glyphs.
    /// </summary>
    /// <param name="codePointCount">The number of code points in the source text.</param>
    public void SetShapingLimits(int codePointCount)
    {
        this.MaxShapingGlyphCount = AdvancedTypographicUtils.GetMaxAllowableShapingCollectionCount(codePointCount);
        this.remainingSubstitutionOperations = AdvancedTypographicUtils.GetMaxAllowableShapingOperationsCount(codePointCount);
        this.remainingPositioningOperations = this.remainingSubstitutionOperations;

        // Each source character can supply one mapped glyph. An inline
        // placeholder supplies a glyph and does not consume a character.
        int capacity = codePointCount;
        for (int i = 0; i < this.TextRuns.Count; i++)
        {
            if (this.TextRuns[i].Placeholder.HasValue)
            {
                capacity++;
            }
        }

        this.EnsureCapacity(capacity);
    }

    /// <summary>
    /// Counts work against the current phase's limit.
    /// </summary>
    /// <param name="count">The number of operations.</param>
    /// <returns>Whether the remaining limit covered the work.</returns>
    public bool CountOperations(long count)
    {
        bool substitution = this.Role == ShapingBufferRole.Substitution;
        int remaining = substitution ? this.remainingSubstitutionOperations : this.remainingPositioningOperations;
        long difference = remaining - count;
        remaining = (int)Math.Max(0, difference);

        if (substitution)
        {
            this.remainingSubstitutionOperations = remaining;
        }
        else
        {
            this.remainingPositioningOperations = remaining;
        }

        return difference >= 0;
    }

    /// <summary>
    /// Sets the boundaries of the glyphs that the next shaping call can change
    /// and clears the record of out-of-range advances for the previous span.
    /// </summary>
    /// <param name="start">The first glyph in the span.</param>
    /// <param name="end">The first glyph after the span.</param>
    public void SetSpan(int start, int end)
    {
        this.SpanStart = start;
        this.spanTailCount = this.Count - end;
        this.HasOutOfRangeAdvances = false;
    }

    /// <summary>
    /// Gets the selected font and metrics for a positioned glyph.
    /// </summary>
    /// <param name="index">The zero-based index of the entry to get.</param>
    /// <returns>The <see cref="GlyphMetricsEntry"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref GlyphMetricsEntry MetricsAt(int index) => ref this.metrics[index];

    /// <summary>
    /// Gets an interior reference to the positioning entry at the specified index.
    /// Valid only after the buffer has been seeded.
    /// </summary>
    /// <param name="index">The zero-based index of the entry to get.</param>
    /// <returns>The <see cref="GlyphShapingPosition"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref GlyphShapingPosition PositionAt(int index) => ref this.positions[index];

    /// <summary>
    /// Assigns the text runs before the buffer receives its first glyph.
    /// Each glyph stores an index into this list.
    /// </summary>
    /// <param name="textRuns">The resolved text runs covering the input.</param>
    public void SetTextRuns(IReadOnlyList<TextRun> textRuns) => this.TextRuns = textRuns;

    /// <summary>
    /// Changes the phase of the shared glyph buffer.
    /// </summary>
    /// <param name="role">The shaping phase the buffer serves next.</param>
    public void SetRole(ShapingBufferRole role) => this.Role = role;

    /// <summary>
    /// Seeds the metrics and natural advances for a nonempty span after
    /// substitution. All glyphs in the span belong to the same text run.
    /// </summary>
    /// <param name="font">The font used to shape the span.</param>
    /// <param name="start">The first glyph in the span.</param>
    public void SeedMetricsInPlace(Font font, int start)
    {
        FontMetrics fontMetrics = font.FontMetrics;
        LayoutMode layoutMode = this.TextOptions.LayoutMode;
        TextRun textRun = this.TextRuns[this.data[start].TextRunIndex];
        TextAttributes textAttributes = textRun.TextAttributes;
        TextDecorations textDecorations = textRun.TextDecorations;
        ColorFontSupport colorFontSupport = textRun.ColorFontSupport ?? this.TextOptions.ColorFontSupport;
        FontPalette? fontPalette = textRun.FontPalette ?? this.TextOptions.FontPalette;
        uint verticalMask = ShapePlanFeatures.VerticalFeatureMask;

        int end = this.SpanEnd;
        for (int i = start; i < end; i++)
        {
            ref GlyphShapingData slot = ref this.data[i];
            CodePoint codePoint = slot.CodePoint;
            bool isVertical = AdvancedTypographicUtils.IsVerticalGlyph(codePoint, layoutMode)
                || (slot.AppliedFeatureMask & verticalMask) != 0;

            FontGlyphMetrics glyphMetrics = this.GetGlyphMetrics(
                fontMetrics,
                codePoint,
                slot.GlyphId,
                textAttributes,
                textDecorations,
                layoutMode,
                colorFontSupport,
                fontPalette);

            // The substitution plan already registered this glyph's positioning
            // features. Keep those masks and seed only its geometry and metrics.
            this.positions[i] = new(isVertical
                ? new(0, 0, 0, glyphMetrics.AdvanceHeight)
                : new(0, 0, glyphMetrics.AdvanceWidth, 0));

            this.metrics[i] = new(font, glyphMetrics);
        }
    }

    /// <summary>
    /// Resets the buffer for reuse by a new shaping pass: adopts the new options,
    /// re-resolves the language candidates, empties the digest, and truncates the glyph
    /// count. Records are stored by value, so no per-record cleanup is required and
    /// storage is retained at its high-water mark.
    /// </summary>
    /// <param name="textOptions">The text options for the new pass.</param>
    public void Reset(TextOptions textOptions)
    {
        this.Count = 0;
        this.gapIndex = int.MaxValue;
        this.gapLength = 0;
        this.IsPassActive = false;
        this.DirectConsume = false;
        this.ReadIndex = 0;
        this.LigatureId = 1;
        this.HorizontalScriptDirection = null;
        this.randomState = 1;
        this.EnabledFeatureMaskUnion = ShapePlanFeatures.GlobalFeatureMask;
        this.glyphDigest = default;
        this.HasDefaultIgnorables = false;
        this.HasFractionSlash = false;
        this.HasVowelConstraintCandidates = false;
        this.SpanStart = 0;
        this.spanTailCount = 0;
        this.placeholderBidiRuns.Clear();
        this.inputRuns.AsSlice().Span.Clear();
        this.inputRuns.Clear();
        this.deletedGlyphs?.Clear();

        // Cached plans and language tags captured option values when built, so what
        // invalidates them is those values changing. A caller shaping run after run
        // through one buffer hands the same options over each time with the members
        // rewritten, so identity alone would never notice. Tracking only affects
        // plan construction when it crosses zero; its magnitude is applied by
        // layout and therefore does not belong in the shape-plan cache key.
        string language = textOptions.Culture?.Name ?? string.Empty;
        if (!ReferenceEquals(this.TextOptions, textOptions)
            || !string.Equals(this.languageKey, language, StringComparison.Ordinal)
            || !ReferenceEquals(this.featureKey, textOptions.FeatureTags)
            || this.layoutModeKey != textOptions.LayoutMode
            || this.kerningModeKey != textOptions.KerningMode
            || this.hasTrackingKey != (textOptions.Tracking != 0))
        {
            this.planCache.Clear();
            this.TextOptions = textOptions;
            this.languageKey = language;
            this.featureKey = textOptions.FeatureTags;
            this.layoutModeKey = textOptions.LayoutMode;
            this.kerningModeKey = textOptions.KerningMode;
            this.hasTrackingKey = textOptions.Tracking != 0;
        }
    }

    /// <summary>
    /// Sets the applying lookup's mask, joiner handling, and syllable scope for
    /// the duration of its application. The drivers stamp this before each
    /// merged lookup entry; sequence matching reads it through the skipping
    /// iterator.
    /// </summary>
    /// <param name="mask">The lookup's combined mask.</param>
    /// <param name="autoZwnj">Whether the lookup skips the zero width non-joiner during context matching.</param>
    /// <param name="autoZwj">Whether the lookup skips the zero width joiner.</param>
    /// <param name="random">Whether alternate substitutions select randomly.</param>
    /// <param name="perSyllable">Whether matching is confined to one syllable.</param>
    public void SetLookupMatchState(uint mask, bool autoZwnj, bool autoZwj, bool random, bool perSyllable)
    {
        this.LookupMask = mask;
        this.LookupRandom = random;

        // Consecutive lookups mostly share their joiner handling, so the packed
        // bytes are only rebuilt when one of their five inputs actually changed.
        if (this.packedFlagsValid
            && autoZwnj == this.LookupAutoZwnj
            && autoZwj == this.LookupAutoZwj
            && perSyllable == this.LookupPerSyllable
            && this.packedFlagsRole == this.Role
            && this.packedFlagsHadIgnorables == this.HasDefaultIgnorables)
        {
            return;
        }

        this.LookupAutoZwnj = autoZwnj;
        this.LookupAutoZwj = autoZwj;
        this.LookupPerSyllable = perSyllable;
        this.packedFlagsValid = true;
        this.packedFlagsRole = this.Role;
        this.packedFlagsHadIgnorables = this.HasDefaultIgnorables;
        this.InputMatchFlags = SkippingGlyphIterator.PackMatchFlags(this, false);
        this.ContextMatchFlags = SkippingGlyphIterator.PackMatchFlags(this, true);
    }

    /// <summary>
    /// Advances and returns the deterministic random sequence used by alternate substitution.
    /// </summary>
    /// <returns>The next random value.</returns>
    public uint NextRandomNumber()
    {
        this.randomState = unchecked(this.randomState * RandomMultiplier) % RandomModulus;
        return this.randomState;
    }

    /// <summary>
    /// Gets shaper scratch storage of at least the given length, grown to the
    /// workload's high-water mark and retained. Contents are undefined on entry;
    /// callers write every slot they read.
    /// </summary>
    /// <param name="length">The capacity required.</param>
    /// <returns>The scratch storage; entries beyond the length are undefined.</returns>
    public byte[] GetShaperScratch(int length)
    {
        if (this.shaperScratch.Length < length)
        {
            this.shaperScratch = new byte[Math.Max(length, Math.Max(64, this.shaperScratch.Length * 2))];
        }

        return this.shaperScratch;
    }

    /// <summary>
    /// Sets the glyph id at the specified index, recording the id in
    /// <see cref="GlyphDigest"/>. Callers outside the buffer must use this rather than
    /// writing <see cref="GlyphShapingData.GlyphId"/> directly, which would leave the
    /// digest unaware of the new id.
    /// </summary>
    /// <param name="index">The zero-based index of the record.</param>
    /// <param name="glyphId">The glyph id to set.</param>
    public void SetGlyphId(int index, ushort glyphId)
    {
        this.glyphDigest.Add(glyphId);
        this[index].GlyphId = glyphId;
    }

    /// <summary>
    /// Adds the shaping feature to every record in the given range. The caller
    /// resolves the feature's mask bit once for the whole range: shaper plans
    /// register each stage feature across the full run, so the per-glyph work must
    /// be a single bitwise OR.
    /// </summary>
    /// <param name="index">The zero-based index of the first record.</param>
    /// <param name="count">The number of records in the range.</param>
    /// <param name="feature">The feature to apply.</param>
    /// <param name="mask">The feature's plan-assigned mask bit.</param>
    public void AddShapingFeatureRange(int index, int count, TagEntry feature, uint mask)
    {
        if (feature.Enabled)
        {
            this.EnabledFeatureMaskUnion |= mask;
        }

        int end = index + count;
        for (int i = index; i < end; i++)
        {
            ref GlyphShapingData item = ref this.data[i];
            item.RegisteredFeatureMask |= mask;
            if (feature.Enabled)
            {
                item.FeatureMask |= mask;
            }
        }
    }

    /// <summary>
    /// Applies the plan's folded whole-segment feature masks in a single walk:
    /// the registered and enabled bits every replayed planning pass would
    /// otherwise add one feature range at a time.
    /// </summary>
    /// <param name="index">The zero-based index of the first record.</param>
    /// <param name="count">The number of records.</param>
    /// <param name="registeredMask">The fold of the registered feature bits.</param>
    /// <param name="enabledMask">The fold of the enabled feature bits.</param>
    public void AddShapingFeatureMasks(int index, int count, uint registeredMask, uint enabledMask)
    {
        this.EnabledFeatureMaskUnion |= enabledMask;

        int end = index + count;
        for (int i = index; i < end; i++)
        {
            ref GlyphShapingData item = ref this.data[i];
            item.RegisteredFeatureMask |= registeredMask;
            item.FeatureMask |= enabledMask;
        }
    }

    /// <summary>
    /// Enables a previously added shaping feature by its plan-assigned mask bit.
    /// </summary>
    /// <remarks>
    /// Intersecting with the registered mask preserves the contract that enabling a
    /// feature a shaper never added for this record is a no-op.
    /// </remarks>
    /// <param name="index">The zero-based index of the record.</param>
    /// <param name="mask">The feature's plan-assigned mask bit.</param>
    public void EnableShapingFeature(int index, uint mask)
    {
        this.EnabledFeatureMaskUnion |= mask;
        ref GlyphShapingData item = ref this.data[index];
        item.FeatureMask |= item.RegisteredFeatureMask & mask;
    }

    /// <summary>
    /// Disables a previously added shaping feature by its plan-assigned mask bit.
    /// </summary>
    /// <remarks>
    /// An unassigned feature yields a zero mask whose complement clears nothing.
    /// </remarks>
    /// <param name="index">The zero-based index of the record.</param>
    /// <param name="mask">The feature's plan-assigned mask bit.</param>
    public void DisableShapingFeature(int index, uint mask)
    {
        ref GlyphShapingData item = ref this.data[index];
        item.FeatureMask &= ~mask;
    }

    /// <summary>
    /// Adds the glyph id and the codepoint it represents.
    /// </summary>
    /// <param name="glyphId">The id of the glyph to add.</param>
    /// <param name="codePoint">The codepoint the glyph represents.</param>
    /// <param name="direction">The resolved text direction for the codepoint.</param>
    /// <param name="textRunIndex">The index of the text run this glyph belongs to.</param>
    /// <param name="codePointIndex">The zero-based index within the input codepoint buffer.</param>
    /// <param name="stringIndex">The zero-based char index in the original text.</param>
    /// <param name="graphemeIndex">The zero-based index of the grapheme the glyph belongs to.</param>
    public void AddGlyph(ushort glyphId, CodePoint codePoint, TextDirection direction, ushort textRunIndex, int codePointIndex, int stringIndex, int graphemeIndex)
    {
        this.glyphDigest.Add(glyphId);
        ref GlyphShapingData slot = ref this.Append();
        slot = new(textRunIndex)
        {
            CodePointIndex = codePointIndex,
            StringIndex = stringIndex,
            GraphemeIndex = graphemeIndex,
            CodePoint = codePoint,
            Direction = direction,
            GlyphId = glyphId,
        };

        // The render-as-whitespace carve-outs are default ignorables that fonts
        // implement as regular spacing glyphs, such as the Hangul fillers; those
        // keep their glyphs. The joiners and the substitution-visible ignorables
        // (Mongolian free variation selectors, tag characters, the combining
        // grapheme joiner) carry their own bits for the matcher's transparency
        // rules.
        uint value = (uint)codePoint.Value;

        // The fraction slash is the only trigger for automatic fraction
        // forming; recording it here keeps that stage free for other text.
        if (value == FractionSlashCodePoint)
        {
            this.HasFractionSlash = true;
        }

        // Only a character that can begin a constrained vowel sequence makes the
        // sequence worth looking for, and they all sit above this one.
        if (value >= FirstVowelConstraintCharacter)
        {
            this.HasVowelConstraintCandidates = true;
        }

        if (value >= 0x80
            && UnicodeUtility.IsDefaultIgnorableCodePoint(value)
            && !UnicodeUtility.ShouldRenderWhiteSpaceOnly(codePoint))
        {
            slot.IsDefaultIgnorable = true;
            this.HasDefaultIgnorables = true;

            if (CodePoint.IsZeroWidthNonJoiner(codePoint))
            {
                slot.IsZwnj = true;
            }
            else if (CodePoint.IsZeroWidthJoiner(codePoint))
            {
                slot.IsZwj = true;
            }
            else if (value is (>= 0x180B and <= 0x180D) or 0x180F
                or (>= 0xE0020 and <= 0xE007F)
                or 0x034F)
            {
                // MONGOLIAN FREE VARIATION SELECTOR ONE..FOUR, TAG SPACE..CANCEL
                // TAG, and COMBINING GRAPHEME JOINER: substitution must still see
                // these, while positioning treats them as transparent.
                slot.IsHiddenIgnorable = true;
            }
        }
    }

    /// <summary>
    /// Adds the input record for an inline placeholder.
    /// </summary>
    /// <param name="codePoint">The object replacement codepoint used for Unicode processing.</param>
    /// <param name="bidiRun">The resolved bidi run for the placeholder.</param>
    /// <param name="textRunIndex">The index of the text run this placeholder belongs to.</param>
    /// <param name="codePointIndex">The zero-based index within the input codepoint buffer.</param>
    public void AddPlaceholder(CodePoint codePoint, BidiRun bidiRun, ushort textRunIndex, int codePointIndex)
    {
        ref GlyphShapingData slot = ref this.Append();
        slot = new(textRunIndex)
        {
            CodePointIndex = codePointIndex,
            CodePoint = codePoint,
            Direction = (TextDirection)bidiRun.Direction,
            GlyphId = 0,
            IsPlaceholder = true,
        };

        this.placeholderBidiRuns.Add((codePointIndex, bidiRun));
    }

    /// <summary>
    /// Initializes a placeholder's metrics after earlier spans have finished substitution.
    /// </summary>
    /// <param name="index">The placeholder's index in the glyph list.</param>
    public void SeedPlaceholderMetrics(int index)
    {
        TextRun run = this.TextRuns[this[index].TextRunIndex];
        Font font = run.ResolvedFont;
        FontGlyphMetrics metrics = PlaceholderGlyphMetrics.Create(font, run, this.TextOptions.Dpi);
        this.glyphDigest.Add(metrics.GlyphId);
        this.metrics[index] = new(font, metrics);
        this.positions[index] = new(this.TextOptions.LayoutMode.IsVertical()
            ? new(0, 0, 0, metrics.AdvanceHeight)
            : new(0, 0, metrics.AdvanceWidth, 0));
    }

    /// <summary>
    /// Gets the bidi run recorded for the placeholder at the given codepoint index.
    /// Placeholders shape in isolated single-glyph runs, so their indices are stable
    /// for the lifetime of the pass.
    /// </summary>
    /// <param name="codePointIndex">The placeholder's zero-based codepoint index.</param>
    /// <returns>The recorded <see cref="BidiRun"/>, or the default when none was recorded.</returns>
    public BidiRun GetPlaceholderBidiRun(int codePointIndex)
    {
        List<(int CodePointIndex, BidiRun Run)> runs = this.placeholderBidiRuns;
        for (int i = 0; i < runs.Count; i++)
        {
            if (runs[i].CodePointIndex == codePointIndex)
            {
                return runs[i].Run;
            }
        }

        return default;
    }

    /// <summary>
    /// Moves the specified glyph and its original input indices to the specified
    /// position.
    /// </summary>
    /// <remarks>
    /// This method requires a packed glyph array. An AAT rearrangement starts
    /// with a packed array and only moves existing glyphs.
    /// </remarks>
    /// <param name="fromIndex">The index to move from.</param>
    /// <param name="toIndex">The index to move to.</param>
    public void MoveGlyph(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex)
        {
            return;
        }

        GlyphShapingData[] items = this.data;
        GlyphShapingData moved = items[fromIndex];

        if (fromIndex > toIndex)
        {
            Array.Copy(items, toIndex, items, toIndex + 1, fromIndex - toIndex);
        }
        else
        {
            Array.Copy(items, fromIndex + 1, items, fromIndex, toIndex - fromIndex);
        }

        items[toIndex] = moved;
    }

    /// <summary>
    /// Assigns the earliest stored input starts to every record in a shaping range.
    /// </summary>
    /// <param name="startIndex">The first record in the range.</param>
    /// <param name="endIndex">The first record after the range.</param>
    public void CombineInputStarts(int startIndex, int endIndex)
    {
        int sourceIndex = startIndex;
        for (int i = startIndex + 1; i < endIndex; i++)
        {
            if (this[i].StringIndex < this[sourceIndex].StringIndex)
            {
                sourceIndex = i;
            }
        }

        GlyphShapingData source = this[sourceIndex];
        int sourceStringIndex = source.StringIndex;
        int leadingStringIndex = this[startIndex].StringIndex;
        int trailingStringIndex = this[endIndex - 1].StringIndex;

        // HarfBuzz extends a changed boundary across adjacent records that already
        // share its old input start. This keeps every output copied from that input
        // together when only part of it intersects the requested shaping range.
        // The previous font span keeps its own source indices.
        if (sourceStringIndex != trailingStringIndex)
        {
            while (endIndex < this.SpanEnd && this[endIndex].StringIndex == trailingStringIndex)
            {
                endIndex++;
            }
        }

        if (sourceStringIndex != leadingStringIndex)
        {
            while (startIndex > this.SpanStart && this[startIndex - 1].StringIndex == leadingStringIndex)
            {
                startIndex--;
            }
        }

        for (int i = startIndex; i < endIndex; i++)
        {
            ref GlyphShapingData glyph = ref this[i];
            glyph.CodePointIndex = source.CodePointIndex;
            glyph.StringIndex = source.StringIndex;
            glyph.GraphemeIndex = source.GraphemeIndex;
        }
    }

    /// <summary>
    /// Reverses the order of glyph records in the specified range.
    /// </summary>
    /// <remarks>
    /// The range is interpreted as half-open, from <paramref name="startIndex"/>
    /// (inclusive) to <paramref name="endIndex"/> (exclusive). Both indices are clamped
    /// to the valid range [0, <see cref="Count"/>]. If the resulting range contains
    /// fewer than two records, the method performs no action.
    /// </remarks>
    /// <param name="startIndex">The zero-based index at which to start reversing (inclusive).</param>
    /// <param name="endIndex">The zero-based index at which to stop reversing (exclusive).</param>
    public void ReverseRange(int startIndex, int endIndex)
    {
        int s = Math.Max(0, Math.Min(startIndex, this.Count));
        int e = Math.Max(0, Math.Min(endIndex, this.Count));

        if (e < s + 2)
        {
            return;
        }

        // A record is its shaping state, its metrics, and its position, held in
        // step across three arrays; all three move together or the record comes
        // apart.
        int length = e - s;

        Array.Reverse(this.data, s, length);
        Array.Reverse(this.metrics, s, length);
        Array.Reverse(this.positions, s, length);
    }

    /// <summary>
    /// Reverses the grapheme order in the specified range while preserving the
    /// shaped glyph order within each grapheme.
    /// </summary>
    /// <param name="startIndex">The zero-based index at which to start reversing (inclusive).</param>
    /// <param name="endIndex">The zero-based index at which to stop reversing (exclusive).</param>
    public void ReverseGraphemeRange(int startIndex, int endIndex)
    {
        int start = Math.Max(0, Math.Min(startIndex, this.Count));
        int end = Math.Max(0, Math.Min(endIndex, this.Count));
        if (end < start + 2)
        {
            return;
        }

        // Reversing all records puts the graphemes in the required order but also
        // reverses every multi-glyph grapheme. Reverse each contiguous grapheme
        // again to restore the glyph stream produced for that grapheme.
        this.ReverseRange(start, end);
        int graphemeStart = start;
        while (graphemeStart < end)
        {
            int graphemeIndex = this.data[graphemeStart].GraphemeIndex;
            int graphemeEnd = graphemeStart + 1;
            while (graphemeEnd < end && this.data[graphemeEnd].GraphemeIndex == graphemeIndex)
            {
                graphemeEnd++;
            }

            this.ReverseRange(graphemeStart, graphemeEnd);
            graphemeStart = graphemeEnd;
        }
    }

    /// <summary>
    /// Performs a stable sort of the glyph records by the comparison delegate.
    /// </summary>
    /// <param name="startIndex">The start index.</param>
    /// <param name="endIndex">The end index.</param>
    /// <param name="comparer">The comparison delegate.</param>
    public void Sort(int startIndex, int endIndex, Comparison<GlyphShapingData> comparer)
    {
        // The sorted ranges are typically small runs of marks or syllables, so a
        // stable insertion sort avoids both allocation and general-purpose sort
        // overhead.
        GlyphShapingData[] items = this.data;
        for (int i = startIndex + 1; i < endIndex; i++)
        {
            int j = i;
            while (j > startIndex && comparer(items[j - 1], items[i]) > 0)
            {
                j--;
            }

            if (j == i)
            {
                continue;
            }

            this.CombineInputStarts(j, i + 1);
            this.MoveGlyph(i, j);
        }
    }

    /// <summary>
    /// Performs a 1:1 replacement of a glyph id at the given position.
    /// </summary>
    /// <param name="index">The zero-based index of the record to replace.</param>
    /// <param name="glyphId">The replacement glyph id.</param>
    /// <param name="feature">The feature to apply to the record at the specified index.</param>
    public void Replace(int index, ushort glyphId, Tag feature)
    {
        this.glyphDigest.Add(glyphId);
        ref GlyphShapingData current = ref this[index];
        current.GlyphId = glyphId;
        current.LigatureId = 0;
        current.LigatureComponent = -1;
        current.IsSubstituted = true;
        current.AppliedFeatureMask |= ShapePlanFeatures.GetVerticalMask(feature);

        if (this.IsPassActive && this.DirectConsume && index == this.ReadIndex)
        {
            this.ReadIndex++;
        }
    }

    /// <summary>
    /// Performs a 1:1 replacement without changing the record's ligature attachment.
    /// </summary>
    /// <param name="index">The zero-based index of the record to replace.</param>
    /// <param name="glyphId">The replacement glyph id.</param>
    /// <param name="feature">The feature to apply to the record at the specified index.</param>
    public void ReplaceInPlace(int index, ushort glyphId, Tag feature)
    {
        this.glyphDigest.Add(glyphId);

        // Reverse substitutions change only the glyph identity. Attachment metadata
        // must survive because later mark positioning still targets the same record.
        ref GlyphShapingData current = ref this[index];
        current.GlyphId = glyphId;
        current.IsSubstituted = true;
        current.AppliedFeatureMask |= ShapePlanFeatures.GetVerticalMask(feature);
    }

    /// <summary>
    /// Performs a 1:1 replacement of a glyph id at the given position while removing a
    /// series of records at the given positions within the sequence.
    /// </summary>
    /// <param name="index">The zero-based index of the record to replace.</param>
    /// <param name="removalIndices">The indices at which to remove records.</param>
    /// <param name="glyphId">The replacement glyph id.</param>
    /// <param name="ligatureId">The ligature id.</param>
    /// <param name="ligatureComponent">The ligature component retained by a mark ligature, or -1 for a new ligature.</param>
    /// <param name="feature">The feature to apply to the record at the specified index.</param>
    public void Replace(int index, ReadOnlySpan<int> removalIndices, ushort glyphId, int ligatureId, int ligatureComponent, Tag feature)
    {
        if (!removalIndices.IsEmpty)
        {
            this.CombineInputStarts(index, removalIndices[^1] + 1);
        }

        // Gather the merged codepoint bookkeeping from the component records
        // before any of them move.
        int codePointCount = 0;
        CodePoint codePoint = default;

        for (int i = removalIndices.Length - 1; i >= 0; i--)
        {
            ref GlyphShapingData consumed = ref this[removalIndices[i]];

            codePointCount += consumed.CodePointCount;
            CodePoint currentCodePoint = consumed.CodePoint;
            if (!UnicodeUtility.IsDefaultIgnorableCodePoint((uint)currentCodePoint.Value) || UnicodeUtility.ShouldRenderWhiteSpaceOnly(currentCodePoint))
            {
                if (!CodePoint.IsZeroWidthJoiner(currentCodePoint) && !CodePoint.IsZeroWidthNonJoiner(currentCodePoint))
                {
                    // A visible matched component may identify the ligature for
                    // later Unicode-property fallbacks. Formatting controls must
                    // not replace that identity merely because the font consumed them.
                    codePoint = currentCodePoint;
                }
            }
        }

        this.glyphDigest.Add(glyphId);

        // Remove ranges from right to left so each remaining index is still valid.
        // Adjacent components share one move. Marks between components stay in order.
        int remaining = removalIndices.Length;
        while (remaining > 0)
        {
            int end = removalIndices[--remaining] + 1;
            int start = end - 1;
            while (remaining > 0 && removalIndices[remaining - 1] == start - 1)
            {
                remaining--;
                start--;
            }

            this.RemoveRange(start, end - start);
        }

        // Assign our new id at the index. The reference is taken after every removal
        // so it addresses the record's final slot.
        ref GlyphShapingData current = ref this[index];
        if (codePoint != default)
        {
            current.CodePoint = codePoint;
        }

        current.CodePointCount += codePointCount;
        current.GlyphId = glyphId;
        current.LigatureId = ligatureId;
        current.IsLigated = true;

        // Only the most recent ligature/multiple transformation controls
        // reordering decisions. Religation forgives an earlier expansion.
        current.IsDecomposed = false;
        current.LigatureComponent = ligatureComponent;
        current.IsSubstituted = true;
        current.AppliedFeatureMask |= ShapePlanFeatures.GetVerticalMask(feature);

        if (this.IsPassActive && this.DirectConsume && index == this.ReadIndex)
        {
            // Resume after the matched span, including any marks it skipped.
            // Subtract the removed components from the span's original end.
            this.ReadIndex = removalIndices.IsEmpty ? index + 1 : removalIndices[^1] + 1 - removalIndices.Length;
        }
    }

    /// <summary>
    /// Performs a 1:1 replacement of a glyph id at the given position while removing a
    /// series of following records.
    /// </summary>
    /// <param name="index">The zero-based index of the record to replace.</param>
    /// <param name="count">The number of following records to remove.</param>
    /// <param name="glyphId">The replacement glyph id.</param>
    /// <param name="feature">The feature to apply to the record at the specified index.</param>
    public void Replace(int index, int count, ushort glyphId, Tag feature)
    {
        if (count > 0)
        {
            this.CombineInputStarts(index, index + count + 1);
        }

        // Gather the merged codepoint bookkeeping from the following records
        // before any of them move.
        int codePointCount = 0;
        CodePoint codePoint = default;

        for (int i = count; i > 0; i--)
        {
            ref GlyphShapingData consumed = ref this[index + i];

            codePointCount += consumed.CodePointCount;
            CodePoint currentCodePoint = consumed.CodePoint;
            if (!UnicodeUtility.IsDefaultIgnorableCodePoint((uint)currentCodePoint.Value) || UnicodeUtility.ShouldRenderWhiteSpaceOnly(currentCodePoint))
            {
                if (!CodePoint.IsZeroWidthJoiner(currentCodePoint) && !CodePoint.IsZeroWidthNonJoiner(currentCodePoint))
                {
                    // Keep the last visible component as the replacement's
                    // Unicode identity; consumed formatting controls contribute
                    // to its text span but not to its shaping properties.
                    codePoint = currentCodePoint;
                }
            }
        }

        this.glyphDigest.Add(glyphId);

        // Remove the consumed glyphs as one range. During a pass, their slots
        // extend the gap and the unread glyphs stay in place.
        this.RemoveRange(index + 1, count);

        // Take the reference after removal so it addresses the final slot.
        ref GlyphShapingData current = ref this[index];
        if (codePoint != default)
        {
            current.CodePoint = codePoint;
        }

        current.CodePointCount += codePointCount;
        current.GlyphId = glyphId;
        current.LigatureId = 0;
        current.LigatureComponent = -1;
        current.IsSubstituted = true;
        current.AppliedFeatureMask |= ShapePlanFeatures.GetVerticalMask(feature);

        if (this.IsPassActive && this.DirectConsume && index == this.ReadIndex)
        {
            this.ReadIndex++;
        }
    }

    /// <summary>
    /// Joins the record at <paramref name="mergeIndex"/> into the one at
    /// <paramref name="index"/>, which takes the given glyph, and removes it. The
    /// two records need not sit next to each other: records between them keep both
    /// their place and their order.
    /// </summary>
    /// <remarks>
    /// The joined record covers the text of both, so the codepoint-to-glyph
    /// projection stays total. The caller sets the codepoint, because the character
    /// the pair stands for is the caller's to name.
    /// </remarks>
    /// <param name="index">The zero-based index of the record that remains.</param>
    /// <param name="mergeIndex">The zero-based index of the record that is folded in.</param>
    /// <param name="glyphId">The glyph the remaining record takes.</param>
    /// <param name="feature">The feature to apply to the remaining record.</param>
    public void MergeGlyph(int index, int mergeIndex, ushort glyphId, Tag feature)
    {
        this.glyphDigest.Add(glyphId);
        this.CombineInputStarts(index, mergeIndex + 1);

        GlyphShapingData merged = this[mergeIndex];
        int codePointCount = merged.CodePointCount;

        this.RemoveAt(mergeIndex);

        ref GlyphShapingData current = ref this[index];
        current.CodePointCount += codePointCount;
        current.GlyphId = glyphId;
        current.LigatureId = 0;
        current.LigatureComponent = -1;
        current.IsSubstituted = true;
        current.AppliedFeatureMask |= ShapePlanFeatures.GetVerticalMask(feature);
    }

    /// <summary>
    /// Replaces a single glyph id with a buffer of glyph ids.
    /// </summary>
    /// <param name="index">The zero-based index of the record to replace.</param>
    /// <param name="glyphIds">The buffer of replacement glyph ids.</param>
    /// <param name="feature">The feature to apply to the record at the specified index.</param>
    public void Replace(int index, ReadOnlySpan<ushort> glyphIds, Tag feature)
    {
        if (glyphIds.Length > 0)
        {
            this.glyphDigest.Add(glyphIds[0]);
            ref GlyphShapingData first = ref this[index];
            bool preservesLigatureAttachment = first.LigatureId > 0;
            first.GlyphId = glyphIds[0];
            if (!preservesLigatureAttachment)
            {
                // Preserve an existing attachment across every output; only an
                // unattached expansion creates new component indices.
                first.LigatureComponent = 0;
            }

            first.IsSubstituted = true;
            first.IsDecomposed = true;

            // Add additional glyphs from the rest of the sequence. Insertion can grow
            // the storage, so the mutated record is captured by value rather than held
            // by reference.
            if (glyphIds.Length > 1)
            {
                GlyphShapingData template = first;
                uint mask = ShapePlanFeatures.GetVerticalMask(feature);
                int addedCount = glyphIds.Length - 1;
                int insertionIndex = index + 1;

                this.InsertGlyphSlots(insertionIndex, addedCount);
                if (this.Role == ShapingBufferRole.Positioning)
                {
                    Array.Clear(this.metrics, insertionIndex, addedCount);
                    Array.Clear(this.positions, insertionIndex, addedCount);
                }

                // The new slots are before the unused space, so their live
                // indices also address their slots in the array.
                for (int i = 0; i < addedCount; i++)
                {
                    // Each opened slot starts from the complete first output. Assigning
                    // the glyph through its property invalidates only the class cache.
                    GlyphShapingData inserted = template;
                    inserted.GlyphId = glyphIds[i + 1];

                    if (!preservesLigatureAttachment)
                    {
                        inserted.LigatureComponent = i + 1;
                    }

                    inserted.AppliedFeatureMask |= mask;
                    this.glyphDigest.Add(glyphIds[i + 1]);
                    this.data[insertionIndex + i] = inserted;
                }

                this.Count += addedCount;
            }

            if (this.IsPassActive && this.DirectConsume && index == this.ReadIndex)
            {
                // This lookup visits the original input once. Its new glyphs
                // are available to later lookups.
                this.ReadIndex = index + glyphIds.Length;
            }
        }
        else
        {
            // Spec disallows removal of glyphs in this manner but it's common enough practice to allow it.
            // https://github.com/MicrosoftDocs/typography-issues/issues/673
            this.RemoveAt(index);
        }
    }

    /// <summary>
    /// Inserts the shaping data at the given index, adopting the slot's codepoint index.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert.</param>
    /// <param name="data">The shaping data to insert.</param>
    public void Insert(int index, GlyphShapingData data)
    {
        data.CodePointIndex = this[index].CodePointIndex;
        this.InsertAt(index, data);
    }

    /// <summary>
    /// Inserts a fully positioned glyph record while preserving its resolved
    /// metrics and placement. This is used by post-position expansion, after the
    /// three parallel streams have all become meaningful.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert.</param>
    /// <param name="data">The shaping record to insert.</param>
    /// <param name="metricsEntry">The resolved metrics to insert.</param>
    /// <param name="position">The positioned geometry to insert.</param>
    public void InsertPositioned(int index, in GlyphShapingData data, in GlyphMetricsEntry metricsEntry, in GlyphShapingPosition position)
    {
        this.EnsureCapacity(this.Count + 1);

        // The suffix moves to make room for this glyph. Only the active span
        // has metrics and positions to move.
        int positionedTailCount = this.SpanEnd - index;
        Array.Copy(this.data, index, this.data, index + 1, this.Count - index);
        Array.Copy(this.metrics, index, this.metrics, index + 1, positionedTailCount);
        Array.Copy(this.positions, index, this.positions, index + 1, positionedTailCount);

        this.data[index] = data;
        this.metrics[index] = metricsEntry;
        this.positions[index] = position;
        this.Count++;
    }

    /// <summary>
    /// Resolves a glyph id through a direct-mapped cache in front of the font's own
    /// resolver, which hashes a dictionary per lookup. A hit is one load and one
    /// masked compare. No synchronization is needed: a pooled buffer is exclusively
    /// owned for the duration of a shaping pass.
    /// </summary>
    /// <param name="fontMetrics">The font metrics to resolve against.</param>
    /// <param name="codePoint">The codepoint to look up.</param>
    /// <param name="nextCodePoint">The optional following codepoint for variation sequence matching.</param>
    /// <param name="glyphId">When this method returns, contains the glyph id if found.</param>
    /// <param name="skipNextCodePoint">When this method returns, indicates whether the following codepoint was consumed.</param>
    /// <returns><see langword="true"/> if a glyph was found.</returns>
    public bool TryGetGlyphId(FontMetrics fontMetrics, CodePoint codePoint, CodePoint? nextCodePoint, out ushort glyphId, out bool skipNextCodePoint)
    {
        // Glyph IDs and variation mappings are local to one face.
        if (!ReferenceEquals(this.glyphIdCacheOwner, fontMetrics))
        {
            Array.Clear(this.glyphIdCacheEntries);
            this.glyphIdCacheOwner = fontMetrics;
        }

        // Each entry holds the current codepoint in bits 0–20 and the next
        // codepoint plus one in bits 21–42. Zero in the second field means
        // there is no next codepoint. Bit 63 distinguishes an empty slot.
        ulong key = GlyphIdCacheMarkerFlag
            | (uint)codePoint.Value
            | ((ulong)(uint)((nextCodePoint?.Value + 1) ?? 0) << GlyphIdCacheNextShift);

        // The low byte selects one of 256 slots. Compare the full key because
        // different codepoints and selectors can share that slot.
        int slot = codePoint.Value & 0xFF;
        ulong entry = this.glyphIdCacheEntries[slot];
        if ((entry & GlyphIdCacheTagMask) == key)
        {
            // Bits 43–58 hold the glyph ID. Bits 59 and 60 say whether the
            // font found a glyph and whether the sequence consumed the next
            // codepoint. These flags do not participate in the key comparison.
            glyphId = (ushort)(entry >> GlyphIdCacheGlyphShift);
            skipNextCodePoint = (entry & GlyphIdCacheSkipFlag) != 0;
            return (entry & GlyphIdCacheFoundFlag) != 0;
        }

        // Cache misses from the font as well as successful mappings. Repeated
        // missing characters can then go to fallback without another lookup.
        bool found = fontMetrics.TryGetGlyphId(codePoint, nextCodePoint, out glyphId, out skipNextCodePoint);
        this.glyphIdCacheEntries[slot] = key
            | ((ulong)glyphId << GlyphIdCacheGlyphShift)
            | (found ? GlyphIdCacheFoundFlag : 0)
            | (skipNextCodePoint ? GlyphIdCacheSkipFlag : 0);

        return found;
    }

    /// <summary>
    /// Looks up a table-derived shaping class through a direct-mapped cache in front
    /// of the font's class definition tables, whose walks bisect range records per
    /// query. A hit is one load and one masked compare. No synchronization is needed:
    /// a pooled buffer is exclusively owned for the duration of a shaping pass.
    /// </summary>
    /// <param name="fontMetrics">The font metrics the class belongs to.</param>
    /// <param name="glyphId">The glyph id to look up.</param>
    /// <param name="shapingClass">When this method returns, contains the cached class if found.</param>
    /// <returns><see langword="true"/> if a cached class was found.</returns>
    public bool TryGetShapingClass(FontMetrics fontMetrics, ushort glyphId, out GlyphShapingClass shapingClass)
    {
        if (!ReferenceEquals(this.shapingClassCacheOwner, fontMetrics))
        {
            Array.Clear(this.shapingClassCacheEntries);
            this.shapingClassCacheOwner = fontMetrics;
        }

        ulong entry = this.shapingClassCacheEntries[glyphId & 0xFF];
        if ((entry & ShapingClassCacheTagMask) == (ShapingClassCacheMarkerFlag | glyphId))
        {
            shapingClass = new((ushort)(entry >> ShapingClassCachePropsShift));
            return true;
        }

        shapingClass = default;
        return false;
    }

    /// <summary>
    /// Stores a table-derived shaping class in the direct-mapped class cache. Must
    /// only be called for classes computed purely from the font's class definition
    /// tables, after <see cref="TryGetShapingClass"/> has established the cache
    /// owner for the same font.
    /// </summary>
    /// <param name="glyphId">The glyph id the class was computed for.</param>
    /// <param name="shapingClass">The computed class.</param>
    public void SetShapingClass(ushort glyphId, GlyphShapingClass shapingClass)
        => this.shapingClassCacheEntries[glyphId & 0xFF] = ShapingClassCacheMarkerFlag
            | glyphId
            | ((ulong)shapingClass.Props << ShapingClassCachePropsShift);

    /// <summary>
    /// Gets a shaper for the given script and font, reusing a cached instance when
    /// one was built for the same key. Plans persist across passes while the options
    /// instance is unchanged, so steady-state shaping builds no plans and constructs
    /// no shapers. Plans whose resolution depends on live variation coordinates are
    /// rebuilt every call and never cached.
    /// </summary>
    /// <param name="script">The script class to shape.</param>
    /// <param name="unicodeScriptTag">The resolved OpenType script tag.</param>
    /// <param name="fontMetrics">The font metrics the plan binds to.</param>
    /// <param name="culture">The culture whose language system the plan selects.</param>
    /// <param name="featureTags">The effective additional feature tags.</param>
    /// <param name="useMetamorphosis">Whether the span uses AAT substitution.</param>
    /// <returns>The <see cref="ShapePlan"/>.</returns>
    public ShapePlan GetOrCreatePlan(ScriptClass script, Tag unicodeScriptTag, FontMetrics fontMetrics, CultureInfo culture, IReadOnlyList<Tag> featureTags, bool useMetamorphosis)
    {
        // The plan carries the language system the font's features were selected
        // through, so a plan built for one language cannot stand in for another.
        string language = culture.Name;

        // The itemizer gives adjacent records with equivalent effective features
        // one shared list instance. Reference comparison therefore keeps this hot
        // cache lookup allocation-free and avoids enumerating tags per segment.
        for (int i = 0; i < this.planCache.Count; i++)
        {
            (ScriptClass cachedScript, Tag cachedTag, FontMetrics cachedMetrics, string cachedLanguage, IReadOnlyList<Tag> cachedFeatures, bool cachedMetamorphosis, ShapePlan cachedPlan) = this.planCache[i];
            if (cachedScript == script
                && cachedTag == unicodeScriptTag
                && cachedMetamorphosis == useMetamorphosis
                && ReferenceEquals(cachedMetrics, fontMetrics)
                && string.Equals(cachedLanguage, language, StringComparison.Ordinal)
                && ReferenceEquals(cachedFeatures, featureTags))
            {
                return cachedPlan;
            }
        }

        Tag[] languageTags = ResolveLanguageTags(culture);
        ShapePlan plan = ShapePlan.Build(fontMetrics, script, unicodeScriptTag, this.TextOptions, featureTags, languageTags, useMetamorphosis);
        if (plan.IsCacheable)
        {
            // Variation-dependent plans opt out because their resolved lookups can
            // change without any managed cache-key value changing.
            this.planCache.Add((script, unicodeScriptTag, fontMetrics, language, featureTags, useMetamorphosis, plan));
        }

        return plan;
    }

    /// <summary>
    /// Resolves glyph metrics through a direct-mapped cache in front of the font's own
    /// resolver. The tag packs the same key fields the font's cache hashes, so a hit
    /// replaces a dictionary probe with one load and one compare. No synchronization is
    /// needed: a pooled buffer is exclusively owned for the duration of a shaping pass.
    /// </summary>
    /// <param name="fontMetrics">The font metrics to resolve against.</param>
    /// <param name="codePoint">The code point represented by the glyph.</param>
    /// <param name="glyphId">The glyph id.</param>
    /// <param name="textAttributes">The text attributes applied to the glyph.</param>
    /// <param name="textDecorations">The text decorations applied to the glyph.</param>
    /// <param name="layoutMode">The layout mode.</param>
    /// <param name="colorFontSupport">The color font support level.</param>
    /// <param name="palette">The color palette selection, or null for the font's default palette.</param>
    /// <returns>The resolved <see cref="FontGlyphMetrics"/>.</returns>
    private FontGlyphMetrics GetGlyphMetrics(
        FontMetrics fontMetrics,
        CodePoint codePoint,
        ushort glyphId,
        TextAttributes textAttributes,
        TextDecorations textDecorations,
        LayoutMode layoutMode,
        ColorFontSupport colorFontSupport,
        FontPalette? palette)
    {
        if (!ReferenceEquals(this.metricsCacheOwner, fontMetrics) || !ReferenceEquals(this.metricsCachePalette, palette))
        {
            Array.Clear(this.metricsCacheTags);
            this.metricsCacheOwner = fontMetrics;
            this.metricsCachePalette = palette;
        }

        bool isVertical = AdvancedTypographicUtils.IsVerticalGlyph(codePoint, layoutMode);
        ulong tag = (1UL << 63)
            | (uint)codePoint.Value
            | ((ulong)glyphId << 21)
            | ((ulong)(uint)textAttributes << 37)
            | ((ulong)(uint)colorFontSupport << 45)
            | ((isVertical ? 1UL : 0UL) << 49);

        int slot = glyphId & 0xFF;
        if (this.metricsCacheTags[slot] == tag)
        {
            return this.metricsCacheValues[slot]!;
        }

        FontGlyphMetrics glyphMetrics = fontMetrics.GetGlyphMetrics(codePoint, glyphId, textAttributes, textDecorations, layoutMode, colorFontSupport, palette);
        this.metricsCacheTags[slot] = tag;
        this.metricsCacheValues[slot] = glyphMetrics;
        return glyphMetrics;
    }

    /// <summary>
    /// Adds dx and dy to the positioned advance of the glyph at the given index and id.
    /// Advances accumulate in the position entry's shaping bounds so the shared
    /// metrics instance is never mutated.
    /// </summary>
    /// <param name="fontMetrics">The font face with metrics.</param>
    /// <param name="index">The zero-based index of the record.</param>
    /// <param name="glyphId">The id of the glyph to offset.</param>
    /// <param name="dx">The delta x-advance.</param>
    /// <param name="dy">The delta y-advance.</param>
    public void Advance(FontMetrics fontMetrics, int index, ushort glyphId, short dx, short dy)
        => AdvancedTypographicUtils.ApplyAdvance(fontMetrics, this, index, glyphId, dx, dy);

    /// <summary>
    /// Resolves the candidate OpenType language system tags for a culture. The
    /// invariant culture expresses no language preference.
    /// </summary>
    /// <param name="culture">The culture to resolve.</param>
    /// <returns>The candidate tags, most specific first.</returns>
    private static Tag[] ResolveLanguageTags(CultureInfo culture)
        => OpenTypeLanguageTagMap.TryGetTags(culture, out Tag[] tags) ? tags : [];

    /// <summary>
    /// Appends one record and returns an interior reference to it. The slot may hold a
    /// stale record from an earlier pass; callers overwrite it entirely. The metrics
    /// slot is grown in lockstep but left untouched.
    /// </summary>
    /// <returns>The appended record.</returns>
    private ref GlyphShapingData Append()
    {
        if (this.gapLength > 0)
        {
            this.InsertGlyphSlots(this.Count, 1);
        }
        else
        {
            this.EnsureCapacity(this.Count + 1);
        }

        return ref this.data[this.Count++];
    }

    /// <summary>
    /// Inserts one live glyph. Moves the metrics and positions during positioning.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert.</param>
    /// <param name="item">The record to insert.</param>
    private void InsertAt(int index, GlyphShapingData item)
    {
        this.InsertGlyphSlots(index, 1);
        if (this.Role == ShapingBufferRole.Positioning)
        {
            this.positions[index] = default;
        }

        this.data[index] = item;
        this.Count++;
    }

    /// <summary>
    /// Opens slots for live glyphs at the given index.
    /// The caller fills the slots and updates <see cref="Count"/>.
    /// </summary>
    /// <param name="index">The first new live index.</param>
    /// <param name="count">The number of glyphs to insert.</param>
    private void InsertGlyphSlots(int index, int count)
    {
        if (this.IsPassActive)
        {
            this.MoveGap(index);
            if (this.gapLength < count)
            {
                // Bring the array's spare capacity to the edit. Grow the
                // array only if it cannot hold the new live glyphs.
                int oldLength = this.gapLength;

                // The capacity check adds the old gap length to its argument.
                // Subtract that length here so the check counts it only once.
                this.EnsureCapacity(this.Count + count - oldLength);
                int length = this.data.Length - this.Count;
                Array.Copy(this.data, index + oldLength, this.data, index + length, this.Count - index);
                this.gapLength = length;
            }

            this.gapLength -= count;
            this.gapIndex += count;

            // The glyph array already has the new slots. This check gives
            // the metrics and positions enough space for the new live count.
            this.EnsureCapacity(this.Count + count);
        }
        else
        {
            this.EnsureCapacity(this.Count + count);
            Array.Copy(this.data, index, this.data, index + count, this.Count - index);
            if (this.Role == ShapingBufferRole.Positioning)
            {
                int positionedTailCount = this.SpanEnd - index;
                Array.Copy(this.metrics, index, this.metrics, index + count, positionedTailCount);
                Array.Copy(this.positions, index, this.positions, index + count, positionedTailCount);
            }
        }
    }

    /// <summary>
    /// Records the deletion and removes one live glyph.
    /// </summary>
    /// <param name="index">The zero-based index to remove at.</param>
    private void RemoveAt(int index) => this.RemoveRange(index, 1);

    /// <summary>
    /// Records a range of deleted glyphs and removes it from the live buffer.
    /// </summary>
    /// <param name="index">The zero-based index of the first record to remove.</param>
    /// <param name="count">The number of records to remove.</param>
    private void RemoveRange(int index, int count)
    {
        int end = index + count;
        if (this.IsPassActive)
        {
            this.MoveGap(index);
        }

        for (int i = index; i < end; i++)
        {
            this.DeletedGlyphs.Add((i, AdvancedTypographicUtils.NoDeletionOwner, this[i]));
        }

        if (this.IsPassActive)
        {
            // The removed records become unused slots. Live indices skip
            // them immediately. Later edits can reuse the same space.
            this.gapLength += count;
        }
        else
        {
            Array.Copy(this.data, end, this.data, index, this.Count - end);
            if (this.Role == ShapingBufferRole.Positioning)
            {
                int positionedTailCount = this.SpanEnd - end;
                Array.Copy(this.metrics, end, this.metrics, index, positionedTailCount);
                Array.Copy(this.positions, end, this.positions, index, positionedTailCount);
            }
        }

        this.Count -= count;
    }

    /// <summary>
    /// Deletes every record matching the filter in one forward compaction pass,
    /// keeping the parallel streams aligned. A deleted record's codepoint coverage
    /// folds into the preceding kept record, or into the next kept record when
    /// nothing precedes it, so the codepoint-to-glyph projection stays total.
    /// </summary>
    /// <param name="filter">The predicate selecting records to delete.</param>
    public void DeleteGlyphsInPlace(Func<GlyphShapingData, bool> filter)
    {
        bool positioning = this.Role == ShapingBufferRole.Positioning;
        int kept = 0;
        int pendingCodePointIndex = -1;
        int pendingStringIndex = -1;
        int pendingGraphemeIndex = -1;
        int pendingCodePointCount = 0;
        for (int i = 0; i < this.Count; i++)
        {
            if (filter(this.data[i]))
            {
                ref GlyphShapingData deleted = ref this.data[i];
                this.DeletedGlyphs.Add((i, AdvancedTypographicUtils.NoDeletionOwner, deleted));

                if (kept > 0 && deleted.Direction != TextDirection.RightToLeft)
                {
                    int previousCodePointIndex = this.data[kept - 1].CodePointIndex;
                    int previousStringIndex = this.data[kept - 1].StringIndex;
                    if (deleted.CodePointIndex < previousCodePointIndex)
                    {
                        // One input codepoint can produce several adjacent glyphs.
                        // Update every glyph carrying the old index so none of the
                        // surviving output disagrees about the combined input.
                        for (int j = kept - 1; j >= 0 && this.data[j].CodePointIndex == previousCodePointIndex; j--)
                        {
                            this.data[j].CodePointIndex = deleted.CodePointIndex;
                            this.data[j].GraphemeIndex = Math.Min(this.data[j].GraphemeIndex, deleted.GraphemeIndex);
                        }
                    }

                    if (deleted.StringIndex >= 0 && deleted.StringIndex < previousStringIndex)
                    {
                        // The char index travels with glyph records. Update every
                        // glyph representing the preceding input before compaction
                        // removes the record that supplied the earlier index.
                        for (int j = kept - 1; j >= 0 && this.data[j].StringIndex == previousStringIndex; j--)
                        {
                            this.data[j].StringIndex = deleted.StringIndex;
                        }
                    }

                    this.data[kept - 1].CodePointCount += deleted.CodePointCount;
                }
                else
                {
                    // The buffer remains logical until layout reorders it. For a
                    // right-to-left run, the next logical glyph is the preceding
                    // glyph visually and therefore receives the deleted input.
                    pendingCodePointIndex = pendingCodePointIndex < 0
                        ? deleted.CodePointIndex
                        : Math.Min(pendingCodePointIndex, deleted.CodePointIndex);
                    pendingStringIndex = pendingStringIndex < 0
                        ? deleted.StringIndex
                        : Math.Min(pendingStringIndex, deleted.StringIndex);
                    pendingGraphemeIndex = pendingGraphemeIndex < 0
                        ? deleted.GraphemeIndex
                        : Math.Min(pendingGraphemeIndex, deleted.GraphemeIndex);
                    pendingCodePointCount += deleted.CodePointCount;
                }

                continue;
            }

            if (pendingCodePointCount > 0)
            {
                int nextCodePointIndex = this.data[i].CodePointIndex;
                int nextStringIndex = this.data[i].StringIndex;
                int nextGraphemeIndex = this.data[i].GraphemeIndex;

                // The following input can already be represented by several glyphs.
                // Update every sibling before compaction so deleting a leading
                // record cannot leave part of that input with the later indices.
                for (int j = i; j < this.Count && this.data[j].CodePointIndex == nextCodePointIndex; j++)
                {
                    this.data[j].CodePointIndex = Math.Min(this.data[j].CodePointIndex, pendingCodePointIndex);
                }

                if (pendingStringIndex >= 0)
                {
                    for (int j = i; j < this.Count && this.data[j].StringIndex == nextStringIndex; j++)
                    {
                        this.data[j].StringIndex = Math.Min(this.data[j].StringIndex, pendingStringIndex);
                    }
                }

                for (int j = i; j < this.Count && this.data[j].GraphemeIndex == nextGraphemeIndex; j++)
                {
                    this.data[j].GraphemeIndex = Math.Min(this.data[j].GraphemeIndex, pendingGraphemeIndex);
                }

                this.data[i].CodePointCount += pendingCodePointCount;
                pendingCodePointCount = 0;
            }

            if (kept != i)
            {
                this.data[kept] = this.data[i];
                if (positioning)
                {
                    this.metrics[kept] = this.metrics[i];
                    this.positions[kept] = this.positions[i];
                }
            }

            kept++;
        }

        if (pendingCodePointCount > 0 && kept > 0)
        {
            int lastCodePointIndex = this.data[kept - 1].CodePointIndex;
            int lastStringIndex = this.data[kept - 1].StringIndex;
            for (int i = kept - 1; i >= 0 && this.data[i].CodePointIndex == lastCodePointIndex; i--)
            {
                this.data[i].CodePointIndex = Math.Min(this.data[i].CodePointIndex, pendingCodePointIndex);
                this.data[i].GraphemeIndex = Math.Min(this.data[i].GraphemeIndex, pendingGraphemeIndex);
            }

            if (pendingStringIndex >= 0)
            {
                for (int i = kept - 1; i >= 0 && this.data[i].StringIndex == lastStringIndex; i--)
                {
                    this.data[i].StringIndex = Math.Min(this.data[i].StringIndex, pendingStringIndex);
                }
            }

            this.data[kept - 1].CodePointCount += pendingCodePointCount;
        }

        this.Count = kept;
    }

    /// <summary>
    /// Starts a substitution pass at the given glyph. Edits during the pass
    /// can reuse spare slots in the glyph array.
    /// </summary>
    /// <param name="startIndex">The first glyph the lookup will read.</param>
    public void BeginOutputPass(int startIndex)
    {
        this.IsPassActive = true;
        this.ReadIndex = startIndex;
    }

    /// <summary>
    /// Packs the array and clears the cursor at the end of a substitution pass.
    /// </summary>
    public void EndOutputPass()
    {
        // Later stages can use contiguous glyph ranges again. A pass with
        // no unused slots needs no copy.
        if (this.gapLength > 0)
        {
            this.MoveGap(this.Count);
            this.gapLength = 0;
        }

        this.gapIndex = int.MaxValue;
        this.IsPassActive = false;
        this.ReadIndex = 0;
    }

    /// <summary>
    /// Moves the unused slots without changing any live indices.
    /// </summary>
    /// <param name="index">The live index that will follow the unused slots.</param>
    private void MoveGap(int index)
    {
        if (this.gapLength > 0)
        {
            // Move the glyphs between the two indices to the opposite side of
            // the gap. If edits move forward, these glyphs cross the gap once
            // between increases in capacity.
            if (index < this.gapIndex)
            {
                Array.Copy(this.data, index, this.data, index + this.gapLength, this.gapIndex - index);
            }
            else if (index > this.gapIndex)
            {
                Array.Copy(this.data, this.gapIndex + this.gapLength, this.data, this.gapIndex, index - this.gapIndex);
            }
        }

        this.gapIndex = index;
    }

    /// <summary>
    /// Advances the lookup cursor past one glyph.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyGlyph() => this.ReadIndex++;

    /// <summary>
    /// Moves the lookup cursor to the given glyph index.
    /// </summary>
    /// <param name="index">The glyph index to move to.</param>
    public void MoveTo(int index) => this.ReadIndex = this.IsPassActive ? Math.Min(index, this.SpanEnd) : index;

    /// <summary>
    /// Ensures space for the requested live count in all three arrays.
    /// The glyph array also needs space for the unused slots during substitution.
    /// </summary>
    /// <param name="required">The required record capacity.</param>
    private void EnsureCapacity(int required)
    {
        int glyphCapacity = required + this.gapLength;
        if (glyphCapacity > this.data.Length)
        {
            Array.Resize(ref this.data, Math.Max(this.data.Length * 2, glyphCapacity));
        }

        if (required > this.metrics.Length)
        {
            Array.Resize(ref this.metrics, Math.Max(this.metrics.Length * 2, required));
        }

        if (required > this.positions.Length)
        {
            Array.Resize(ref this.positions, Math.Max(this.positions.Length * 2, required));
        }
    }

    /// <summary>
    /// Places a dotted circle before the record at the given position. The
    /// circle copies the following record's run and direction so it travels
    /// with it, and stands for itself rather than continuing what it precedes.
    /// </summary>
    /// <param name="index">The position to insert before.</param>
    /// <param name="glyphId">The dotted circle's glyph id.</param>
    public void InsertDottedCircle(int index, ushort glyphId)
    {
        this.EnsureCapacity(this.Count + 1);
        Array.Copy(this.data, index, this.data, index + 1, this.Count - index);
        this.Count++;

        GlyphShapingData following = this.data[index + 1];
        this.data[index] = new GlyphShapingData(following, true)
        {
            GlyphId = glyphId,
            CodePoint = new CodePoint(DottedCircleCodePoint),
            CodePointCount = 1,
            LigatureComponent = -1,
        };

        this.glyphDigest.Add(glyphId);
    }

    /// <summary>
    /// Gets the contextual-match position slice for the current lookup depth.
    /// </summary>
    /// <remarks>
    /// Contextual subtables are entered from per-glyph lookup loops, so reserving
    /// the maximum match array on the stack in each call creates avoidable stack
    /// pressure. Depth partitioning preserves the parent match while a nested
    /// contextual lookup uses the next slice.
    /// </remarks>
    /// <returns>A reusable span large enough for the maximum shaping context.</returns>
    public Span<int> GetContextMatchPositions()
    {
        int stride = AdvancedTypographicUtils.MaxContextLength + 1;
        int offset = this.nestedApplicationDepth * stride;
        int required = offset + stride;
        if (this.contextMatchPositions.Length < required)
        {
            Array.Resize(ref this.contextMatchPositions, required);
        }

        return this.contextMatchPositions.AsSpan(offset, stride);
    }

    /// <summary>
    /// Enters a nested lookup application within a contextual match.
    /// </summary>
    public void PushNestedApplication() => this.nestedApplicationDepth++;

    /// <summary>
    /// Leaves a nested lookup application within a contextual match.
    /// </summary>
    public void PopNestedApplication() => this.nestedApplicationDepth--;

    /// <summary>
    /// One glyph's font and metrics, stored alongside the glyph record.
    /// </summary>
    public struct GlyphMetricsEntry
    {
        /// <summary>
        /// The font that resolved the glyph.
        /// </summary>
        public Font Font;

        /// <summary>
        /// The font glyph metrics.
        /// </summary>
        public FontGlyphMetrics Metrics;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphMetricsEntry"/> struct.
        /// </summary>
        /// <param name="font">The font that resolved the glyph.</param>
        /// <param name="metrics">The font glyph metrics.</param>
        public GlyphMetricsEntry(Font font, FontGlyphMetrics metrics)
        {
            this.Font = font;
            this.Metrics = metrics;
        }

        /// <summary>
        /// Gets the positioned horizontal advance in font design units for the paired
        /// entry: the shaping bounds value once positioning has written one, otherwise
        /// the metrics advance.
        /// </summary>
        /// <param name="position">The paired positioning entry.</param>
        /// <returns>The advance.</returns>
        public readonly ushort GetAdvanceWidth(in GlyphShapingPosition position)
            => position.Bounds.IsDirtyWH ? (ushort)position.Bounds.Width : this.Metrics.AdvanceWidth;

        /// <summary>
        /// Gets the positioned vertical advance in font design units for the paired
        /// entry: the shaping bounds value once positioning has written one, otherwise
        /// the metrics advance.
        /// </summary>
        /// <param name="position">The paired positioning entry.</param>
        /// <returns>The advance.</returns>
        public readonly ushort GetAdvanceHeight(in GlyphShapingPosition position)
            => position.Bounds.IsDirtyWH ? (ushort)position.Bounds.Height : this.Metrics.AdvanceHeight;
    }
}
