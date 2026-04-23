// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Unicode Indic_Conjunct_Break property values used by UAX #29 GB9c.
/// </summary>
/// <remarks>
/// UAX #29 GB9c uses this property to keep matching consonant-linker-consonant
/// sequences inside one extended grapheme cluster.
/// <see href="https://www.unicode.org/reports/tr29/#Grapheme_Cluster_Boundary_Rules"/>
/// </remarks>
public enum IndicConjunctBreakClass
{
    /// <summary>
    /// No Indic conjunct break behavior. This is the default for code points that do not
    /// participate in GB9c.
    /// </summary>
    None = 0,

    /// <summary>
    /// Consonant: a code point that can start or continue an Indic conjunct sequence.
    /// </summary>
    /// <remarks>
    /// In GB9c this is the stable anchor on either side of the linker sequence.
    /// </remarks>
    Consonant = 1,

    /// <summary>
    /// Extend: a combining or extending code point that is transparent inside an Indic conjunct sequence.
    /// </summary>
    /// <remarks>
    /// Extend code points can appear between the consonant and linker without ending
    /// the conjunct sequence.
    /// </remarks>
    Extend = 2,

    /// <summary>
    /// Linker: a code point, such as a virama, that connects a following consonant.
    /// </summary>
    /// <remarks>
    /// A linker keeps the following consonant in the same extended grapheme cluster
    /// when the surrounding GB9c pattern matches.
    /// </remarks>
    Linker = 3
}
