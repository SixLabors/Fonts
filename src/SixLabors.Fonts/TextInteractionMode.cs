// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Specifies how text interaction positions are modeled for laid-out text.
/// </summary>
public enum TextInteractionMode
{
    /// <summary>
    /// Uses paragraph-style interaction where trailing breaking whitespace at line ends does not create additional caret stops.
    /// </summary>
    Paragraph,

    /// <summary>
    /// Uses editor-style interaction where ordinary trailing breaking whitespace at line ends remains addressable by caret movement and selection.
    /// </summary>
    Editor
}
