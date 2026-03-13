// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// A single MVAR value record mapping a metric tag to a delta-set index.
/// </summary>
internal readonly struct MetricValueRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MetricValueRecord"/> struct.
    /// </summary>
    /// <param name="tag">The four-byte tag identifying the metric.</param>
    /// <param name="deltaSetOuterIndex">The outer index into the ItemVariationStore.</param>
    /// <param name="deltaSetInnerIndex">The inner index into the ItemVariationStore.</param>
    public MetricValueRecord(Tag tag, ushort deltaSetOuterIndex, ushort deltaSetInnerIndex)
    {
        this.Tag = tag;
        this.DeltaSetOuterIndex = deltaSetOuterIndex;
        this.DeltaSetInnerIndex = deltaSetInnerIndex;
    }

    /// <summary>
    /// Gets the four-byte tag identifying the metric (e.g. 'hasc', 'hdsc').
    /// </summary>
    public Tag Tag { get; }

    /// <summary>
    /// Gets the outer index into the ItemVariationStore.
    /// </summary>
    public ushort DeltaSetOuterIndex { get; }

    /// <summary>
    /// Gets the inner index into the ItemVariationStore.
    /// </summary>
    public ushort DeltaSetInnerIndex { get; }
}
