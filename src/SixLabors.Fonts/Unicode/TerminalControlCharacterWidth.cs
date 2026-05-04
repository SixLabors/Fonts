// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Defines how C0 and C1 control scalars resolve for terminal cell measurement.
/// </summary>
public enum TerminalControlCharacterWidth
{
    /// <summary>
    /// Resolve controls as non-printable.
    /// </summary>
    NonPrintable = 0,

    /// <summary>
    /// Resolve controls as zero terminal cells.
    /// </summary>
    Zero = 1,

    /// <summary>
    /// Resolve controls as one terminal cell.
    /// </summary>
    Narrow = 2
}
