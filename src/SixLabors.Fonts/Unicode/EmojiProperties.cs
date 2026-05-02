// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Binary Unicode emoji properties from UTS #51.
/// </summary>
[Flags]
public enum EmojiProperties : byte
{
    /// <summary>
    /// No emoji properties.
    /// </summary>
    None = 0,

    /// <summary>
    /// The scalar has the Emoji property.
    /// </summary>
    Emoji = 1 << 0,

    /// <summary>
    /// The scalar defaults to emoji presentation.
    /// </summary>
    EmojiPresentation = 1 << 1,

    /// <summary>
    /// The scalar is an emoji modifier.
    /// </summary>
    EmojiModifier = 1 << 2,

    /// <summary>
    /// The scalar can be followed by an emoji modifier.
    /// </summary>
    EmojiModifierBase = 1 << 3,

    /// <summary>
    /// The scalar is used as a component in emoji sequences.
    /// </summary>
    EmojiComponent = 1 << 4,

    /// <summary>
    /// The scalar can be followed by U+FE0E to request text presentation.
    /// </summary>
    TextPresentationSequenceBase = 1 << 5,

    /// <summary>
    /// The scalar can be followed by U+FE0F to request emoji presentation.
    /// </summary>
    EmojiPresentationSequenceBase = 1 << 6,

    /// <summary>
    /// The scalar can start an emoji keycap sequence.
    /// </summary>
    EmojiKeycapSequenceBase = 1 << 7
}
