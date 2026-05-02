// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Defines how emoji clusters resolve for terminal cell measurement.
/// </summary>
public enum TerminalEmojiWidth
{
    /// <summary>
    /// Resolve emoji clusters as two terminal cells.
    /// </summary>
    Wide = 0,

    /// <summary>
    /// Do not apply a whole-cluster emoji override; use East Asian Width-derived scalar widths.
    /// </summary>
    EastAsianWidth = 1
}
