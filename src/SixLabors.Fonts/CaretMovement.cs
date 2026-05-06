// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Specifies a caret movement operation within laid-out text.
/// </summary>
public enum CaretMovement
{
    /// <summary>
    /// Move to the previous grapheme insertion position.
    /// </summary>
    Previous,

    /// <summary>
    /// Move to the next grapheme insertion position.
    /// </summary>
    Next,

    /// <summary>
    /// Move to the start of the current line.
    /// </summary>
    LineStart,

    /// <summary>
    /// Move to the end of the current line.
    /// </summary>
    LineEnd,

    /// <summary>
    /// Move to the start of the laid-out text.
    /// </summary>
    TextStart,

    /// <summary>
    /// Move to the end of the laid-out text.
    /// </summary>
    TextEnd,

    /// <summary>
    /// Move to the previous visual line.
    /// </summary>
    LineUp,

    /// <summary>
    /// Move to the next visual line.
    /// </summary>
    LineDown
}
