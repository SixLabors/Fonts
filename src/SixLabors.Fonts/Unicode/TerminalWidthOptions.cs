// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Defines policy used when resolving grapheme clusters to terminal cell widths.
/// </summary>
public struct TerminalWidthOptions
{
    /// <summary>
    /// Gets the default terminal width policy.
    /// </summary>
    public static TerminalWidthOptions Default => default;

    /// <summary>
    /// Gets or sets the width used for East Asian Width Ambiguous scalars.
    /// </summary>
    public TerminalAmbiguousWidth AmbiguousWidth { get; set; }

    /// <summary>
    /// Gets or sets the width policy used for emoji clusters.
    /// </summary>
    public TerminalEmojiWidth EmojiWidth { get; set; }

    /// <summary>
    /// Gets or sets the width policy used for C0 and C1 control scalars.
    /// </summary>
    public TerminalControlCharacterWidth ControlCharacterWidth { get; set; }
}
