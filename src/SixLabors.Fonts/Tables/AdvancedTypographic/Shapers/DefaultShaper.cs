// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;

/// <summary>
/// Default shaper, which will be applied to all glyphs.
/// Based on fontkit: <see href="https://github.com/foliojs/fontkit/blob/master/src/opentype/shapers/DefaultShaper.js"/>
/// </summary>
internal class DefaultShaper : BaseShaper
{
    /// <summary>
    /// The 'rvrn' (required variation alternates) feature tag.
    /// </summary>
    protected static readonly Tag RvnrTag = Tag.Parse("rvrn");

    /// <summary>
    /// The random alternate feature tag.
    /// </summary>
    protected static readonly Tag RandTag = Tag.Parse("rand");

    /// <summary>
    /// The 'ltra' (left-to-right alternates) feature tag.
    /// </summary>
    protected static readonly Tag LtraTag = Tag.Parse("ltra");

    /// <summary>
    /// The 'ltrm' (left-to-right mirrored forms) feature tag.
    /// </summary>
    protected static readonly Tag LtrmTag = Tag.Parse("ltrm");

    /// <summary>
    /// The 'rtla' (right-to-left alternates) feature tag.
    /// </summary>
    protected static readonly Tag RtlaTag = Tag.Parse("rtla");

    /// <summary>
    /// The 'rtlm' (right-to-left mirrored forms) feature tag.
    /// </summary>
    protected static readonly Tag RtlmTag = Tag.Parse("rtlm");

    /// <summary>
    /// The 'frac' (fractions) feature tag.
    /// </summary>
    protected static readonly Tag FracTag = Tag.Parse("frac");

    /// <summary>
    /// The 'numr' (numerators) feature tag.
    /// </summary>
    protected static readonly Tag NumrTag = Tag.Parse("numr");

    /// <summary>
    /// The 'dnom' (denominators) feature tag.
    /// </summary>
    protected static readonly Tag DnomTag = Tag.Parse("dnom");

    /// <summary>
    /// The 'ccmp' (glyph composition/decomposition) feature tag.
    /// </summary>
    protected static readonly Tag CcmpTag = Tag.Parse("ccmp");

    /// <summary>
    /// The 'locl' (localized forms) feature tag.
    /// </summary>
    protected static readonly Tag LoclTag = Tag.Parse("locl");

    /// <summary>
    /// The 'rlig' (required ligatures) feature tag.
    /// </summary>
    protected static readonly Tag RligTag = Tag.Parse("rlig");

    /// <summary>
    /// The 'mark' (mark positioning) feature tag.
    /// </summary>
    protected static readonly Tag MarkTag = Tag.Parse("mark");

    /// <summary>
    /// The 'mkmk' (mark-to-mark positioning) feature tag.
    /// </summary>
    protected static readonly Tag MkmkTag = Tag.Parse("mkmk");

    /// <summary>
    /// The 'abvm' (above-base mark positioning) feature tag.
    /// </summary>
    protected static readonly Tag AbvmTag = Tag.Parse("abvm");

    /// <summary>
    /// The 'blwm' (below-base mark positioning) feature tag.
    /// </summary>
    protected static readonly Tag BlwmTag = Tag.Parse("blwm");

    /// <summary>
    /// The 'calt' (contextual alternates) feature tag.
    /// </summary>
    protected static readonly Tag CaltTag = Tag.Parse("calt");

    /// <summary>
    /// The 'clig' (contextual ligatures) feature tag.
    /// </summary>
    protected static readonly Tag CligTag = Tag.Parse("clig");

    /// <summary>
    /// The 'liga' (standard ligatures) feature tag.
    /// </summary>
    protected static readonly Tag LigaTag = Tag.Parse("liga");

    /// <summary>
    /// The 'rclt' (required contextual alternates) feature tag.
    /// </summary>
    protected static readonly Tag RcltTag = Tag.Parse("rclt");

    /// <summary>
    /// The 'curs' (cursive positioning) feature tag.
    /// </summary>
    protected static readonly Tag CursTag = Tag.Parse("curs");

    /// <summary>
    /// The 'dist' (distances) feature tag.
    /// </summary>
    protected static readonly Tag DistTag = Tag.Parse("dist");

    /// <summary>
    /// The 'kern' (kerning) feature tag.
    /// </summary>
    protected static readonly Tag KernTag = Tag.Parse("kern");

    /// <summary>
    /// The 'vert' (vertical alternates) feature tag.
    /// </summary>
    protected static readonly Tag VertTag = Tag.Parse("vert");

    /// <summary>
    /// The 'vkrn' (vertical kerning) feature tag.
    /// </summary>
    protected static readonly Tag VKernTag = Tag.Parse("vkrn");

    /// <summary>
    /// The fraction slash code point (U+2044), the only code point that forms
    /// a fraction from the digit runs surrounding it.
    /// </summary>
    private static readonly CodePoint FractionSlash = new(0x2044);

    /// <summary>
    /// The shaping stages accumulated during feature planning, in registration order.
    /// Stage counts are small (≤ ~16), so duplicate suppression scans the list by tag;
    /// this keeps application order deterministic and avoids per-call hashing.
    /// </summary>
    private readonly List<ShapingStage> shapingStages = new(16);

    /// <summary>
    /// The kerning mode from the text options.
    /// </summary>
    private readonly KerningMode kerningMode;

    /// <summary>
    /// Whether layout will add tracking after shaping. Optional ligatures and
    /// contextual alternates must stay off so spacing can be applied at every
    /// grapheme boundary.
    /// </summary>
    private readonly bool hasTracking;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultShaper"/> class with PostGpos mark zeroing.
    /// </summary>
    /// <param name="script">The script classification.</param>
    /// <param name="textOptions">The text options.</param>
    public DefaultShaper(ScriptClass script, TextOptions textOptions)
        : this(script, MarkZeroingMode.PostGpos, textOptions)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultShaper"/> class.
    /// </summary>
    /// <param name="script">The script classification.</param>
    /// <param name="markZeroingMode">The mark zeroing mode.</param>
    /// <param name="textOptions">The text options.</param>
    protected DefaultShaper(ScriptClass script, MarkZeroingMode markZeroingMode, TextOptions textOptions)
    {
        this.ScriptClass = script;
        this.MarkZeroingMode = markZeroingMode;
        this.FallbackMarkPositioning = true;
        this.kerningMode = textOptions.KerningMode;
        this.hasTracking = textOptions.Tracking != 0;
    }

    /// <summary>
    /// Creates a default shaper for an AAT run and disables mark zeroing.
    /// </summary>
    /// <param name="script">The Unicode script of the run.</param>
    /// <param name="textOptions">The text options for the run.</param>
    /// <param name="fallbackMarkPositioning">Whether the run permits fallback mark positioning.</param>
    /// <returns>The default shaper for the run.</returns>
    public static DefaultShaper CreateForMetamorphosis(ScriptClass script, TextOptions textOptions, bool fallbackMarkPositioning)
        => new(script, MarkZeroingMode.None, textOptions) { FallbackMarkPositioning = fallbackMarkPositioning };

    /// <inheritdoc />
    protected override void PlanFeatures(ShapingBuffer buffer, int index, int count)
    {
    }

    /// <inheritdoc />
    protected override void PlanPreprocessingFeatures(ShapingBuffer buffer, int index, int count)
    {
        // Add variation Features.
        this.EnableFeature(buffer, index, count, RvnrTag);

        this.AddDirectionalFeatures(buffer, index, count);
        this.EnableFeature(buffer, index, count, RandTag, ShapingFeatureFlags.Random);
    }

    /// <inheritdoc />
    protected override void SetupMasks(ShapingBuffer buffer, int index, int count)
    {
        this.AddDirectionalFeatures(buffer, index, count);

        if (buffer.HasFractionSlash)
        {
            this.SetupFractionMasks(buffer, index, count);
        }
    }

    /// <summary>
    /// Turns on the fraction features over the digit runs a fraction slash
    /// joins: the numerator before it, the denominator after it, and the slash
    /// itself. A slash without digits on both sides forms no fraction. The
    /// numerator and denominator features swap roles for right-to-left text,
    /// where the leading run is the denominator.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based index of the first element.</param>
    /// <param name="count">The number of elements.</param>
    private void SetupFractionMasks(ShapingBuffer buffer, int index, int count)
    {
        uint fracMask = this.Features.GetOrAddMask(FracTag);
        uint numrMask = this.Features.GetOrAddMask(NumrTag);
        uint dnomMask = this.Features.GetOrAddMask(DnomTag);

        int end = index + count;
        for (int i = index; i < end; i++)
        {
            if (buffer[i].CodePoint != FractionSlash)
            {
                continue;
            }

            int runStart = i;
            while (runStart > index && CodePoint.IsDigit(buffer[runStart - 1].CodePoint))
            {
                runStart--;
            }

            int runEnd = i + 1;
            while (runEnd < end && CodePoint.IsDigit(buffer[runEnd].CodePoint))
            {
                runEnd++;
            }

            // A slash missing digits on either side is just a slash.
            if (runStart == i || runEnd == i + 1)
            {
                continue;
            }

            bool leftToRight = buffer[i].Direction != TextDirection.RightToLeft;
            uint preMask = leftToRight ? numrMask | fracMask : fracMask | dnomMask;
            uint postMask = leftToRight ? fracMask | dnomMask : numrMask | fracMask;

            for (int j = runStart; j < i; j++)
            {
                buffer.EnableShapingFeature(j, preMask);
            }

            buffer.EnableShapingFeature(i, fracMask);

            for (int j = i + 1; j < runEnd; j++)
            {
                buffer.EnableShapingFeature(j, postMask);
            }

            i = runEnd - 1;
        }
    }

    /// <summary>
    /// Adds the directional features once per direction span. A segment may span
    /// direction runs, so these stay varying features whose bits cover only
    /// their own span; a global classification would let one direction's
    /// lookups match glyphs of the other. Their masks depend on each text's
    /// resolved directions, so they never fold and re-apply on every pass.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based index of the first element.</param>
    /// <param name="count">The number of elements.</param>
    private void AddDirectionalFeatures(ShapingBuffer buffer, int index, int count)
    {
        bool collecting = this.CollectingFeatures;
        this.CollectingFeatures = false;

        int end = index + count;
        int spanStart = index;
        while (spanStart < end)
        {
            TextDirection direction = buffer[spanStart].Direction;
            int spanEnd = spanStart + 1;
            while (spanEnd < end && buffer[spanEnd].Direction == direction)
            {
                spanEnd++;
            }

            int spanCount = spanEnd - spanStart;
            if (direction == TextDirection.LeftToRight)
            {
                this.AddFeature(buffer, spanStart, spanCount, LtraTag);
                this.AddFeature(buffer, spanStart, spanCount, LtrmTag);
            }
            else
            {
                this.AddFeature(buffer, spanStart, spanCount, RtlaTag);
                this.AddFeature(buffer, spanStart, spanCount, RtlmTag);
            }

            spanStart = spanEnd;
        }

        this.CollectingFeatures = collecting;
    }

    /// <inheritdoc />
    protected override void PlanPostprocessingFeatures(ShapingBuffer buffer, int index, int count, bool isVertical)
    {
        // Add common features.
        this.EnableFeature(buffer, index, count, AbvmTag);
        this.EnableFeature(buffer, index, count, BlwmTag);
        this.EnableFeature(buffer, index, count, CcmpTag);
        this.EnableFeature(buffer, index, count, LoclTag);
        this.EnableFeature(buffer, index, count, RligTag);
        this.EnableFeature(buffer, index, count, MarkTag, ShapingFeatureFlags.ManualJoiners);
        this.EnableFeature(buffer, index, count, MkmkTag, ShapingFeatureFlags.ManualJoiners);

        // Add horizontal or vertical features.
        if (!isVertical)
        {
            // Add horizontal features.
            if (!this.hasTracking)
            {
                // Browsers disable these optional substitutions for non-zero letter
                // spacing because they can erase boundaries where spacing is applied.
                // Mandatory script shaping stays enabled, and an explicit feature
                // below can opt an optional substitution in.
                this.EnableFeature(buffer, index, count, CaltTag);
                this.EnableFeature(buffer, index, count, CligTag);
                this.EnableFeature(buffer, index, count, LigaTag);
            }

            this.EnableFeature(buffer, index, count, RcltTag);
            this.EnableFeature(buffer, index, count, CursTag);
            this.EnableFeature(buffer, index, count, DistTag);
            this.EnableFeature(buffer, index, count, KernTag);
        }
        else
        {
            // We only apply `vert` feature.See:
            // https://github.com/harfbuzz/harfbuzz/commit/d71c0df2d17f4590d5611239577a6cb532c26528
            // https://lists.freedesktop.org/archives/harfbuzz/2013-August/003490.html

            // We really want to find a 'vert' feature if there's any in the font, no
            // matter which script/langsys it is listed (or not) under.
            // See various bugs referenced from:
            // https://github.com/harfbuzz/harfbuzz/issues/63
            this.EnableFeature(buffer, index, count, VertTag);
        }

        // Register the fraction trio without enabling it: the features exist
        // for every plan, and each text turns them on over the digit runs that
        // surround a fraction slash. A font without them has no lookups to
        // apply, so registration alone costs nothing.
        this.AddFeature(buffer, index, count, FracTag, false);
        this.AddFeature(buffer, index, count, NumrTag, false);
        this.AddFeature(buffer, index, count, DnomTag, false);

        // Apply user features after defaults so an explicit tag can restore an
        // optional feature that tracking disabled above.
        foreach (Tag feature in this.FeatureTags)
        {
            if (feature == FracTag || feature == NumrTag || feature == DnomTag)
            {
                // The trio is registered off for automatic fraction forming; an
                // explicit request turns it on over the whole segment, so the
                // font's own fraction lookups apply wherever they match.
                this.AddFeature(buffer, index, count, feature, true);
            }
            else
            {
                this.EnableFeature(buffer, index, count, feature);
            }
        }
    }

    /// <inheritdoc />
    protected override void AssignFeatures(ShapingBuffer buffer, int index, int count)
    {
    }

    /// <summary>
    /// Adds a shaping feature to the specified range of glyphs in the buffer and registers the corresponding shaping stage.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based index of the first element.</param>
    /// <param name="count">The number of elements.</param>
    /// <param name="feature">The feature tag to add.</param>
    /// <param name="enabled">Whether the feature is initially enabled.</param>
    /// <param name="preAction">An optional action to invoke before the feature is applied.</param>
    /// <param name="postAction">An optional action to invoke after the feature is applied.</param>
    protected void AddFeature(
        ShapingBuffer buffer,
        int index,
        int count,
        Tag feature,
        bool enabled = true,
        Action<ShapePlan, ShapingBuffer, int, int>? preAction = null,
        Action<ShapePlan, ShapingBuffer, int, int>? postAction = null)
    {
        if (this.kerningMode == KerningMode.None)
        {
            if (feature == KernTag || feature == VKernTag)
            {
                return;
            }
        }

        uint mask = this.Features.GetOrAddMask(feature);

        // A whole-segment addition during the collecting pass folds into the
        // masks the replay pass applies in one walk.
        if (this.CollectingFeatures && index == this.CollectSegmentIndex && count == this.CollectSegmentCount)
        {
            this.FoldedRegisteredMask |= mask;
            if (enabled)
            {
                this.FoldedEnabledMask |= mask;
            }
        }

        buffer.AddShapingFeatureRange(index, count, new TagEntry(feature, enabled), mask);
        this.AddStage(feature, preAction, postAction);
    }

    /// <summary>
    /// Adds a varying shaping feature with registration flags controlling its
    /// lookups' joiner handling and syllable scope.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based index of the first element.</param>
    /// <param name="count">The number of elements.</param>
    /// <param name="feature">The feature tag to add.</param>
    /// <param name="flags">The registration flags.</param>
    /// <param name="enabled">Whether the feature is initially enabled.</param>
    /// <param name="preAction">The action to invoke before the feature is applied, or <see langword="null"/>.</param>
    /// <param name="postAction">The action to invoke after the feature is applied, or <see langword="null"/>.</param>
    protected void AddFeature(
        ShapingBuffer buffer,
        int index,
        int count,
        Tag feature,
        ShapingFeatureFlags flags,
        bool enabled,
        Action<ShapePlan, ShapingBuffer, int, int>? preAction,
        Action<ShapePlan, ShapingBuffer, int, int>? postAction)
    {
        this.Features.AddFlags(feature, flags);
        this.AddFeature(buffer, index, count, feature, enabled, preAction, postAction);
    }

    /// <summary>
    /// Registers a global feature over the given range: one that applies to every
    /// glyph of the plan's segments and therefore shares the plan's single global
    /// mask bit instead of consuming a distinct bit. Features whose per-glyph
    /// state varies register through the add-feature overloads instead.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based index of the first element.</param>
    /// <param name="count">The number of elements.</param>
    /// <param name="feature">The feature tag to enable.</param>
    protected void EnableFeature(ShapingBuffer buffer, int index, int count, Tag feature)
        => this.EnableFeature(buffer, index, count, feature, null, null);

    /// <summary>
    /// Registers a global feature with registration flags controlling its lookups'
    /// joiner handling and syllable scope.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based index of the first element.</param>
    /// <param name="count">The number of elements.</param>
    /// <param name="feature">The feature tag to enable.</param>
    /// <param name="flags">The registration flags.</param>
    protected void EnableFeature(ShapingBuffer buffer, int index, int count, Tag feature, ShapingFeatureFlags flags)
    {
        this.Features.AddFlags(feature, flags);
        this.EnableFeature(buffer, index, count, feature, null, null);
    }

    /// <summary>
    /// Registers a global feature with registration flags and stage actions.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based index of the first element.</param>
    /// <param name="count">The number of elements.</param>
    /// <param name="feature">The feature tag to enable.</param>
    /// <param name="flags">The registration flags.</param>
    /// <param name="preAction">The action to invoke before the feature is applied, or <see langword="null"/>.</param>
    /// <param name="postAction">The action to invoke after the feature is applied, or <see langword="null"/>.</param>
    protected void EnableFeature(
        ShapingBuffer buffer,
        int index,
        int count,
        Tag feature,
        ShapingFeatureFlags flags,
        Action<ShapePlan, ShapingBuffer, int, int>? preAction,
        Action<ShapePlan, ShapingBuffer, int, int>? postAction)
    {
        this.Features.AddFlags(feature, flags);
        this.EnableFeature(buffer, index, count, feature, preAction, postAction);
    }

    /// <summary>
    /// Registers a global feature and attaches the supplied stage actions. The
    /// feature applies to every glyph of the plan's segments and therefore shares
    /// the plan's single global mask bit, which every glyph record is born
    /// carrying: registration records the classification and the stage without
    /// touching any glyph.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based index of the first element.</param>
    /// <param name="count">The number of elements.</param>
    /// <param name="feature">The feature tag to enable.</param>
    /// <param name="preAction">The action to invoke before the feature is applied, or <see langword="null"/>.</param>
    /// <param name="postAction">The action to invoke after the feature is applied, or <see langword="null"/>.</param>
    protected void EnableFeature(
        ShapingBuffer buffer,
        int index,
        int count,
        Tag feature,
        Action<ShapePlan, ShapingBuffer, int, int>? preAction,
        Action<ShapePlan, ShapingBuffer, int, int>? postAction)
    {
        if (this.kerningMode == KerningMode.None)
        {
            if (feature == KernTag || feature == VKernTag)
            {
                return;
            }
        }

        _ = this.Features.GetOrAddGlobalMask(feature);
        this.AddStage(feature, preAction, postAction);
    }

    /// <summary>
    /// Appends a shaping stage for the feature unless one already exists. The
    /// first registration wins, matching the previous set semantics: a duplicate
    /// tag keeps the originally supplied pre and post actions.
    /// </summary>
    /// <param name="feature">The feature tag the stage applies.</param>
    /// <param name="preAction">The action to invoke before the feature is applied, or <see langword="null"/>.</param>
    /// <param name="postAction">The action to invoke after the feature is applied, or <see langword="null"/>.</param>
    private void AddStage(
        Tag feature,
        Action<ShapePlan, ShapingBuffer, int, int>? preAction,
        Action<ShapePlan, ShapingBuffer, int, int>? postAction)
    {
        List<ShapingStage> stages = this.shapingStages;
        for (int i = 0; i < stages.Count; i++)
        {
            if (stages[i].FeatureTag == feature)
            {
                return;
            }
        }

        stages.Add(new ShapingStage(feature, preAction, postAction));
    }

    /// <inheritdoc />
    public override List<ShapingStage> GetShapingStages() => this.shapingStages;
}
