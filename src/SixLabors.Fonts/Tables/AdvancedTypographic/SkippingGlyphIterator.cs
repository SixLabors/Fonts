// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// An iterator over a glyph shaping buffer that respects OpenType lookup flags,
/// skipping glyphs that should be ignored (marks, base glyphs, ligatures) based on the flags.
/// </summary>
internal struct SkippingGlyphIterator
{
    private readonly FontMetrics fontMetrics;

    /// <summary>
    /// The three ignore lookup flags collapsed into a mask over the packed glyph class
    /// bits, so the common skip decision is a single bitwise test.
    /// </summary>
    private ushort ignoreClassMask;
    private ushort markAttachmentType;
    private bool useMarkFilteringSet;
    private ushort markFilteringSet;

    /// <summary>
    /// True when the current lookup flags cannot ignore any glyph, so stepping never
    /// needs to fetch or classify glyphs. Most lookups carry no ignore flags, which
    /// makes plain index arithmetic the common path.
    /// </summary>
    private bool skipsNothing;

    /// <summary>
    /// True when stepping is plain index arithmetic: the lookup flags cannot
    /// ignore any glyph and no default-ignorable transparency is active. Folded
    /// into one test so the per-step fast path stays a single branch.
    /// </summary>
    private bool stepsDirectly;

    /// <summary>
    /// True when this matcher reads the records the active pass has produced
    /// rather than the input it has yet to consume. Backtrack reads that side:
    /// the records behind the cursor were consumed by the pass, and a rule must
    /// see what earlier lookups produced rather than the input they replaced.
    /// </summary>
    private bool readsProducedSide;

    /// <summary>
    /// The <see cref="matchFlags"/> bit recording that default-ignorable
    /// transparency is active for the duration of sequence matching; plain
    /// stepping outside a match keeps its historical semantics.
    /// </summary>
    private const byte TransparencyActiveFlag = 1 << 0;

    /// <summary>
    /// The <see cref="matchFlags"/> bit recording that the zero width non-joiner
    /// is transparent rather than matchable.
    /// </summary>
    private const byte IgnoreZwnjFlag = 1 << 1;

    /// <summary>
    /// The <see cref="matchFlags"/> bit recording that the zero width joiner is
    /// transparent rather than matchable.
    /// </summary>
    private const byte IgnoreZwjFlag = 1 << 2;

    /// <summary>
    /// The <see cref="matchFlags"/> bit recording that the substitution-visible
    /// ignorables are transparent; set during positioning.
    /// </summary>
    private const byte IgnoreHiddenFlag = 1 << 3;

    /// <summary>
    /// The <see cref="matchFlags"/> bit recording that matching refuses records
    /// outside the latched syllable.
    /// </summary>
    private const byte SyllableGateFlag = 1 << 4;

    /// <summary>
    /// The <see cref="matchFlags"/> bit recording that the applying lookup
    /// matches per syllable, so stamping latches the syllable at the cursor.
    /// </summary>
    private const byte SyllableLatchFlag = 1 << 5;

    /// <summary>
    /// The mask matched records must carry a bit of; all bits during context
    /// matching, the applying lookup's mask otherwise.
    /// </summary>
    private uint matchMask;

    /// <summary>
    /// Packed matcher state addressed through the named flag constants above,
    /// keeping the struct narrow for the per-attempt copies matching makes.
    /// </summary>
    private byte matchFlags;

    /// <summary>
    /// The latched syllable serial compared under <see cref="SyllableGateFlag"/>.
    /// </summary>
    private byte syllableNumber;

    /// <summary>
    /// The latched syllable type compared under <see cref="SyllableGateFlag"/>.
    /// </summary>
    private byte syllableType;

    /// <summary>
    /// Initializes a new instance of the <see cref="SkippingGlyphIterator"/> struct.
    /// </summary>
    /// <param name="fontMetrics">The font metrics for glyph class lookups.</param>
    /// <param name="buffer">The glyph shaping buffer to iterate over.</param>
    /// <param name="index">The starting index in the buffer.</param>
    /// <param name="lookupFlags">The lookup flags that control which glyphs to skip.</param>
    /// <param name="markFilteringSet">The mark filtering set index, used when <see cref="LookupFlags.UseMarkFilteringSet"/> is set.</param>
    public SkippingGlyphIterator(
        FontMetrics fontMetrics,
        ShapingBuffer buffer,
        int index,
        LookupFlags lookupFlags,
        ushort markFilteringSet)
    {
        this.fontMetrics = fontMetrics;
        this.Collection = buffer;
        this.Index = index;
        this.ignoreClassMask = (ushort)(((lookupFlags & LookupFlags.IgnoreBaseGlyphs) != 0 ? GlyphShapingClass.BaseProp : 0)
            | ((lookupFlags & LookupFlags.IgnoreLigatures) != 0 ? GlyphShapingClass.LigatureProp : 0)
            | ((lookupFlags & LookupFlags.IgnoreMarks) != 0 ? GlyphShapingClass.MarkProp : 0));
        this.markAttachmentType = (ushort)((int)(lookupFlags & LookupFlags.MarkAttachmentTypeMask) >> 8);
        this.useMarkFilteringSet = (lookupFlags & LookupFlags.UseMarkFilteringSet) != 0;
        this.markFilteringSet = markFilteringSet;
        this.skipsNothing = this.ignoreClassMask == 0 && this.markAttachmentType == 0 && !this.useMarkFilteringSet;
        this.stepsDirectly = this.skipsNothing;
        this.matchMask = uint.MaxValue;
    }

    /// <summary>
    /// Gets the glyph shaping buffer being iterated.
    /// </summary>
    public ShapingBuffer Collection { get; }

    /// <summary>
    /// Gets or sets the current index in the buffer.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets a value indicating whether the stamped matcher can encounter a
    /// transparent record at all; matching keeps its solid-glyph walk when not.
    /// </summary>
    public readonly bool MatchTransparencyActive => (this.matchFlags & TransparencyActiveFlag) != 0;

    /// <summary>
    /// Gets the number of records on the side this matcher reads.
    /// </summary>
    public readonly int RecordCount
        => this.readsProducedSide ? this.Collection.PassOutputCount : this.Collection.Count;

    /// <summary>
    /// Gets a reference to the record at the given index on the side this
    /// matcher reads.
    /// </summary>
    /// <param name="index">The zero-based index on the read side.</param>
    /// <returns>A reference to the record.</returns>
    public readonly ref GlyphShapingData RecordAt(int index)
    {
        if (this.readsProducedSide)
        {
            return ref this.Collection.PassOutputAt(index);
        }

        return ref this.Collection[index];
    }

    /// <summary>
    /// Points this matcher at the side backtrack reads, stamps it as context,
    /// and steps to the first backtrack position. During a substitution pass
    /// that side is the records the pass has produced; outside one the records
    /// behind the cursor are the buffer itself.
    /// </summary>
    /// <returns>The first backtrack position, or a negative value when none remains.</returns>
    public int StartBacktrack()
    {
        ShapingBuffer buffer = this.Collection;
        if (buffer.IsPassActive)
        {
            this.readsProducedSide = true;
            this.Index = buffer.PassOutputCount;
        }

        this.SetMatchContext(0, true);

        // The matcher must test the immediately preceding record before deciding
        // whether it is transparent. Stepping here would discard a transparent
        // record that the backtrack sequence explicitly names.
        return --this.Index;
    }

    /// <summary>
    /// Advances to the next non-skipped glyph in the forward direction.
    /// </summary>
    /// <returns>The new index after advancing.</returns>
    public int Next()
    {
        this.Move(1);
        return this.Index;
    }

    /// <summary>
    /// Advances to the next non-skipped glyph in the backward direction.
    /// </summary>
    /// <returns>The new index after moving backward.</returns>
    public int Prev()
    {
        this.Move(-1);
        return this.Index;
    }

    /// <summary>
    /// Moves the iterator by the specified number of non-skipped glyphs. A negative count moves backward.
    /// </summary>
    /// <param name="count">The number of positions to move. Negative values move backward.</param>
    /// <returns>The new index after incrementing.</returns>
    public int Increment(int count = 1)
    {
        int direction = count < 0 ? -1 : 1;
        count = Math.Abs(count);
        while (count-- > 0)
        {
            this.Move(direction);
        }

        return this.Index;
    }

    /// <summary>
    /// Resets the iterator to a new index and lookup flags.
    /// </summary>
    /// <param name="index">The new starting index.</param>
    /// <param name="lookupFlags">The new lookup flags.</param>
    /// <param name="markFilteringSet">The new mark filtering set index.</param>
    public void Reset(int index, LookupFlags lookupFlags, ushort markFilteringSet)
    {
        this.Index = index;
        this.ignoreClassMask = (ushort)(((lookupFlags & LookupFlags.IgnoreBaseGlyphs) != 0 ? GlyphShapingClass.BaseProp : 0)
            | ((lookupFlags & LookupFlags.IgnoreLigatures) != 0 ? GlyphShapingClass.LigatureProp : 0)
            | ((lookupFlags & LookupFlags.IgnoreMarks) != 0 ? GlyphShapingClass.MarkProp : 0));
        this.markAttachmentType = (ushort)((int)(lookupFlags & LookupFlags.MarkAttachmentTypeMask) >> 8);
        this.useMarkFilteringSet = (lookupFlags & LookupFlags.UseMarkFilteringSet) != 0;
        this.markFilteringSet = markFilteringSet;
        this.skipsNothing = this.ignoreClassMask == 0 && this.markAttachmentType == 0 && !this.useMarkFilteringSet;
        this.stepsDirectly = this.skipsNothing;
        this.matchMask = uint.MaxValue;
        this.matchFlags = 0;
    }

    /// <summary>
    /// Moves the iterator one step in the given direction, skipping glyphs that should be ignored.
    /// </summary>
    /// <param name="direction">The direction to move: 1 for forward, -1 for backward.</param>
    private void Move(int direction)
    {
        this.Index += direction;

        // When the flags cannot ignore anything and no transparency is active,
        // ShouldIgnore is provably false for every glyph: the class mask test is
        // against zero and the mark branches are disabled. Skip the per-glyph
        // fetch and classification entirely.
        if (this.stepsDirectly)
        {
            return;
        }

        int limit = this.RecordCount;
        while (this.Index >= 0 && this.Index < limit)
        {
            // The class-mask test only runs when the flags can actually ignore
            // something; a skips-nothing iterator steps straight to transparency.
            if (this.skipsNothing || !this.ShouldIgnore(this.Index))
            {
                if ((this.matchFlags & TransparencyActiveFlag) == 0)
                {
                    break;
                }

                ref GlyphShapingData data = ref this.RecordAt(this.Index);
                if (!this.IsTransparent(ref data))
                {
                    break;
                }
            }

            this.Index += direction;
        }
    }

    /// <summary>
    /// Determines whether the glyph at the given index is ignored under the
    /// current lookup flags without moving the iterator. The pass driver tests
    /// each record with this before attempting a lookup, copying ignored records
    /// through to the output side untouched.
    /// </summary>
    /// <param name="index">The index of the glyph to check.</param>
    /// <returns><see langword="true"/> if the glyph is ignored; otherwise, <see langword="false"/>.</returns>
    public readonly bool IsIgnored(int index) => !this.skipsNothing && this.ShouldIgnore(index);

    /// <summary>
    /// Packs the matcher flags for the buffer's applying lookup, computed once
    /// when the lookup is stamped rather than on every match attempt: which
    /// joiner classes are transparent, whether transparency can apply at all,
    /// and whether stamping latches a syllable.
    /// </summary>
    /// <param name="buffer">The buffer carrying the applying lookup's state.</param>
    /// <param name="contextMatch">Whether the flags serve backtrack or lookahead matching.</param>
    /// <returns>The packed matcher flags.</returns>
    public static byte PackMatchFlags(ShapingBuffer buffer, bool contextMatch)
    {
        bool positioning = buffer.Role == ShapingBufferRole.Positioning;

        // A buffer holding no default ignorables cannot contain a transparent
        // record, so matching keeps its solid-glyph fast paths.
        byte flags = 0;
        if (buffer.HasDefaultIgnorables)
        {
            flags |= TransparencyActiveFlag;
        }

        if (positioning || (contextMatch && buffer.LookupAutoZwnj))
        {
            flags |= IgnoreZwnjFlag;
        }

        if (contextMatch || buffer.LookupAutoZwj)
        {
            flags |= IgnoreZwjFlag;
        }

        if (positioning)
        {
            flags |= IgnoreHiddenFlag;
        }

        if (!positioning && buffer.LookupPerSyllable)
        {
            flags |= SyllableLatchFlag;
        }

        return flags;
    }

    /// <summary>
    /// Activates default-ignorable transparency for sequence matching under the
    /// applying lookup's joiner handling: a default ignorable whose joiner bits
    /// the lookup treats as transparent is stepped over unless it matches the
    /// sequence position itself. Latches the syllable at the current index when
    /// the lookup matches per syllable. Copies of the iterator carry the stamped
    /// state; the caller's iterator is unaffected by helpers stamping their own
    /// copies.
    /// </summary>
    /// <param name="mask">The applying lookup's mask; ignored during context matching.</param>
    /// <param name="contextMatch">Whether this matcher walks backtrack or lookahead context.</param>
    public void SetMatchContext(uint mask, bool contextMatch)
    {
        ShapingBuffer buffer = this.Collection;
        byte flags = contextMatch ? buffer.ContextMatchFlags : buffer.InputMatchFlags;

        this.matchMask = contextMatch ? uint.MaxValue : mask;
        if ((flags & SyllableLatchFlag) != 0 && (uint)this.Index < (uint)buffer.Count)
        {
            SyllableInfo syllable = buffer[this.Index].Syllable;
            this.syllableNumber = (byte)syllable.Number;
            this.syllableType = (byte)syllable.Type;
            if (syllable.Type != SyllableType.None || syllable.Number != 0)
            {
                flags |= SyllableGateFlag;
            }
        }

        this.matchFlags = flags;
        this.stepsDirectly = this.skipsNothing && (flags & TransparencyActiveFlag) == 0;
    }

    /// <summary>
    /// Determines whether the record is transparent to the current matcher: a
    /// default ignorable that no lookup has substituted and whose joiner bits the
    /// matcher ignores. A substituted record is an ordinary glyph whatever
    /// codepoint it came from. Transparent records are stepped over during
    /// matching unless they match the sequence position themselves.
    /// </summary>
    /// <param name="data">The record to test.</param>
    /// <returns><see langword="true"/> when the record may be stepped over.</returns>
    public readonly bool IsTransparent(ref GlyphShapingData data)
        => (this.matchFlags & TransparencyActiveFlag) != 0
        && data.IsDefaultIgnorable
        && !data.IsSubstituted
        && ((this.matchFlags & IgnoreZwnjFlag) != 0 || !data.IsZwnj)
        && ((this.matchFlags & IgnoreZwjFlag) != 0 || !data.IsZwj)
        && ((this.matchFlags & IgnoreHiddenFlag) != 0 || !data.IsHiddenIgnorable);

    /// <summary>
    /// Determines whether the record passes the matcher's mask and syllable
    /// gates; the shape test itself runs only for records that do.
    /// </summary>
    /// <param name="data">The record to test.</param>
    /// <returns><see langword="true"/> when the record may be match-tested.</returns>
    public readonly bool MayMatch(ref GlyphShapingData data)
        => (data.FeatureMask & this.matchMask) != 0
        && ((this.matchFlags & SyllableGateFlag) == 0 || (data.Syllable.Number == this.syllableNumber && (byte)data.Syllable.Type == this.syllableType));

    /// <summary>
    /// Determines whether the glyph at the given index fails the lookup's glyph
    /// property check and is therefore always skipped, whatever the sequence
    /// expects.
    /// </summary>
    /// <param name="index">The index of the glyph to check.</param>
    /// <returns><see langword="true"/> when the glyph never participates.</returns>
    public readonly bool IsPropertySkipped(int index) => !this.skipsNothing && this.ShouldIgnore(index);

    /// <summary>
    /// Determines whether the glyph at the given index should be ignored based on the current lookup flags.
    /// </summary>
    /// <param name="index">The index of the glyph to check.</param>
    /// <returns><see langword="true"/> if the glyph should be skipped; otherwise, <see langword="false"/>.</returns>
    private readonly bool ShouldIgnore(int index)
    {
        ref GlyphShapingData data = ref this.RecordAt(index);

        // The shaping class is cached on the glyph keyed by glyph id; test the cache
        // inline so the common hit path avoids the classification call entirely.
        ushort props = data.ShapingClassCacheKey == data.GlyphId
            ? data.CachedShapingClass.Props
            : AdvancedTypographicUtils.GetGlyphShapingClass(this.fontMetrics, this.Collection, data.GlyphId, ref data).Props;

        if ((props & this.ignoreClassMask) != 0)
        {
            return true;
        }

        if ((props & GlyphShapingClass.MarkProp) != 0)
        {
            // Skip marks not in the lookup's MarkFilteringSet.
            // This requires GDEF MarkGlyphSetsDef support.
            if (this.useMarkFilteringSet && !AdvancedTypographicUtils.IsInMarkFilteringSet(this.fontMetrics, this.markFilteringSet, data.GlyphId))
            {
                return true;
            }

            // The high byte carries the mark attachment class; a lookup restricted to
            // one attachment class skips marks of any other.
            if (this.markAttachmentType > 0 && (props >> 8) != this.markAttachmentType)
            {
                return true;
            }
        }

        return false;
    }
}
