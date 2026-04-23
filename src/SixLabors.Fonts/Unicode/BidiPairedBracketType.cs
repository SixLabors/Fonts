// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Unicode Bidi_Paired_Bracket_Type property values.
/// <see href="https://www.unicode.org/reports/tr9/#Paired_Brackets"/>
/// </summary>
/// <remarks>
/// UAX #9 uses this property with <c>Bidi_Paired_Bracket</c> to find bracket pairs
/// while resolving neutral characters.
/// </remarks>
public enum BidiPairedBracketType
{
    /// <summary>
    /// No paired bracket behavior.
    /// </summary>
    None = 0,

    /// <summary>
    /// Opening paired bracket.
    /// </summary>
    Open = 1,

    /// <summary>
    /// Closing paired bracket.
    /// </summary>
    Close = 2
}
