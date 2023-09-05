// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;

internal abstract class BaseShaper
{
    public ScriptClass ScriptClass { get; protected set; }

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

    public abstract IEnumerable<ShapingStage> GetShapingStages();

    private static void RecalculateCount(IGlyphShapingCollection collection, ref int oldCount, ref int count)
    {
        // If the collection has changed size we need to recalculate the count.
        int delta = collection.Count - oldCount;
        count += delta;
        oldCount += delta;
    }
}
