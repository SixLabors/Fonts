// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using System.Runtime.CompilerServices;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// The shaping pipeline's glyph buffer. Glyph state lives in one flat array of
/// <see cref="GlyphShapingData"/> records with a parallel metrics stream seeded after
/// substitution, both mutated in place through interior references. Storage doubles on
/// demand, is truncated rather than released on reset, and so stays at the workload's
/// high-water mark across pooled shaping passes.
/// </summary>
/// <remarks>
/// Mutation contract: write through the indexer expression
/// (<c>buffer[i].GlyphId = x</c>), which addresses storage directly. Binding an element
/// to a local without <see langword="ref"/> copies it and silently discards writes; a
/// convention test rejects such bindings. Interior references are invalidated by any
/// operation that inserts or removes glyphs.
/// </remarks>
internal sealed class ShapingBuffer
{
    /// <summary>
    /// The flat glyph storage. Only the first <see cref="Count"/> records are live;
    /// records beyond the count are stale leftovers awaiting overwrite.
    /// </summary>
    private GlyphShapingData[] data = new GlyphShapingData[64];

    /// <summary>
    /// The metrics stream, parallel to <see cref="data"/>. Entries are seeded by
    /// <see cref="TryAdd"/> and <see cref="TryUpdate"/>; substitution-phase buffers
    /// never populate it.
    /// </summary>
    private GlyphMetricsEntry[] metrics = new GlyphMetricsEntry[64];

    /// <summary>
    /// The positioning stream, parallel to <see cref="data"/>: shaping bounds,
    /// attachment links, and positioned and kerned marks. Seeded alongside the
    /// metrics stream; keeping this state out of the glyph record keeps the record
    /// narrow for the substitution walks that never touch it.
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
    private readonly List<(ScriptClass Script, Tag ScriptTag, FontMetrics FontMetrics, string Language, IReadOnlyList<Tag> FeatureTags, ShapePlan Plan)> planCache = new(4);

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
    /// The output-side record storage for substitution passes. Allocated on the
    /// first pass whose output grows past its read cursor and retained at the
    /// workload's high-water mark afterwards; passes whose output never outgrows
    /// the input keep writing into the primary storage and never touch this.
    /// </summary>
    private GlyphShapingData[] outData = [];

    /// <summary>
    /// Whether the active pass's output has diverged into <see cref="outData"/>.
    /// While false the output region aliases the head of the primary storage:
    /// equal-length passes write every record onto itself and copy nothing, and
    /// shrinking passes move records forward within the primary storage.
    /// Divergence occurs only when output would overtake the read cursor.
    /// </summary>
    private bool passDiverged;

    /// <summary>
    /// The depth of nested lookup application within contextual matches. Nested
    /// replacements mutate the input side in place regardless of their type.
    /// </summary>
    private int nestedApplicationDepth;

    /// <summary>
    /// Retained contextual-match positions, partitioned into one fixed-width slice
    /// per nested lookup depth so a child lookup cannot overwrite its parent's
    /// positions. The storage grows to the deepest level observed and is then reused.
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
    /// Gets the number of live glyph records. Substitution can leave this greater or
    /// smaller than the input codepoint count.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Gets the number of records already produced by the active pass; matching
    /// walks backtrack context against these records, not the unconsumed input.
    /// </summary>
    public int PassOutputCount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a substitution pass is active.
    /// </summary>
    public bool IsPassActive { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the applying lookup consumes
    /// records through the pass cursor. The substitution driver sets this for
    /// top-level lookups whose replacements consume exactly the records they
    /// match; contextual lookups leave it clear so their nested replacements
    /// mutate the input side in place and the driver alone advances the cursor.
    /// </summary>
    public bool DirectConsume { get; set; }

    /// <summary>
    /// Gets the read cursor of the active pass: the input-side position of the
    /// next record to consume.
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
    /// Gets a value indicating whether lookup application is currently nested
    /// inside a contextual match. Nested replacements never consume through the
    /// pass cursor, whatever their type: the outer contextual owns the cursor.
    /// </summary>
    public bool IsNestedApplication => this.nestedApplicationDepth > 0;

    /// <summary>
    /// Gets the number of records behind the pass position: those the pass has
    /// produced while a pass is active, and those before the cursor otherwise.
    /// Positions handed to <see cref="MoveTo"/> are measured against this.
    /// </summary>
    public int PassBacktrackLength => this.IsPassActive ? this.PassOutputCount : this.ReadIndex;

    /// <summary>
    /// Gets the number of records ahead of the pass position, still to be read.
    /// </summary>
    public int PassLookaheadLength => this.Count - this.ReadIndex;

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
    /// Gets the text runs covering the pass's input. Records store run indices into
    /// this list; the substitution and positioning buffers of a pass must share one
    /// list so the indices agree when records are seeded across buffers.
    /// </summary>
    public IReadOnlyList<TextRun> TextRuns { get; private set; } = Array.Empty<TextRun>();

    /// <summary>
    /// Gets the shaping segments recorded during substitution: each script segment's
    /// final range, script, and the plan that shaped it. The in-place positioning
    /// pass reuses these so one plan drives both tables; the list stays empty when
    /// records were seeded across buffers and positioning must segment for itself.
    /// </summary>
    public List<(int Index, int Count, ScriptClass Script, ShapePlan Plan)> SegmentPlans { get; } = [];

    /// <summary>
    /// Gets an interior reference to the glyph shaping data at the specified index.
    /// The reference writes through to the buffer's storage and is invalidated by any
    /// operation that inserts or removes glyphs.
    /// </summary>
    /// <param name="index">The zero-based index of the record to get.</param>
    /// <returns>The <see cref="GlyphShapingData"/>.</returns>
    public ref GlyphShapingData this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref this.data[index];
    }

    /// <summary>
    /// Gets an interior reference to the metrics entry at the specified index. Valid
    /// only after the buffer has been seeded by <see cref="TryAdd"/>.
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
    /// Assigns the text runs for the pass. Must run on both of a pass's buffers
    /// before any glyph is added, so record run indices resolve identically across
    /// them.
    /// </summary>
    /// <param name="textRuns">The resolved text runs covering the input.</param>
    public void SetTextRuns(IReadOnlyList<TextRun> textRuns) => this.TextRuns = textRuns;

    /// <summary>
    /// Changes the shaping phase this buffer serves. The single-run fast path shapes
    /// and seeds one buffer in place, flipping it from substitution to positioning
    /// instead of copying every record into a second buffer.
    /// </summary>
    /// <param name="role">The shaping phase the buffer serves next.</param>
    public void SetRole(ShapingBufferRole role) => this.Role = role;

    /// <summary>
    /// Seeds this buffer's metrics stream in place after substitution: fetches each
    /// glyph's metrics from <paramref name="font"/>, clears the record's feature
    /// registration for the positioning pass, and starts the shaping bounds from the
    /// single-axis advance. Behaviorally the in-place equivalent of
    /// <see cref="TryAdd"/> without the cross-buffer record copy; valid only when the
    /// buffer holds no placeholders.
    /// </summary>
    /// <param name="font">The font used to resolve metrics.</param>
    /// <returns>
    /// <see langword="true"/> when every mapped codepoint resolved a real glyph;
    /// <see langword="false"/> when fallback glyphs remain for a later font pass.
    /// </returns>
    public bool SeedMetricsInPlace(Font font)
    {
        bool hasFallBacks = false;
        FontMetrics fontMetrics = font.FontMetrics;
        LayoutMode layoutMode = this.TextOptions.LayoutMode;
        ColorFontSupport colorFontSupport = this.TextOptions.ColorFontSupport;
        FontPalette? fontPalette = this.TextOptions.FontPalette;

        uint verticalMask = ShapePlanFeatures.VerticalFeatureMask;

        for (int i = 0; i < this.Count; i++)
        {
            ref GlyphShapingData slot = ref this.data[i];
            CodePoint codePoint = slot.CodePoint;

            TextRun textRun = this.TextRuns[slot.TextRunIndex];
            TextAttributes textAttributes = textRun.TextAttributes;
            TextDecorations textDecorations = textRun.TextDecorations;

            bool isVertical = AdvancedTypographicUtils.IsVerticalGlyph(codePoint, layoutMode)
                || (slot.AppliedFeatureMask & verticalMask) != 0;

            FontGlyphMetrics glyphMetrics = this.GetGlyphMetrics(fontMetrics, codePoint, slot.GlyphId, textAttributes, textDecorations, layoutMode, textRun.ColorFontSupport ?? colorFontSupport, textRun.FontPalette ?? fontPalette);

            if (glyphMetrics.GlyphType == GlyphType.Fallback && !CodePoint.IsControl(codePoint))
            {
                hasFallBacks = true;
            }

            // Feature masks persist deliberately: the in-place positioning pass
            // reuses the substitution pass's plan, whose registrations already cover
            // the positioning features.
            this.positions[i] = new GlyphShapingPosition(isVertical
                ? new GlyphShapingBounds(0, 0, 0, glyphMetrics.AdvanceHeight)
                : new GlyphShapingBounds(0, 0, glyphMetrics.AdvanceWidth, 0));

            this.metrics[i] = new GlyphMetricsEntry(font, font.Size, glyphMetrics);
        }

        return !hasFallBacks;
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
        this.LigatureId = 1;
        this.randomState = 1;
        this.EnabledFeatureMaskUnion = ShapePlanFeatures.GlobalFeatureMask;
        this.glyphDigest = default;
        this.placeholderBidiRuns.Clear();
        this.SegmentPlans.Clear();

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
    /// Removes all glyph records while keeping the pass-wide state, so a fresh font run
    /// can populate the buffer without re-resolving options or language tags.
    /// </summary>
    public void Clear()
    {
        this.Count = 0;
        this.LigatureId = 1;
        this.EnabledFeatureMaskUnion = ShapePlanFeatures.GlobalFeatureMask;
        this.HasDefaultIgnorables = false;
        this.HasFractionSlash = false;
        this.HasVowelConstraintCandidates = false;
        this.placeholderBidiRuns.Clear();
        this.SegmentPlans.Clear();
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
        this.data[index].GlyphId = glyphId;
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
        slot = new GlyphShapingData(textRunIndex)
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
    /// Adds an atomic inline placeholder.
    /// </summary>
    /// <param name="codePoint">The object replacement codepoint used for Unicode processing.</param>
    /// <param name="bidiRun">The resolved bidi run for the placeholder.</param>
    /// <param name="textRunIndex">The index of the text run this placeholder belongs to.</param>
    /// <param name="codePointIndex">The zero-based index within the input codepoint buffer.</param>
    public void AddPlaceholder(CodePoint codePoint, BidiRun bidiRun, ushort textRunIndex, int codePointIndex)
    {
        ref GlyphShapingData slot = ref this.Append();
        slot = new GlyphShapingData(textRunIndex)
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
    /// Copies the placeholder bidi run recorded at the given codepoint index in
    /// <paramref name="source"/> into this buffer, so seeding from a workspace
    /// preserves placeholder bidi state without carrying it on every glyph record.
    /// </summary>
    /// <param name="source">The buffer to copy the recorded run from.</param>
    /// <param name="codePointIndex">The placeholder's zero-based codepoint index.</param>
    public void CopyPlaceholderBidiRun(ShapingBuffer source, int codePointIndex)
        => this.placeholderBidiRuns.Add((codePointIndex, source.GetPlaceholderBidiRun(codePointIndex)));

    /// <summary>
    /// Moves the specified glyph and its original input indices to the specified
    /// position.
    /// </summary>
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
        GlyphShapingData[] items = this.data;
        int sourceIndex = startIndex;
        for (int i = startIndex + 1; i < endIndex; i++)
        {
            if (items[i].StringIndex < items[sourceIndex].StringIndex)
            {
                sourceIndex = i;
            }
        }

        int sourceStringIndex = items[sourceIndex].StringIndex;
        int leadingStringIndex = items[startIndex].StringIndex;
        int trailingStringIndex = items[endIndex - 1].StringIndex;

        // HarfBuzz extends a changed boundary across adjacent records that already
        // share its old input start. This keeps every output copied from that input
        // together when only part of it intersects the requested shaping range.
        if (sourceStringIndex != trailingStringIndex)
        {
            while (endIndex < this.Count && items[endIndex].StringIndex == trailingStringIndex)
            {
                endIndex++;
            }
        }

        if (sourceStringIndex != leadingStringIndex)
        {
            while (startIndex > 0 && items[startIndex - 1].StringIndex == leadingStringIndex)
            {
                startIndex--;
            }
        }

        GlyphShapingData source = items[sourceIndex];
        for (int i = startIndex; i < endIndex; i++)
        {
            items[i].CodePointIndex = source.CodePointIndex;
            items[i].StringIndex = source.StringIndex;
            items[i].GraphemeIndex = source.GraphemeIndex;
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
        if (this.IsPassActive && this.DirectConsume && index == this.ReadIndex)
        {
            ref GlyphShapingData produced = ref this.ProduceFromCursor();
            produced.GlyphId = glyphId;
            produced.LigatureId = 0;
            produced.LigatureComponent = -1;
            produced.IsSubstituted = true;
            produced.AppliedFeatureMask |= ShapePlanFeatures.GetVerticalMask(feature);
            return;
        }

        ref GlyphShapingData current = ref this.data[index];
        current.GlyphId = glyphId;
        current.LigatureId = 0;
        current.LigatureComponent = -1;
        current.IsSubstituted = true;
        current.AppliedFeatureMask |= ShapePlanFeatures.GetVerticalMask(feature);
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
        ref GlyphShapingData current = ref this.data[index];
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
            ref GlyphShapingData consumed = ref this.data[removalIndices[i]];

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
        if (this.IsPassActive && this.DirectConsume && index == this.ReadIndex)
        {
            // Produce the ligature from the cursor, then stream the span it
            // matched over: component records are consumed without output and
            // everything between them, such as marks, is copied through.
            ref GlyphShapingData produced = ref this.ProduceFromCursor();
            if (codePoint != default)
            {
                produced.CodePoint = codePoint;
            }

            produced.CodePointCount += codePointCount;
            produced.GlyphId = glyphId;
            produced.LigatureId = ligatureId;
            produced.IsLigated = true;

            // Only the most recent ligature/multiple transformation controls
            // reordering decisions. Religation forgives an earlier expansion.
            produced.IsDecomposed = false;
            produced.LigatureComponent = ligatureComponent;
            produced.IsSubstituted = true;
            produced.AppliedFeatureMask |= ShapePlanFeatures.GetVerticalMask(feature);

            if (removalIndices.Length > 0)
            {
                int removal = 0;
                int last = removalIndices[^1];
                for (int position = index + 1; position <= last; position++)
                {
                    if (removal < removalIndices.Length && removalIndices[removal] == position)
                    {
                        this.SkipGlyph();
                        removal++;
                    }
                    else
                    {
                        this.CopyGlyph();
                    }
                }
            }

            return;
        }

        for (int i = removalIndices.Length - 1; i >= 0; i--)
        {
            this.RemoveAt(removalIndices[i]);
        }

        // Assign our new id at the index. The reference is taken after every removal
        // so it addresses the record's final slot.
        ref GlyphShapingData current = ref this.data[index];
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
            ref GlyphShapingData consumed = ref this.data[index + i];

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
        if (this.IsPassActive && this.DirectConsume && index == this.ReadIndex)
        {
            // Produce the replacement from the cursor, then consume the
            // contiguous following records without output.
            ref GlyphShapingData produced = ref this.ProduceFromCursor();
            if (codePoint != default)
            {
                produced.CodePoint = codePoint;
            }

            produced.CodePointCount += codePointCount;
            produced.GlyphId = glyphId;
            produced.LigatureId = 0;
            produced.LigatureComponent = -1;
            produced.IsSubstituted = true;
            produced.AppliedFeatureMask |= ShapePlanFeatures.GetVerticalMask(feature);

            for (int i = 0; i < count; i++)
            {
                this.SkipGlyph();
            }

            return;
        }

        // The consumed records are contiguous, so close their gap with one tail
        // move. Removing them individually would move the same tail once per record.
        this.RemoveRange(index + 1, count);

        // Assign our new id at the index. The reference is taken after every removal
        // so it addresses the record's final slot.
        ref GlyphShapingData current = ref this.data[index];
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

        GlyphShapingData merged = this.data[mergeIndex];
        int codePointCount = merged.CodePointCount;

        this.RemoveAt(mergeIndex);

        ref GlyphShapingData current = ref this.data[index];
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
        if (this.IsPassActive && this.DirectConsume && index == this.ReadIndex)
        {
            if (glyphIds.Length == 0)
            {
                // Spec disallows removal of glyphs in this manner but it's common enough practice to allow it.
                // https://github.com/MicrosoftDocs/typography-issues/issues/673
                this.SkipGlyph();
                return;
            }

            ref GlyphShapingData first = ref this.ProduceFromCursor();
            bool preservesLigatureAttachment = first.LigatureId > 0;
            first.GlyphId = glyphIds[0];
            if (!preservesLigatureAttachment)
            {
                // A free-standing expansion numbers its outputs as components.
                // When the input is already attached to a ligature, every output
                // must retain that existing attachment instead.
                first.LigatureComponent = 0;
            }

            first.IsSubstituted = true;
            first.IsDecomposed = true;
            this.glyphDigest.Add(glyphIds[0]);

            if (glyphIds.Length > 1)
            {
                // The produced record is captured by value as the template: the
                // appends below may diverge or grow the output storage, which
                // would invalidate a reference into it.
                GlyphShapingData template = first;
                uint mask = ShapePlanFeatures.GetVerticalMask(feature);
                for (int i = 1; i < glyphIds.Length; i++)
                {
                    // The expansion starts as an exact value copy. Assigning the new
                    // glyph through its property then invalidates only the class cache.
                    GlyphShapingData appended = template;
                    appended.GlyphId = glyphIds[i];

                    if (!preservesLigatureAttachment)
                    {
                        appended.LigatureComponent = i;
                    }

                    appended.AppliedFeatureMask |= mask;
                    this.glyphDigest.Add(glyphIds[i]);
                    this.AppendOutputGlyph(in appended);
                }
            }

            return;
        }

        if (glyphIds.Length > 0)
        {
            this.glyphDigest.Add(glyphIds[0]);
            bool preservesLigatureAttachment = this.data[index].LigatureId > 0;
            this.data[index].GlyphId = glyphIds[0];
            if (!preservesLigatureAttachment)
            {
                // Preserve an existing attachment across every output; only an
                // unattached expansion creates new component indices.
                this.data[index].LigatureComponent = 0;
            }

            this.data[index].IsSubstituted = true;
            this.data[index].IsDecomposed = true;

            // Add additional glyphs from the rest of the sequence. Insertion can grow
            // the storage, so the mutated record is captured by value rather than held
            // by reference.
            if (glyphIds.Length > 1)
            {
                GlyphShapingData template = this.data[index];
                uint mask = ShapePlanFeatures.GetVerticalMask(feature);
                int addedCount = glyphIds.Length - 1;
                int insertionIndex = index + 1;
                int tailCount = this.Count - insertionIndex;

                this.EnsureCapacity(this.Count + addedCount);

                // Open every output slot with one tail move. Repeated insertion would
                // move the same suffix once for each decomposed glyph.
                Array.Copy(this.data, insertionIndex, this.data, insertionIndex + addedCount, tailCount);
                if (this.Role == ShapingBufferRole.Positioning)
                {
                    Array.Copy(this.metrics, insertionIndex, this.metrics, insertionIndex + addedCount, tailCount);
                    Array.Copy(this.positions, insertionIndex, this.positions, insertionIndex + addedCount, tailCount);
                    Array.Clear(this.metrics, insertionIndex, addedCount);
                    Array.Clear(this.positions, insertionIndex, addedCount);
                }

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
        data.CodePointIndex = this.data[index].CodePointIndex;
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

        // Post-position insertion must shift and fill all three streams together;
        // leaving even one at its old index would pair a glyph with another glyph's
        // metrics or placement.
        Array.Copy(this.data, index, this.data, index + 1, this.Count - index);
        Array.Copy(this.metrics, index, this.metrics, index + 1, this.Count - index);
        Array.Copy(this.positions, index, this.positions, index + 1, this.Count - index);

        this.data[index] = data;
        this.metrics[index] = metricsEntry;
        this.positions[index] = position;
        this.Count++;
    }

    /// <summary>
    /// Seeds this buffer from a substituted workspace buffer: fetches each glyph's
    /// metrics from <paramref name="font"/>, seeds the record's shaping bounds with the
    /// single-axis advance so positioning starts from clean dirty-tracking, and appends
    /// record and metrics entry. Placeholders receive synthetic metrics and skip glyph
    /// lookup entirely.
    /// </summary>
    /// <param name="font">The font used to resolve metrics.</param>
    /// <param name="workspace">The substituted workspace buffer.</param>
    /// <returns>
    /// <see langword="true"/> when every mapped codepoint resolved a real glyph;
    /// <see langword="false"/> when fallback glyphs remain for a later font pass.
    /// </returns>
    public bool TryAdd(Font font, ShapingBuffer workspace)
    {
        bool hasFallBacks = false;
        FontMetrics fontMetrics = font.FontMetrics;
        LayoutMode layoutMode = this.TextOptions.LayoutMode;
        ColorFontSupport colorFontSupport = this.TextOptions.ColorFontSupport;
        FontPalette? fontPalette = this.TextOptions.FontPalette;

        // The hide-ignorables stage runs against this buffer, so the workspace's
        // knowledge of default ignorables must travel with its records.
        this.HasDefaultIgnorables |= workspace.HasDefaultIgnorables;
        this.HasFractionSlash |= workspace.HasFractionSlash;
        this.HasVowelConstraintCandidates |= workspace.HasVowelConstraintCandidates;

        uint verticalMask = ShapePlanFeatures.VerticalFeatureMask;

        for (int i = 0; i < workspace.Count; i++)
        {
            ref GlyphShapingData source = ref workspace.data[i];
            CodePoint codePoint = source.CodePoint;
            ushort id = source.GlyphId;

            if (source.IsPlaceholder)
            {
                // Placeholders are synthetic glyphs: they need layout metrics but must not
                // go through font glyph lookup, fallback resolution, or GPOS positioning.
                this.CopyPlaceholderBidiRun(workspace, source.CodePointIndex);
                FontGlyphMetrics placeholderMetrics = PlaceholderGlyphMetrics.Create(font, this.TextRuns[source.TextRunIndex], this.TextOptions.Dpi);

                this.glyphDigest.Add(placeholderMetrics.GlyphId);
                ref GlyphShapingData placeholderSlot = ref this.Append();
                placeholderSlot = source;
                placeholderSlot.ClearFeatures();
                this.positions[this.Count - 1] = new GlyphShapingPosition(layoutMode.IsVertical()
                    ? new GlyphShapingBounds(0, 0, 0, placeholderMetrics.AdvanceHeight)
                    : new GlyphShapingBounds(0, 0, placeholderMetrics.AdvanceWidth, 0))
                {
                    IsPositioned = true,
                };

                this.metrics[this.Count - 1] = new GlyphMetricsEntry(font, font.Size, placeholderMetrics);
                continue;
            }

            TextRun sourceRun = this.TextRuns[source.TextRunIndex];
            TextAttributes textAttributes = sourceRun.TextAttributes;
            TextDecorations textDecorations = sourceRun.TextDecorations;

            bool isVertical = AdvancedTypographicUtils.IsVerticalGlyph(codePoint, layoutMode)
                || (source.AppliedFeatureMask & verticalMask) != 0;

            FontGlyphMetrics glyphMetrics = this.GetGlyphMetrics(fontMetrics, codePoint, id, textAttributes, textDecorations, layoutMode, sourceRun.ColorFontSupport ?? colorFontSupport, sourceRun.FontPalette ?? fontPalette);

            if (glyphMetrics.GlyphType == GlyphType.Fallback && !CodePoint.IsControl(codePoint))
            {
                hasFallBacks = true;
            }

            // We only want a single dimensional advance for positioning; assigning a
            // fresh bounds value starts dirty tracking clean for GPOS.
            this.glyphDigest.Add(glyphMetrics.GlyphId);
            ref GlyphShapingData slot = ref this.Append();
            slot = source;
            slot.ClearFeatures();
            this.positions[this.Count - 1] = new GlyphShapingPosition(isVertical
                ? new GlyphShapingBounds(0, 0, 0, glyphMetrics.AdvanceHeight)
                : new GlyphShapingBounds(0, 0, glyphMetrics.AdvanceWidth, 0));

            this.metrics[this.Count - 1] = new GlyphMetricsEntry(font, font.Size, glyphMetrics);
        }

        return !hasFallBacks;
    }

    /// <summary>
    /// Replaces fallback glyphs in this buffer with glyphs shaped by a fallback font.
    /// Each surviving workspace codepoint index supersedes the source interval up to
    /// the next surviving index, including records consumed by substitution.
    /// </summary>
    /// <param name="font">The fallback font used to resolve metrics.</param>
    /// <param name="workspace">The substituted workspace buffer for the fallback font.</param>
    /// <returns>
    /// <see langword="true"/> when no fallback glyphs remain;
    /// <see langword="false"/> when further font passes are required.
    /// </returns>
    public bool TryUpdate(Font font, ShapingBuffer workspace)
    {
        // The fallback font supplies outlines and font tables, while layout mode and
        // color policy remain properties of the destination shaping operation.
        FontMetrics fontMetrics = font.FontMetrics;
        LayoutMode layoutMode = this.TextOptions.LayoutMode;
        ColorFontSupport colorFontSupport = this.TextOptions.ColorFontSupport;
        FontPalette? fontPalette = this.TextOptions.FontPalette;
        bool hasFallBacks = false;

        // The hide-ignorables stage runs against this buffer, so the workspace's
        // knowledge of default ignorables must travel with its records.
        this.HasDefaultIgnorables |= workspace.HasDefaultIgnorables;
        this.HasFractionSlash |= workspace.HasFractionSlash;
        this.HasVowelConstraintCandidates |= workspace.HasVowelConstraintCandidates;

        uint verticalMask = ShapePlanFeatures.VerticalFeatureMask;

        for (int i = 0; i < this.Count;)
        {
            if (this.metrics[i].Metrics.GlyphType != GlyphType.Fallback)
            {
                // A primary or earlier fallback font already resolved this record.
                // Later fallback passes must not replace a successful choice.
                i++;
                continue;
            }

            // Fallback fonts inherit the point size of the unresolved destination
            // record. Their Font instance identifies the face, but an explicit text
            // run can have a different size from the options' default font.
            int codePointIndex = this.data[i].CodePointIndex;
            float pointSize = this.metrics[i].PointSize;

            // Shaping preserves ascending source codepoint indices even when substitution
            // changes the number of glyphs. Locate the contiguous replacement group
            // directly in the workspace instead of allocating a temporary list.
            int replacementStart = 0;
            while (replacementStart < workspace.Count && workspace.data[replacementStart].CodePointIndex < codePointIndex)
            {
                replacementStart++;
            }

            int replacementEnd = replacementStart;
            while (replacementEnd < workspace.Count && workspace.data[replacementEnd].CodePointIndex == codePointIndex)
            {
                replacementEnd++;
            }

            if (replacementStart == replacementEnd)
            {
                // The fallback shaping result has no glyph at this source codepoint
                // index and no earlier result in this pass claimed it. Leave the
                // destination record available for the next configured fallback font.
                hasFallBacks = true;
                i++;
                continue;
            }

            // A substitution can consume later source records into the glyphs at
            // this codepoint index. The next surviving index marks the end of that
            // source interval; replacing only the first record would leave the
            // consumed primary-font .notdef records visible.
            int replacementLimit = replacementEnd < workspace.Count
                ? workspace.data[replacementEnd].CodePointIndex
                : int.MaxValue;

            // Validate the whole replacement group before mutating the destination.
            // Installing only the glyphs this font can draw would mix independently
            // shaped fragments and destroy the substitution result. Controls retain
            // the established exception because their fallback metrics are layout
            // placeholders rather than visible missing-glyph boxes.
            bool replacementsComplete = true;
            for (int j = replacementStart; j < replacementEnd; j++)
            {
                ref GlyphShapingData shape = ref workspace.data[j];
                TextRun shapeRun = this.TextRuns[shape.TextRunIndex];
                FontGlyphMetrics glyphMetrics = this.GetGlyphMetrics(
                    fontMetrics,
                    shape.CodePoint,
                    shape.GlyphId,
                    shapeRun.TextAttributes,
                    shapeRun.TextDecorations,
                    layoutMode,
                    shapeRun.ColorFontSupport ?? colorFontSupport,
                    shapeRun.FontPalette ?? fontPalette);

                if (glyphMetrics.GlyphType == GlyphType.Fallback && !CodePoint.IsControl(shape.CodePoint))
                {
                    replacementsComplete = false;
                    break;
                }
            }

            if (!replacementsComplete)
            {
                // Keep the original group intact so another fallback font can try the
                // same source interval as one shaping unit.
                hasFallBacks = true;
                i++;
                continue;
            }

            // Multiple glyphs can survive at the same source codepoint index. Walk back to
            // include every primary-font result at the replacement boundary, then
            // extend through indices consumed by the fallback substitution.
            int destinationStart = i;
            while (destinationStart > 0 && this.data[destinationStart - 1].CodePointIndex == codePointIndex)
            {
                destinationStart--;
            }

            int destinationEnd = destinationStart;
            while (destinationEnd < this.Count && this.data[destinationEnd].CodePointIndex < replacementLimit)
            {
                destinationEnd++;
            }

            // Remove backwards so each deletion cannot change the indices of records
            // still awaiting removal. RemoveAt keeps shaping data, metrics, and
            // positions aligned across their parallel arrays.
            for (int j = destinationEnd - 1; j >= destinationStart; j--)
            {
                this.RemoveAt(j);
            }

            int replacementCount = 0;
            for (int j = replacementStart; j < replacementEnd; j++)
            {
                GlyphShapingData shape = workspace.data[j];
                CodePoint codePoint = shape.CodePoint;
                TextRun shapeRun = this.TextRuns[shape.TextRunIndex];

                // The validation pass primed the direct-mapped metrics cache, so this
                // second lookup retrieves the value needed for insertion without a
                // second font-table or dictionary lookup.
                FontGlyphMetrics glyphMetrics = this.GetGlyphMetrics(
                    fontMetrics,
                    codePoint,
                    shape.GlyphId,
                    shapeRun.TextAttributes,
                    shapeRun.TextDecorations,
                    layoutMode,
                    shapeRun.ColorFontSupport ?? colorFontSupport,
                    shapeRun.FontPalette ?? fontPalette);

                bool isVertical = AdvancedTypographicUtils.IsVerticalGlyph(codePoint, layoutMode)
                    || (shape.AppliedFeatureMask & verticalMask) != 0;

                // Substitution masks belong to the temporary workspace plan. The
                // destination retains the substitution result but positioning starts
                // with clean feature state for the fallback font's positioning plan.
                shape.ClearFeatures();

                this.glyphDigest.Add(glyphMetrics.GlyphId);
                this.InsertAt(destinationStart + replacementCount, shape, new GlyphMetricsEntry(font, pointSize, glyphMetrics));

                // Positioning begins from the fallback glyph's natural advance on the
                // active layout axis. Offsets and cross-axis adjustments remain zero
                // until the fallback font's positioning tables run.
                this.positions[destinationStart + replacementCount] = new GlyphShapingPosition(isVertical
                    ? new GlyphShapingBounds(0, 0, 0, glyphMetrics.AdvanceHeight)
                    : new GlyphShapingBounds(0, 0, glyphMetrics.AdvanceWidth, 0));
                replacementCount++;
            }

            // Continue after the inserted group. Reexamining it would treat neither
            // its resolved metrics nor its workspace offsets as new fallback work.
            i = destinationStart + replacementCount;
        }

        return !hasFallBacks;
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
        if (!ReferenceEquals(this.glyphIdCacheOwner, fontMetrics))
        {
            Array.Clear(this.glyphIdCacheEntries);
            this.glyphIdCacheOwner = fontMetrics;
        }

        ulong tag = GlyphIdCacheMarkerFlag
            | (uint)codePoint.Value
            | ((ulong)(uint)((nextCodePoint?.Value + 1) ?? 0) << GlyphIdCacheNextShift);

        int slot = codePoint.Value & 0xFF;
        ulong entry = this.glyphIdCacheEntries[slot];
        if ((entry & GlyphIdCacheTagMask) == tag)
        {
            glyphId = (ushort)(entry >> GlyphIdCacheGlyphShift);
            skipNextCodePoint = (entry & GlyphIdCacheSkipFlag) != 0;
            return (entry & GlyphIdCacheFoundFlag) != 0;
        }

        bool found = fontMetrics.TryGetGlyphId(codePoint, nextCodePoint, out glyphId, out skipNextCodePoint);
        this.glyphIdCacheEntries[slot] = tag
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
            shapingClass = new GlyphShapingClass((ushort)(entry >> ShapingClassCachePropsShift));
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
    /// <returns>The <see cref="ShapePlan"/>.</returns>
    public ShapePlan GetOrCreatePlan(ScriptClass script, Tag unicodeScriptTag, FontMetrics fontMetrics, CultureInfo culture, IReadOnlyList<Tag> featureTags)
    {
        // The plan carries the language system the font's features were selected
        // through, so a plan built for one language cannot stand in for another.
        string language = culture.Name;

        // The itemizer gives adjacent records with equivalent effective features
        // one shared list instance. Reference comparison therefore keeps this hot
        // cache lookup allocation-free and avoids enumerating tags per segment.
        List<(ScriptClass Script, Tag ScriptTag, FontMetrics FontMetrics, string Language, IReadOnlyList<Tag> FeatureTags, ShapePlan Plan)> cache = this.planCache;
        for (int i = 0; i < cache.Count; i++)
        {
            (ScriptClass cachedScript, Tag cachedTag, FontMetrics cachedMetrics, string cachedLanguage, IReadOnlyList<Tag> cachedFeatures, ShapePlan cachedPlan) = cache[i];
            if (cachedScript == script
                && cachedTag == unicodeScriptTag
                && ReferenceEquals(cachedMetrics, fontMetrics)
                && string.Equals(cachedLanguage, language, StringComparison.Ordinal)
                && ReferenceEquals(cachedFeatures, featureTags))
            {
                return cachedPlan;
            }
        }

        Tag[] languageTags = ResolveLanguageTags(culture);
        ShapePlan plan = ShapePlan.Build(fontMetrics, script, unicodeScriptTag, this.TextOptions, featureTags, languageTags);
        if (plan.IsCacheable)
        {
            // Variation-dependent plans opt out because their resolved lookups can
            // change without any managed cache-key value changing.
            cache.Add((script, unicodeScriptTag, fontMetrics, language, featureTags, plan));
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
    /// Collects the code points of records still carrying fallback metrics, in buffer order
    /// and including repeats. Placeholders never resolve through fonts and controls keep
    /// their synthetic fallback metrics by design, so both are excluded.
    /// </summary>
    /// <param name="destination">The list receiving the unresolved code points.</param>
    public void CollectUnresolvedCodePoints(List<CodePoint> destination)
    {
        for (int i = 0; i < this.Count; i++)
        {
            ref GlyphShapingData slot = ref this.data[i];
            if (slot.IsPlaceholder || CodePoint.IsControl(slot.CodePoint))
            {
                continue;
            }

            if (this.metrics[i].Metrics.GlyphType == GlyphType.Fallback)
            {
                destination.Add(slot.CodePoint);
            }
        }
    }

    /// <summary>
    /// Marks the glyph at the specified index as positioned. Positions accumulate in
    /// the position entry's shaping bounds and are read from there by consumers, so
    /// the shared metrics instance is never mutated.
    /// </summary>
    /// <param name="index">The zero-based index of the record.</param>
    public void UpdatePosition(int index) => this.positions[index].IsPositioned = true;

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
    {
        FontGlyphMetrics m = this.metrics[index].Metrics;
        if (m.GlyphId != glyphId || fontMetrics != m.FontMetrics)
        {
            return;
        }

        bool isVertical = AdvancedTypographicUtils.IsVerticalGlyph(m.CodePoint, this.TextOptions.LayoutMode)
            || (this.data[index].AppliedFeatureMask & ShapePlanFeatures.VerticalFeatureMask) != 0;

        // Advance heights grow downward but font-space grows upward, hence the negation.
        this.positions[index].Bounds.Width += dx;
        if (isVertical)
        {
            this.positions[index].Bounds.Height -= dy;
        }
    }

    /// <summary>
    /// Returns a value indicating whether the record at the given index should be
    /// processed by the given font's positioning pass.
    /// </summary>
    /// <param name="fontMetrics">The font face with metrics.</param>
    /// <param name="index">The zero-based index of the record.</param>
    /// <returns><see langword="true"/> if the record should be processed.</returns>
    public bool ShouldProcess(FontMetrics fontMetrics, int index)
        => !this.positions[index].IsPositioned && this.metrics[index].Metrics.FontMetrics == fontMetrics;

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
        if (this.Count == this.data.Length)
        {
            Array.Resize(ref this.data, this.data.Length * 2);
            Array.Resize(ref this.metrics, this.metrics.Length * 2);
            Array.Resize(ref this.positions, this.positions.Length * 2);
        }

        return ref this.data[this.Count++];
    }

    /// <summary>
    /// Inserts one record at the given index, shifting later records right.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert.</param>
    /// <param name="item">The record to insert.</param>
    private void InsertAt(int index, GlyphShapingData item) => this.InsertAt(index, item, default);

    /// <summary>
    /// Inserts one record and its metrics entry at the given index, shifting later
    /// entries right. The metrics and positioning streams shift only on a positioning
    /// buffer: substitution-phase edits precede stream seeding, so their contents are
    /// undefined and copying them would be pure waste per edit.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert.</param>
    /// <param name="item">The record to insert.</param>
    /// <param name="metricsEntry">The metrics entry to insert.</param>
    private void InsertAt(int index, GlyphShapingData item, GlyphMetricsEntry metricsEntry)
    {
        if (this.Count == this.data.Length)
        {
            Array.Resize(ref this.data, this.data.Length * 2);
            Array.Resize(ref this.metrics, this.metrics.Length * 2);
            Array.Resize(ref this.positions, this.positions.Length * 2);
        }

        Array.Copy(this.data, index, this.data, index + 1, this.Count - index);
        if (this.Role == ShapingBufferRole.Positioning)
        {
            Array.Copy(this.metrics, index, this.metrics, index + 1, this.Count - index);
            Array.Copy(this.positions, index, this.positions, index + 1, this.Count - index);
            this.positions[index] = default;
        }

        this.data[index] = item;
        this.metrics[index] = metricsEntry;
        this.Count++;
    }

    /// <summary>
    /// Removes the record at the given index, shifting later entries left. The
    /// metrics and positioning streams shift only on a positioning buffer, matching
    /// the insertion contract. Stale entries beyond the count are overwritten by
    /// later appends.
    /// </summary>
    /// <param name="index">The zero-based index to remove at.</param>
    private void RemoveAt(int index)
    {
        Array.Copy(this.data, index + 1, this.data, index, this.Count - index - 1);
        if (this.Role == ShapingBufferRole.Positioning)
        {
            Array.Copy(this.metrics, index + 1, this.metrics, index, this.Count - index - 1);
            Array.Copy(this.positions, index + 1, this.positions, index, this.Count - index - 1);
        }

        this.Count--;
    }

    /// <summary>
    /// Removes a contiguous range of records, shifting the remaining tail once and
    /// keeping the parallel positioning streams aligned.
    /// </summary>
    /// <param name="index">The zero-based index of the first record to remove.</param>
    /// <param name="count">The number of records to remove.</param>
    private void RemoveRange(int index, int count)
    {
        int tailCount = this.Count - index - count;
        Array.Copy(this.data, index + count, this.data, index, tailCount);
        if (this.Role == ShapingBufferRole.Positioning)
        {
            Array.Copy(this.metrics, index + count, this.metrics, index, tailCount);
            Array.Copy(this.positions, index + count, this.positions, index, tailCount);
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
    /// Gets a reference to a record on the output side of the active pass.
    /// </summary>
    /// <param name="index">The zero-based output-side index.</param>
    /// <returns>A reference to the record.</returns>
    public ref GlyphShapingData PassOutputAt(int index)
    {
        if (this.passDiverged)
        {
            return ref this.outData[index];
        }

        return ref this.data[index];
    }

    /// <summary>
    /// Begins a substitution pass with both cursors at the given position: the
    /// records before it are untouched by the pass by construction, so the
    /// aliased output region simply adopts them. Output aliases the primary
    /// storage until a write would overtake unread input.
    /// </summary>
    /// <param name="startIndex">The position at which the pass begins.</param>
    public void BeginOutputPass(int startIndex)
    {
        this.IsPassActive = true;
        this.passDiverged = false;
        this.PassOutputCount = startIndex;
        this.ReadIndex = startIndex;
    }

    /// <summary>
    /// Ends the active pass: unconsumed input records stream to the output side,
    /// the output becomes the buffer content, and in-place semantics resume. A
    /// pass that is still aliased and level has changed nothing structural, so
    /// the tail is already in place and the pass closes with cursor resets alone.
    /// </summary>
    public void EndOutputPass()
    {
        if (!this.passDiverged && this.PassOutputCount == this.ReadIndex)
        {
            this.IsPassActive = false;
            this.ReadIndex = 0;
            this.PassOutputCount = 0;
            return;
        }

        this.CopyGlyphs(this.Count - this.ReadIndex);

        if (this.passDiverged)
        {
            (this.data, this.outData) = (this.outData, this.data);
        }

        this.Count = this.PassOutputCount;
        this.EnsureCapacity(this.Count);
        this.IsPassActive = false;
        this.passDiverged = false;
        this.ReadIndex = 0;
        this.PassOutputCount = 0;
    }

    /// <summary>
    /// Copies the record at the read cursor to the output side and advances both
    /// cursors. While the sides are aliased and level this is two cursor
    /// increments and nothing else; the copying forms live in the cold method so
    /// the overwhelmingly common no-op inlines into the pass walk.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyGlyph()
    {
        if (!this.passDiverged && this.PassOutputCount == this.ReadIndex)
        {
            this.PassOutputCount++;
            this.ReadIndex++;
            return;
        }

        this.CopyGlyphMoved();
    }

    /// <summary>
    /// Copies the given number of consecutive records from the read cursor to the
    /// output side and advances both cursors. An aligned pass adopts the range by
    /// moving its cursors; a shifted pass moves the range as one block.
    /// </summary>
    /// <param name="count">The number of records to copy.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyGlyphs(int count)
    {
        if (!this.passDiverged && this.PassOutputCount == this.ReadIndex)
        {
            this.PassOutputCount += count;
            this.ReadIndex += count;
            return;
        }

        this.CopyGlyphsMoved(count);
    }

    /// <summary>
    /// Copies the record at the read cursor to the output side when the sides
    /// have shifted or diverged.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void CopyGlyphMoved()
    {
        if (this.passDiverged)
        {
            this.EnsureOutCapacity(this.PassOutputCount + 1);
            this.outData[this.PassOutputCount] = this.data[this.ReadIndex];
        }
        else
        {
            this.data[this.PassOutputCount] = this.data[this.ReadIndex];
        }

        this.PassOutputCount++;
        this.ReadIndex++;
    }

    /// <summary>
    /// Copies consecutive records at the read cursor when the pass sides have
    /// shifted or diverged.
    /// </summary>
    /// <param name="count">The number of records to copy.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void CopyGlyphsMoved(int count)
    {
        if (this.passDiverged)
        {
            this.EnsureOutCapacity(this.PassOutputCount + count);
            Array.Copy(this.data, this.ReadIndex, this.outData, this.PassOutputCount, count);
        }
        else
        {
            Array.Copy(this.data, this.ReadIndex, this.data, this.PassOutputCount, count);
        }

        this.PassOutputCount += count;
        this.ReadIndex += count;
    }

    /// <summary>
    /// Consumes the record at the read cursor without producing output, deleting
    /// it from the pass result. The sides stay aliased: output only ever trails
    /// the cursor after a deletion.
    /// </summary>
    public void SkipGlyph() => this.ReadIndex++;

    /// <summary>
    /// Moves the pass position so that the given number of records sit on the
    /// output side. Advancing streams records forward; rewinding returns produced
    /// records to the input side ahead of the read cursor, exactly reversing the
    /// stream.
    /// </summary>
    /// <param name="outputPosition">The output-side record count to move to.</param>
    public void MoveTo(int outputPosition)
    {
        if (this.PassOutputCount < outputPosition && this.ReadIndex < this.Count)
        {
            int count = Math.Min(outputPosition - this.PassOutputCount, this.Count - this.ReadIndex);
            this.CopyGlyphs(count);
        }

        if (outputPosition < this.PassOutputCount)
        {
            int rewound = this.PassOutputCount - outputPosition;

            // Produced records return to the input side ahead of the read
            // cursor, which needs that many free slots behind it. A pass that
            // produced more than it consumed has fewer, so the unread input
            // moves up to open them.
            if (rewound > this.ReadIndex)
            {
                this.ShiftInputForward(rewound - this.ReadIndex);
            }

            this.ReadIndex -= rewound;
            if (this.passDiverged)
            {
                Array.Copy(this.outData, outputPosition, this.data, this.ReadIndex, rewound);
            }
            else
            {
                Array.Copy(this.data, outputPosition, this.data, this.ReadIndex, rewound);
            }

            this.PassOutputCount = outputPosition;
        }
    }

    /// <summary>
    /// Moves the unread input up by the given number of slots, opening room
    /// between the produced records and the read cursor. The records that fill
    /// them come back from the produced side, so the buffer's total is
    /// unchanged even though its input length grows.
    /// </summary>
    /// <param name="count">The number of slots to open.</param>
    private void ShiftInputForward(int count)
    {
        int unread = this.Count - this.ReadIndex;
        this.EnsureCapacity(this.Count + count);
        if (unread > 0)
        {
            Array.Copy(this.data, this.ReadIndex, this.data, this.ReadIndex + count, unread);
        }

        this.ReadIndex += count;
        this.Count += count;
    }

    /// <summary>
    /// Grows the record storage and the streams parallel to it. The three stay
    /// the same length: every record addresses its metrics and its position by
    /// its own index.
    /// </summary>
    /// <param name="required">The required record capacity.</param>
    private void EnsureCapacity(int required)
    {
        if (required > this.data.Length)
        {
            Array.Resize(ref this.data, Math.Max(this.data.Length * 2, required));
        }

        // The parallel streams are sized independently because ending a pass
        // swaps the record storage with the output storage, which grew on its
        // own; after such a swap the records can outnumber the slots that were
        // allocated alongside them.
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
    /// Consumes the record at the read cursor onto the output side and returns a
    /// reference to the produced record for mutation. While the sides are aliased
    /// and level the record is produced onto itself.
    /// </summary>
    /// <returns>A reference to the produced record.</returns>
    private ref GlyphShapingData ProduceFromCursor()
    {
        if (this.passDiverged)
        {
            this.EnsureOutCapacity(this.PassOutputCount + 1);
            this.outData[this.PassOutputCount] = this.data[this.ReadIndex];
            this.ReadIndex++;
            return ref this.outData[this.PassOutputCount++];
        }

        if (this.PassOutputCount != this.ReadIndex)
        {
            this.data[this.PassOutputCount] = this.data[this.ReadIndex];
        }

        this.ReadIndex++;
        return ref this.data[this.PassOutputCount++];
    }

    /// <summary>
    /// Appends a record to the output side without consuming input, diverging
    /// first if the write would otherwise overtake unread input.
    /// </summary>
    /// <param name="record">The record to append.</param>
    private void AppendOutputGlyph(in GlyphShapingData record)
    {
        if (!this.passDiverged && this.PassOutputCount >= this.ReadIndex)
        {
            this.Diverge();
        }

        if (this.passDiverged)
        {
            this.EnsureOutCapacity(this.PassOutputCount + 1);
            this.outData[this.PassOutputCount++] = record;
        }
        else
        {
            this.data[this.PassOutputCount++] = record;
        }
    }

    /// <summary>
    /// Diverges the active pass into the output storage: everything produced so
    /// far is copied out of the primary storage, and later writes stream there.
    /// Called only when output would otherwise overtake unread input.
    /// </summary>
    private void Diverge()
    {
        if (this.outData.Length < this.data.Length)
        {
            this.outData = new GlyphShapingData[this.data.Length];
        }

        Array.Copy(this.data, this.outData, this.PassOutputCount);
        this.passDiverged = true;
    }

    /// <summary>
    /// Grows the output storage to hold at least the required record count.
    /// </summary>
    /// <param name="required">The required record capacity.</param>
    private void EnsureOutCapacity(int required)
    {
        if (required > this.outData.Length)
        {
            int length = Math.Max(this.outData.Length * 2, required);
            Array.Resize(ref this.outData, length);
        }
    }

    /// <summary>
    /// One glyph's metrics-phase state: the resolving font, its point size, and the
    /// resolved metrics instance. Stored in a stream parallel to the glyph records.
    /// </summary>
    public struct GlyphMetricsEntry
    {
        /// <summary>
        /// The font that resolved the glyph.
        /// </summary>
        public Font Font;

        /// <summary>
        /// The font size in PT units of the font containing this glyph.
        /// </summary>
        public float PointSize;

        /// <summary>
        /// The font glyph metrics.
        /// </summary>
        public FontGlyphMetrics Metrics;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphMetricsEntry"/> struct.
        /// </summary>
        /// <param name="font">The font that resolved the glyph.</param>
        /// <param name="pointSize">The font size in PT units.</param>
        /// <param name="metrics">The font glyph metrics.</param>
        public GlyphMetricsEntry(Font font, float pointSize, FontGlyphMetrics metrics)
        {
            this.Font = font;
            this.PointSize = pointSize;
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
