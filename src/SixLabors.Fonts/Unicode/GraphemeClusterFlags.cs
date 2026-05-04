// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Flags describing properties of a Unicode grapheme cluster.
/// </summary>
[Flags]
public enum GraphemeClusterFlags : byte
{
    /// <summary>
    /// No flags.
    /// </summary>
    None = 0,

    /// <summary>
    /// At least one scalar in the cluster resolved to two terminal cells before
    /// any whole-cluster emoji override was applied.
    /// </summary>
    ContainsWide = 1 << 0,

    /// <summary>
    /// At least one scalar in the cluster has East Asian Width Ambiguous.
    /// </summary>
    ContainsAmbiguous = 1 << 1,

    /// <summary>
    /// All scalars in the cluster are zero-width for terminal measurement.
    /// </summary>
    AllZeroWidth = 1 << 2,

    /// <summary>
    /// The cluster contains a C0 or C1 control scalar.
    /// </summary>
    ContainsControl = 1 << 3,

    /// <summary>
    /// The cluster contains an emoji-like scalar or sequence.
    /// </summary>
    ContainsEmoji = 1 << 4,

    /// <summary>
    /// The cluster contains a zero-width joiner sequence.
    /// </summary>
    ContainsZwjSequence = 1 << 5,

    /// <summary>
    /// The cluster contains a variation selector.
    /// </summary>
    ContainsVariationSelector = 1 << 6,

    /// <summary>
    /// The cluster contains exactly one code point.
    /// </summary>
    IsSingleCodePoint = 1 << 7
}
