// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// A single MVAR value record mapping a metric tag to a delta-set index.
/// </summary>
internal readonly struct MetricValueRecord
{
    public MetricValueRecord(uint tag, ushort deltaSetOuterIndex, ushort deltaSetInnerIndex)
    {
        this.Tag = tag;
        this.DeltaSetOuterIndex = deltaSetOuterIndex;
        this.DeltaSetInnerIndex = deltaSetInnerIndex;
    }

    /// <summary>
    /// Gets the four-byte tag identifying the metric (e.g. 'hasc', 'hdsc').
    /// </summary>
    public uint Tag { get; }

    /// <summary>
    /// Gets the outer index into the ItemVariationStore.
    /// </summary>
    public ushort DeltaSetOuterIndex { get; }

    /// <summary>
    /// Gets the inner index into the ItemVariationStore.
    /// </summary>
    public ushort DeltaSetInnerIndex { get; }
}
