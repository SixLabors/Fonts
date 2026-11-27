// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Contains supplementary data that allows the shaping of glyphs. Stored by value in
/// the shaping buffer's flat storage; call sites mutate through the buffer indexer so
/// writes land in place.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal struct GlyphShapingData
{
    /// <summary>
    /// The syllable classification assigned by the complex-script shapers, stored by
    /// value so classification never allocates. A <see cref="SyllableInfo.Type"/> of
    /// <see cref="SyllableType.None"/> means no classification has been assigned.
    /// </summary>
    public SyllableInfo Syllable;

    /// <summary>
    /// The <see cref="flags"/> bit recording <see cref="IsLigated"/>.
    /// </summary>
    private const ushort LigatedFlag = 1 << 0;

    /// <summary>
    /// The <see cref="flags"/> bit recording <see cref="IsSubstituted"/>.
    /// </summary>
    private const ushort SubstitutedFlag = 1 << 1;

    /// <summary>
    /// The <see cref="flags"/> bit recording <see cref="IsDecomposed"/>.
    /// </summary>
    private const ushort DecomposedFlag = 1 << 2;

    /// <summary>
    /// The <see cref="flags"/> bit recording <see cref="IsPlaceholder"/>.
    /// </summary>
    private const ushort PlaceholderFlag = 1 << 3;

    /// <summary>
    /// The <see cref="flags"/> bit recording <see cref="IsDefaultIgnorable"/>.
    /// </summary>
    private const ushort DefaultIgnorableFlag = 1 << 4;

    /// <summary>
    /// The <see cref="flags"/> bit recording <see cref="IsHidden"/>.
    /// </summary>
    private const ushort HiddenFlag = 1 << 5;

    /// <summary>
    /// The <see cref="flags"/> bit recording <see cref="IsZwnj"/>.
    /// </summary>
    private const ushort ZwnjFlag = 1 << 7;

    /// <summary>
    /// The <see cref="flags"/> bit recording <see cref="IsZwj"/>.
    /// </summary>
    private const ushort ZwjFlag = 1 << 8;

    /// <summary>
    /// The <see cref="flags"/> bit recording <see cref="IsHiddenIgnorable"/>.
    /// </summary>
    private const ushort HiddenIgnorableFlag = 1 << 9;

    /// <summary>
    /// The <see cref="flags"/> bit recording a mark-order override of 22.
    /// </summary>
    private const ushort MarkOrder22Flag = 1 << 10;

    /// <summary>
    /// The <see cref="flags"/> bit recording a mark-order override of 26.
    /// </summary>
    private const ushort MarkOrder26Flag = 1 << 11;

    /// <summary>
    /// The <see cref="flags"/> bit recording that this is a fixed stretch tile.
    /// </summary>
    private const ushort FixedStretchFlag = 1 << 12;

    /// <summary>
    /// The <see cref="flags"/> bit recording that this is a repeating stretch tile.
    /// </summary>
    private const ushort RepeatingStretchFlag = 1 << 13;

    /// <summary>
    /// The <see cref="flags"/> bit recording that tracking must preserve this
    /// glyph's cursive shaping run.
    /// </summary>
    private const ushort CursiveScriptFlag = 1 << 14;

    /// <summary>
    /// The <see cref="flags"/> bits reserved for the script-specific mark-order override.
    /// </summary>
    private const ushort MarkOrderFlags = MarkOrder22Flag | MarkOrder26Flag;

    /// <summary>
    /// The <see cref="flags"/> bit recording that <see cref="shapingClassCacheId"/>
    /// holds the glyph id <see cref="CachedShapingClass"/> was computed for. A default
    /// record therefore reports an invalid cache.
    /// </summary>
    private const ushort ShapingClassCacheValidFlag = 1 << 6;

    /// <summary>
    /// The modulus folding ligature ids into the three-bit range 1..7. Zero remains
    /// the not-a-ligature value, so allocation skips it when the serial wraps.
    /// </summary>
    private const int LigatureIdModulus = 7;

    private ushort glyphId;

    /// <summary>
    /// The three-bit ligature id, or zero when the glyph is not a ligature.
    /// </summary>
    private byte ligatureId;

    /// <summary>
    /// The ligature component index stored as component + 1, so the default record
    /// encodes the -1 no-component sentinel as zero.
    /// </summary>
    private byte ligatureComponentPlusOne;

    /// <summary>
    /// The number of matched components represented by a ligature. Zero stores the
    /// common single-component value without requiring initialization.
    /// </summary>
    private byte ligatureComponentCount;

    /// <summary>
    /// Packed boolean shaping state addressed through the named flag constants above.
    /// Single bits keep the record narrow; the properties are the only readers and
    /// writers.
    /// </summary>
    private ushort flags;

    /// <summary>
    /// The glyph id the cached shaping class was computed for; meaningful only while
    /// the cache-valid flag bit is set.
    /// </summary>
    private ushort shapingClassCacheId;

    /// <summary>
    /// The text direction, stored as a byte to keep the record narrow.
    /// </summary>
    private byte direction;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphShapingData"/> struct. The
    /// feature masks are seeded with the shared global bit: global features apply to
    /// every glyph by definition, so their registration never walks glyph ranges and
    /// every record is born carrying the bit their lookups gate on.
    /// </summary>
    /// <param name="textRunIndex">The index of the text run this glyph belongs to.</param>
    public GlyphShapingData(ushort textRunIndex)
    {
        this.TextRunIndex = textRunIndex;
        this.RegisteredFeatureMask = ShapePlanFeatures.GlobalFeatureMask;
        this.FeatureMask = ShapePlanFeatures.GlobalFeatureMask;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphShapingData"/> struct.
    /// </summary>
    /// <param name="data">The data to copy properties from.</param>
    /// <param name="clearFeatures">Whether to clear features.</param>
    public GlyphShapingData(GlyphShapingData data, bool clearFeatures)
    {
        this.GlyphId = data.GlyphId;
        this.CodePointIndex = data.CodePointIndex;
        this.StringIndex = data.StringIndex;
        this.GraphemeIndex = data.GraphemeIndex;
        this.CodePoint = data.CodePoint;
        this.CodePointCount = data.CodePointCount;
        this.Direction = data.Direction;
        this.TextRunIndex = data.TextRunIndex;
        this.FallbackFontIndex = data.FallbackFontIndex;
        this.LigatureId = data.LigatureId;
        this.IsLigated = data.IsLigated;
        this.LigatureComponent = data.LigatureComponent;
        this.LigatureComponentCount = data.LigatureComponentCount;
        this.IsSubstituted = data.IsSubstituted;
        this.IsDecomposed = data.IsDecomposed;
        this.IsPlaceholder = data.IsPlaceholder;
        this.IsDefaultIgnorable = data.IsDefaultIgnorable;
        this.IsHidden = data.IsHidden;
        this.IsZwnj = data.IsZwnj;
        this.IsZwj = data.IsZwj;
        this.IsHiddenIgnorable = data.IsHiddenIgnorable;
        this.MarkOrderOverride = data.MarkOrderOverride;
        this.IsFixedStretch = data.IsFixedStretch;
        this.IsRepeatingStretch = data.IsRepeatingStretch;
        this.IsCursiveScript = data.IsCursiveScript;

        this.Syllable = data.Syllable;

        if (!clearFeatures)
        {
            this.RegisteredFeatureMask = data.RegisteredFeatureMask;
            this.FeatureMask = data.FeatureMask;
        }
        else
        {
            this.RegisteredFeatureMask = ShapePlanFeatures.GlobalFeatureMask;
            this.FeatureMask = ShapePlanFeatures.GlobalFeatureMask;
        }

        this.AppliedFeatureMask = data.AppliedFeatureMask;

        this.CachedShapingClass = data.CachedShapingClass;
        this.ShapingClassCacheKey = data.ShapingClassCacheKey;
    }

    /// <summary>
    /// Gets or sets the glyph id. Setting this value invalidates the cached shaping class.
    /// </summary>
    public ushort GlyphId
    {
        readonly get => this.glyphId;
        set
        {
            if (this.glyphId != value)
            {
                this.glyphId = value;
                this.ShapingClassCacheKey = -1;
            }
        }
    }

    /// <summary>
    /// Gets or sets the cached glyph shaping class, avoiding repeated GDEF lookups.
    /// </summary>
    public GlyphShapingClass CachedShapingClass { get; set; }

    /// <summary>
    /// Gets or sets the cache key for <see cref="CachedShapingClass"/>.
    /// A value of <c>-1</c> indicates the cache is invalid. Valid entries store the glyph id.
    /// </summary>
    public int ShapingClassCacheKey
    {
        readonly get => (this.flags & ShapingClassCacheValidFlag) != 0 ? this.shapingClassCacheId : -1;
        set
        {
            if (value < 0)
            {
                this.flags = (ushort)(this.flags & ~ShapingClassCacheValidFlag);
            }
            else
            {
                this.shapingClassCacheId = (ushort)value;
                this.flags = (ushort)(this.flags | ShapingClassCacheValidFlag);
            }
        }
    }

    /// <summary>
    /// Gets or sets the zero-based index within the input codepoint collection of the
    /// leading codepoint this glyph represents.
    /// </summary>
    public int CodePointIndex { get; set; }

    /// <summary>
    /// Gets or sets the zero-based char index in the original text represented by
    /// this glyph.
    /// </summary>
    public int StringIndex { get; set; }

    /// <summary>
    /// Gets or sets the zero-based index of the grapheme this glyph belongs to. The
    /// text is walked grapheme by grapheme as the buffer is filled, so the grouping is
    /// recorded there rather than worked out again from the characters later.
    /// </summary>
    public int GraphemeIndex { get; set; }

    /// <summary>
    /// Gets or sets the leading codepoint.
    /// </summary>
    public CodePoint CodePoint { get; set; }

    /// <summary>
    /// Gets or sets the codepoint count represented by this glyph.
    /// </summary>
    public int CodePointCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the text direction.
    /// </summary>
    public TextDirection Direction
    {
        readonly get => (TextDirection)this.direction;
        set => this.direction = (byte)value;
    }

    /// <summary>
    /// Gets or sets the index of the text run this glyph belongs to, resolved against
    /// the buffer's run list. An index keeps the record free of object references, so
    /// the garbage collector never scans the pooled glyph arrays.
    /// </summary>
    public ushort TextRunIndex { get; set; }

    /// <summary>
    /// Gets or sets the one-based index into the fallback font list.
    /// Zero selects the font of the text run.
    /// </summary>
    public int FallbackFontIndex { get; set; }

    /// <summary>
    /// Gets or sets the id of any ligature this glyph is a member of. Zero means the
    /// glyph is not a ligature member; assigned ids fold into the three-bit range
    /// while remaining distinct from zero.
    /// </summary>
    public int LigatureId
    {
        readonly get => this.ligatureId;
        set => this.ligatureId = value == 0 ? (byte)0 : (byte)(((value - 1) % LigatureIdModulus) + 1);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the glyph is ligated.
    /// </summary>
    public bool IsLigated
    {
        readonly get => (this.flags & LigatedFlag) != 0;
        set => this.flags = value ? (ushort)(this.flags | LigatedFlag) : (ushort)(this.flags & ~LigatedFlag);
    }

    /// <summary>
    /// Gets or sets the ligature component index of the glyph, or -1 when the glyph
    /// is not a ligature component. Stored offset by one so the default record holds
    /// the -1 sentinel; indices clamp to the storable range, far beyond any real
    /// font's component count.
    /// </summary>
    public int LigatureComponent
    {
        readonly get => this.ligatureComponentPlusOne - 1;
        set => this.ligatureComponentPlusOne = (byte)(Math.Min(value, byte.MaxValue - 1) + 1);
    }

    /// <summary>
    /// Gets or sets the number of matched components represented by this ligature.
    /// This differs from <see cref="CodePointCount"/> because ignored marks may remain
    /// between matched components.
    /// </summary>
    public int LigatureComponentCount
    {
        readonly get => this.ligatureComponentCount == 0 ? 1 : this.ligatureComponentCount;
        set => this.ligatureComponentCount = (byte)Math.Min(value, byte.MaxValue);
    }

    /// <summary>
    /// Gets or sets the mask of features a shaper has registered for this glyph, enabled
    /// or not. Bits are assigned by the owning plan's
    /// <see cref="ShapePlanFeatures"/>. Enabling a feature
    /// only ever reveals a registered bit; a feature that was never registered for the
    /// glyph cannot be enabled.
    /// </summary>
    public uint RegisteredFeatureMask { get; set; }

    /// <summary>
    /// Gets or sets the mask of features currently enabled for this glyph: the subset of
    /// <see cref="RegisteredFeatureMask"/> a lookup application gate tests with a single
    /// bitwise AND.
    /// </summary>
    public uint FeatureMask { get; set; }

    /// <summary>
    /// Gets or sets the mask of features whose lookups actually changed this glyph.
    /// Only the vertical trio is recorded, in the reserved bits every plan shares,
    /// because vertical alternate detection is the sole consumer; the record
    /// therefore survives the copy into the positioning buffer regardless of which
    /// plan wrote it.
    /// </summary>
    public uint AppliedFeatureMask { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this glyph is the result of a substitution.
    /// </summary>
    public bool IsSubstituted
    {
        readonly get => (this.flags & SubstitutedFlag) != 0;
        set => this.flags = value ? (ushort)(this.flags | SubstitutedFlag) : (ushort)(this.flags & ~SubstitutedFlag);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this glyph is the result of a decomposition substitution
    /// </summary>
    public bool IsDecomposed
    {
        readonly get => (this.flags & DecomposedFlag) != 0;
        set => this.flags = value ? (ushort)(this.flags | DecomposedFlag) : (ushort)(this.flags & ~DecomposedFlag);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this glyph is a fixed tile in a
    /// stretch decomposition.
    /// </summary>
    public bool IsFixedStretch
    {
        readonly get => (this.flags & FixedStretchFlag) != 0;
        set => this.flags = value ? (ushort)(this.flags | FixedStretchFlag) : (ushort)(this.flags & ~FixedStretchFlag);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this glyph is a repeating tile in a
    /// stretch decomposition.
    /// </summary>
    public bool IsRepeatingStretch
    {
        readonly get => (this.flags & RepeatingStretchFlag) != 0;
        set => this.flags = value ? (ushort)(this.flags | RepeatingStretchFlag) : (ushort)(this.flags & ~RepeatingStretchFlag);
    }

    /// <summary>
    /// Gets or sets a value indicating whether tracking must preserve the glyph's
    /// cursive shaping run.
    /// </summary>
    public bool IsCursiveScript
    {
        readonly get => (this.flags & CursiveScriptFlag) != 0;
        set => this.flags = value ? (ushort)(this.flags | CursiveScriptFlag) : (ushort)(this.flags & ~CursiveScriptFlag);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this glyph represents an inline placeholder.
    /// A placeholder's bidi run lives on the buffer, keyed by codepoint index.
    /// </summary>
    public bool IsPlaceholder
    {
        readonly get => (this.flags & PlaceholderFlag) != 0;
        set => this.flags = value ? (ushort)(this.flags | PlaceholderFlag) : (ushort)(this.flags & ~PlaceholderFlag);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the glyph's codepoint is a default
    /// ignorable that renders invisibly. Classified once as the record enters the
    /// buffer; the carve-outs that render as regular spacing glyphs, such as the
    /// Hangul fillers, never receive the bit.
    /// </summary>
    public bool IsDefaultIgnorable
    {
        readonly get => (this.flags & DefaultIgnorableFlag) != 0;
        set => this.flags = value ? (ushort)(this.flags | DefaultIgnorableFlag) : (ushort)(this.flags & ~DefaultIgnorableFlag);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the hide stage replaced this glyph
    /// with the invisible glyph at zero advance. Consumers read this recorded
    /// decision instead of re-deriving it from the codepoint.
    /// </summary>
    public bool IsHidden
    {
        readonly get => (this.flags & HiddenFlag) != 0;
        set => this.flags = value ? (ushort)(this.flags | HiddenFlag) : (ushort)(this.flags & ~HiddenFlag);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the codepoint is the zero width
    /// non-joiner. Classified once as the record enters the buffer; sequence
    /// matching consults the bit when deciding joiner transparency.
    /// </summary>
    public bool IsZwnj
    {
        readonly get => (this.flags & ZwnjFlag) != 0;
        set => this.flags = value ? (ushort)(this.flags | ZwnjFlag) : (ushort)(this.flags & ~ZwnjFlag);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the codepoint is the zero width
    /// joiner. Classified once as the record enters the buffer; sequence matching
    /// consults the bit when deciding joiner transparency.
    /// </summary>
    public bool IsZwj
    {
        readonly get => (this.flags & ZwjFlag) != 0;
        set => this.flags = value ? (ushort)(this.flags | ZwjFlag) : (ushort)(this.flags & ~ZwjFlag);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the codepoint is a default
    /// ignorable that must stay matchable during substitution while positioning
    /// treats it as transparent: the Mongolian free variation selectors, the tag
    /// characters, and the combining grapheme joiner.
    /// </summary>
    public bool IsHiddenIgnorable
    {
        readonly get => (this.flags & HiddenIgnorableFlag) != 0;
        set => this.flags = value ? (ushort)(this.flags | HiddenIgnorableFlag) : (ushort)(this.flags & ~HiddenIgnorableFlag);
    }

    /// <summary>
    /// Gets or sets the script-specific mark-order override, or zero when the character's generated order applies.
    /// </summary>
    public int MarkOrderOverride
    {
        readonly get => (this.flags & MarkOrder22Flag) != 0 ? 22 : (this.flags & MarkOrder26Flag) != 0 ? 26 : 0;
        set
        {
            this.flags = (ushort)(this.flags & ~MarkOrderFlags);
            this.flags = value == 22
                ? (ushort)(this.flags | MarkOrder22Flag)
                : value == 26
                    ? (ushort)(this.flags | MarkOrder26Flag)
                    : this.flags;
        }
    }

    /// <summary>
    /// Gets the order used to compare this record with adjacent combining marks.
    /// </summary>
    public readonly int MarkOrderingClass
        => this.MarkOrderOverride is int order and not 0 ? order : CodePoint.GetMarkOrderingClass(this.CodePoint);

    private readonly string DebuggerDisplay
        => FormattableString
        .Invariant($" {this.GlyphId} : {this.CodePoint.ToDebuggerDisplay()} : {CodePoint.GetScriptClass(this.CodePoint)} : {this.Direction} : run {this.TextRunIndex} : {this.LigatureId} : {this.LigatureComponent} : {this.IsDecomposed}");

    /// <summary>
    /// Resets the registered and enabled feature masks to the seeded global bit
    /// while preserving the applied mask, matching the semantics of copying with
    /// cleared features. Positioning reuses the substituted glyph data and re-plans
    /// its own varying features, but the applied record of what substitution did
    /// must survive for consumers such as vertical alternate detection.
    /// </summary>
    public void ClearFeatures()
    {
        this.RegisteredFeatureMask = ShapePlanFeatures.GlobalFeatureMask;
        this.FeatureMask = ShapePlanFeatures.GlobalFeatureMask;
    }

    public readonly string ToDebuggerDisplay() => this.DebuggerDisplay;
}
