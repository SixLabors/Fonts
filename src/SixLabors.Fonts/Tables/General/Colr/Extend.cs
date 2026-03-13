// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Defines the extend mode for COLR v1 gradient color lines, controlling how the gradient
/// is rendered outside the region defined by the color stops.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#color-references"/>
/// </summary>
internal enum Extend : byte
{
    /// <summary>
    /// Pad: the color at the nearest stop is used for all positions outside the stop range.
    /// </summary>
    Pad = 0,

    /// <summary>
    /// Repeat: the gradient pattern is repeated beyond the stop range.
    /// </summary>
    Repeat = 1,

    /// <summary>
    /// Reflect: the gradient pattern is reflected (mirrored) alternately beyond the stop range.
    /// </summary>
    Reflect = 2
}
