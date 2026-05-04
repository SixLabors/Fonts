// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Defines how East Asian Width Ambiguous scalars resolve for terminal cell measurement.
/// </summary>
public enum TerminalAmbiguousWidth
{
    /// <summary>
    /// Resolve ambiguous scalars as one terminal cell.
    /// </summary>
    Narrow = 0,

    /// <summary>
    /// Resolve ambiguous scalars as two terminal cells.
    /// </summary>
    Wide = 1
}
