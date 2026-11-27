// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;

/// <summary>
/// Abstract base class for all script shapers. Defines the shaping pipeline
/// consisting of preprocessing, feature planning, postprocessing, and feature assignment stages.
/// </summary>
internal abstract class BaseShaper
{
    /// <summary>
    /// Gets the feature bit assignment for the plan this shaper belongs to. The
    /// shaper creates it and the owning plan adopts it, so both hold the same
    /// non-null instance for their whole lifetime; a shaper exists only inside a
    /// plan, so feature state is never reachable without one.
    /// </summary>
    public ShapePlanFeatures Features { get; } = new();

    /// <summary>
    /// Gets or sets the user-specified feature tags for the shaping segments
    /// owned by this plan.
    /// </summary>
    public IReadOnlyList<Tag> FeatureTags { get; set; } = Array.Empty<Tag>();

    /// <summary>
    /// Gets a value indicating whether the first planning pass has collected the
    /// plan's feature registrations. Registration is deterministic for a plan's
    /// identity, so later passes replay the collected masks instead of
    /// re-registering.
    /// </summary>
    public bool FeaturesCollected { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the first planning pass is
    /// currently collecting registrations; whole-segment feature additions fold
    /// their masks only while this is set. Shapers clear it around additions
    /// whose ranges depend on the text, such as direction spans.
    /// </summary>
    public bool CollectingFeatures { get; protected set; }

    /// <summary>
    /// Gets or sets the registered-mask fold of every whole-segment feature the
    /// collecting pass added, replayed over later segments in one walk.
    /// </summary>
    public uint FoldedRegisteredMask { get; protected set; }

    /// <summary>
    /// Gets or sets the enabled-mask fold paired with
    /// <see cref="FoldedRegisteredMask"/>.
    /// </summary>
    public uint FoldedEnabledMask { get; protected set; }

    /// <summary>
    /// Gets the segment start the collecting pass is planning; feature additions
    /// fold only when their range is exactly the planned segment.
    /// </summary>
    public int CollectSegmentIndex { get; private set; }

    /// <summary>
    /// Gets the segment length the collecting pass is planning, tracking count
    /// adjustments between phases.
    /// </summary>
    public int CollectSegmentCount { get; private set; }

    /// <summary>
    /// Gets or sets the script classification for this shaper.
    /// </summary>
    public ScriptClass ScriptClass { get; protected set; }

    /// <summary>
    /// Gets or sets the mark zeroing mode that determines when mark advances are zeroed.
    /// </summary>
    public MarkZeroingMode MarkZeroingMode { get; protected set; }

    /// <summary>
    /// Gets or sets a value indicating whether marks are positioned from glyph extents when no positioning table is available.
    /// </summary>
    public bool FallbackMarkPositioning { get; protected set; }

    /// <summary>
    /// Gets or sets how far the text is taken apart and put back together before it
    /// is shaped. Defaults to joining the marks the font offers joined forms for,
    /// which is what a script without an engine of its own wants.
    /// </summary>
    public NormalizationMode NormalizationMode { get; protected set; } = NormalizationMode.ComposedDiacritics;

    /// <summary>
    /// Takes a character apart into the pair it is canonically equivalent to. A
    /// script whose characters spell a sound the font draws in pieces overrides this
    /// to hand those pieces back.
    /// </summary>
    /// <param name="codePoint">The character to take apart.</param>
    /// <param name="first">When this method returns, contains the leading character.</param>
    /// <param name="second">
    /// When this method returns, contains the trailing character, or the default when
    /// the character stands for a single other one.
    /// </param>
    /// <returns><see langword="true"/> when the character comes apart.</returns>
    public virtual bool TryDecompose(CodePoint codePoint, out CodePoint first, out CodePoint second)
        => CodePoint.TryDecompose(codePoint, out first, out second);

    /// <summary>
    /// Joins a pair of characters into the single character they are canonically
    /// equivalent to. A script that must not have two of its marks joined back
    /// together overrides this to refuse.
    /// </summary>
    /// <param name="first">The leading character.</param>
    /// <param name="second">The trailing character.</param>
    /// <param name="composed">When this method returns, contains the joined character.</param>
    /// <returns><see langword="true"/> when the pair joins.</returns>
    public virtual bool TryCompose(CodePoint first, CodePoint second, out CodePoint composed)
        => CodePoint.TryCompose(first, second, out composed);

    /// <summary>
    /// Applies script-specific ordering within a canonically ordered mark run.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="start">The zero-based index of the first mark.</param>
    /// <param name="end">The exclusive end index of the mark run.</param>
    public virtual void ReorderNormalizedMarks(ShapingBuffer buffer, int start, int end)
    {
    }

    /// <summary>
    /// Applies script-specific expansion after positioning has completed. The
    /// default shaper leaves the positioned records unchanged.
    /// </summary>
    /// <param name="buffer">The positioned shaping buffer.</param>
    /// <param name="index">The zero-based index of the first record.</param>
    /// <param name="count">The number of records in the segment.</param>
    public virtual void PostprocessGlyphs(ShapingBuffer buffer, int index, int count)
    {
    }

    /// <summary>
    /// Assigns the features to each glyph within the buffer.
    /// </summary>
    /// <param name="fontMetrics">The font metrics, which decide what the font can draw.</param>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based index of the elements to assign.</param>
    /// <param name="count">The number of elements to assign.</param>
    /// <param name="isVertical">Whether the run uses vertical shaping.</param>
    public void Plan(FontMetrics fontMetrics, ShapingBuffer buffer, int index, int count, bool isVertical)
    {
        // The text is prepared before any feature is planned, so every later
        // stage sees the records this step inserts. It reads the text alone, so
        // it runs once, on the pass that substitutes.
        if (buffer.Role == ShapingBufferRole.Substitution)
        {
            int preprocessCount = buffer.Count;

            this.PreprocessText(buffer, index, count);

            count += buffer.Count - preprocessCount;

            // Normalizing settles which characters the run is spelled with, so it
            // runs before any feature is planned against them, and once.
            count += TextNormalizer.Normalize(this, fontMetrics, buffer, index, count);
        }

        // Registration is deterministic for a plan's identity, so the first pass
        // collects it once: feature bits, stages, joiner flags, and the fold of
        // every whole-segment mask. Later passes replay the fold in one walk and
        // run only the per-text work. Runs in mixed layout share plans but can
        // have different orientations, so they collect features for each run.
        if (!this.FeaturesCollected)
        {
            int collectionCount = buffer.Count;
            this.CollectingFeatures = true;
            this.CollectSegmentIndex = index;
            this.CollectSegmentCount = count;

            this.PlanPreprocessingFeatures(buffer, index, count);

            RecalculateCount(buffer, ref collectionCount, ref count);
            this.CollectSegmentCount = count;

            this.PlanFeatures(buffer, index, count);

            RecalculateCount(buffer, ref collectionCount, ref count);
            this.CollectSegmentCount = count;

            this.PlanPostprocessingFeatures(buffer, index, count, isVertical);

            RecalculateCount(buffer, ref collectionCount, ref count);

            this.CollectingFeatures = false;
            this.FeaturesCollected = !buffer.TextOptions.LayoutMode.IsVerticalMixed();
        }
        else
        {
            if (this.FoldedRegisteredMask != 0)
            {
                buffer.AddShapingFeatureMasks(index, count, this.FoldedRegisteredMask, this.FoldedEnabledMask);
            }
        }

        this.SetupMasks(buffer, index, count);

        this.AssignFeatures(buffer, index, count);
    }

    /// <summary>
    /// Prepares the text of the segment before any feature is planned. A script
    /// whose text can spell something it must not be read as separates the
    /// characters here, where the records still stand as the text wrote them.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based index of the first record.</param>
    /// <param name="count">The number of records.</param>
    protected virtual void PreprocessText(ShapingBuffer buffer, int index, int count)
    {
    }

    /// <summary>
    /// Applies the per-text feature masks that cannot fold into the collected
    /// whole-segment masks, such as spans that follow the resolved direction of
    /// the records. Runs on every planning pass, after collection or replay.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based index of the elements to assign.</param>
    /// <param name="count">The number of elements to assign.</param>
    protected virtual void SetupMasks(ShapingBuffer buffer, int index, int count)
    {
    }

    /// <summary>
    /// Assigns the features to each glyph within the buffer.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based index of the elements to assign.</param>
    /// <param name="count">The number of elements to assign.</param>
    protected abstract void PlanFeatures(ShapingBuffer buffer, int index, int count);

    /// <summary>
    /// Assigns the preprocessing features to each glyph within the buffer.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based index of the elements to assign.</param>
    /// <param name="count">The number of elements to assign.</param>
    protected abstract void PlanPreprocessingFeatures(ShapingBuffer buffer, int index, int count);

    /// <summary>
    /// Assigns the postprocessing features to each glyph within the buffer.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based index of the elements to assign.</param>
    /// <param name="count">The number of elements to assign.</param>
    /// <param name="isVertical">Whether the run uses vertical shaping.</param>
    protected abstract void PlanPostprocessingFeatures(ShapingBuffer buffer, int index, int count, bool isVertical);

    /// <summary>
    /// Assigns the shaper specific substitution features to each glyph within the buffer.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based index of the elements to assign.</param>
    /// <param name="count">The number of elements to assign.</param>
    protected abstract void AssignFeatures(ShapingBuffer buffer, int index, int count);

    /// <summary>
    /// Gets the ordered buffer of shaping stages for this shaper. The concrete
    /// list type lets the per-section stage walk enumerate without interface
    /// dispatch or a boxed enumerator.
    /// </summary>
    /// <returns>The shaping stages.</returns>
    public abstract List<ShapingStage> GetShapingStages();

    /// <summary>
    /// Recalculates the count when the buffer size changes during shaping.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="oldCount">The previous buffer count, updated to the current count.</param>
    /// <param name="count">The element count, adjusted by the size delta.</param>
    private static void RecalculateCount(ShapingBuffer buffer, ref int oldCount, ref int count)
    {
        // If the buffer has changed size we need to recalculate the count.
        int delta = buffer.Count - oldCount;
        count += delta;
        oldCount += delta;
    }
}
