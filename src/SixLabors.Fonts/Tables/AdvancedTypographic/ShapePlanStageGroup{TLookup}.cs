// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// One pause-delimited stage group of a shape plan: the stage index range it covers
/// and the group's lookups merged into lookup-index order, each entry carrying the
/// combined plan-assigned mask of every feature that registered it, frozen when the
/// plan is built.
/// </summary>
/// <typeparam name="TLookup">The layout table's lookup type.</typeparam>
internal sealed class ShapePlanStageGroup<TLookup>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShapePlanStageGroup{TLookup}"/>
    /// class covering the given stage range.
    /// </summary>
    /// <param name="start">The index of the first stage in the group.</param>
    /// <param name="end">The exclusive index of the last stage in the group.</param>
    public ShapePlanStageGroup(int start, int end)
    {
        this.Start = start;
        this.End = end;
        this.Lookups = new List<(Tag Feature, ushort Index, TLookup LookupTable, uint Mask, bool AutoZwnj, bool AutoZwj, bool Random, bool PerSyllable)>();
    }

    /// <summary>
    /// Gets the index of the first stage in the group; its pre action opens the
    /// group.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// Gets the exclusive index of the last stage in the group; the previous stage's
    /// post action closes the group.
    /// </summary>
    public int End { get; }

    /// <summary>
    /// Gets the group's lookups merged across its stage features into lookup-index
    /// order. A lookup registered by several features appears once with their
    /// plan-assigned masks combined and their joiner handling intersected: a
    /// lookup skips a joiner automatically only when every registering feature
    /// allows it. Application consumes the list directly.
    /// </summary>
    public List<(Tag Feature, ushort Index, TLookup LookupTable, uint Mask, bool AutoZwnj, bool AutoZwj, bool Random, bool PerSyllable)> Lookups { get; }
}
