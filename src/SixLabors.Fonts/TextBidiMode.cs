// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Specifies how bidirectional text is resolved.
/// </summary>
public enum TextBidiMode
{
    /// <summary>
    /// Uses the Unicode Bidirectional Algorithm with each character's bidirectional class.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Lays out text in the resolved text direction, ignoring each character's normal bidirectional class.
    /// </summary>
    Override = 1,
}
