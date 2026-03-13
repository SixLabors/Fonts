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
    /// Gets or sets the script classification for this shaper.
    /// </summary>
    public ScriptClass ScriptClass { get; protected set; }

    /// <summary>
    /// Gets or sets the mark zeroing mode that determines when mark advances are zeroed.
    /// </summary>
    public MarkZeroingMode MarkZeroingMode { get; protected set; }

    /// <summary>
    /// Assigns the features to each glyph within the collection.
    /// </summary>
    /// <param name="collection">The glyph shaping collection.</param>
    /// <param name="index">The zero-based index of the elements to assign.</param>
    /// <param name="count">The number of elements to assign.</param>
    public void Plan(IGlyphShapingCollection collection, int index, int count)
    {
        int collectionCount = collection.Count;

        this.PlanPreprocessingFeatures(collection, index, count);

        RecalculateCount(collection, ref collectionCount, ref count);

        this.PlanFeatures(collection, index, count);

        RecalculateCount(collection, ref collectionCount, ref count);

        this.PlanPostprocessingFeatures(collection, index, count);

        RecalculateCount(collection, ref collectionCount, ref count);

        this.AssignFeatures(collection, index, count);
    }

    /// <summary>
    /// Assigns the features to each glyph within the collection.
    /// </summary>
    /// <param name="collection">The glyph shaping collection.</param>
    /// <param name="index">The zero-based index of the elements to assign.</param>
    /// <param name="count">The number of elements to assign.</param>
    protected abstract void PlanFeatures(IGlyphShapingCollection collection, int index, int count);

    /// <summary>
    /// Assigns the preprocessing features to each glyph within the collection.
    /// </summary>
    /// <param name="collection">The glyph shaping collection.</param>
    /// <param name="index">The zero-based index of the elements to assign.</param>
    /// <param name="count">The number of elements to assign.</param>
    protected abstract void PlanPreprocessingFeatures(IGlyphShapingCollection collection, int index, int count);

    /// <summary>
    /// Assigns the postprocessing features to each glyph within the collection.
    /// </summary>
    /// <param name="collection">The glyph shaping collection.</param>
    /// <param name="index">The zero-based index of the elements to assign.</param>
    /// <param name="count">The number of elements to assign.</param>
    protected abstract void PlanPostprocessingFeatures(IGlyphShapingCollection collection, int index, int count);

    /// <summary>
    /// Assigns the shaper specific substitution features to each glyph within the collection.
    /// </summary>
    /// <param name="collection">The glyph shaping collection.</param>
    /// <param name="index">The zero-based index of the elements to assign.</param>
    /// <param name="count">The number of elements to assign.</param>
    protected abstract void AssignFeatures(IGlyphShapingCollection collection, int index, int count);

    /// <summary>
    /// Gets the ordered collection of shaping stages for this shaper.
    /// </summary>
    /// <returns>The shaping stages.</returns>
    public abstract IEnumerable<ShapingStage> GetShapingStages();

    /// <summary>
    /// Recalculates the count when the collection size changes during shaping.
    /// </summary>
    /// <param name="collection">The glyph shaping collection.</param>
    /// <param name="oldCount">The previous collection count, updated to the current count.</param>
    /// <param name="count">The element count, adjusted by the size delta.</param>
    private static void RecalculateCount(IGlyphShapingCollection collection, ref int oldCount, ref int count)
    {
        // If the collection has changed size we need to recalculate the count.
        int delta = collection.Count - oldCount;
        count += delta;
        oldCount += delta;
    }
}
