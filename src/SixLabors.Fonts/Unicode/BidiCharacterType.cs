// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Unicode Bidi_Class property values.
/// <see href="https://www.unicode.org/reports/tr9/#Bidirectional_Character_Types"/>
/// </summary>
/// <remarks>
/// These are the bidirectional character types used by the Unicode Bidirectional
/// Algorithm. The algorithm resolves these input classes into embedding levels and
/// final visual ordering; the enum represents the original character class.
/// </remarks>
public enum BidiCharacterType
{
    // Strong Types

    /// <summary>
    /// Left-to-Right (L).
    /// </summary>
    LeftToRight = 0,

    /// <summary>
    /// Right-to-Left (R).
    /// </summary>
    RightToLeft = 1,

    /// <summary>
    /// Right-to-Left Arabic (AL).
    /// </summary>
    ArabicLetter = 2,

    // Weak Types

    /// <summary>
    /// European Number (EN).
    /// </summary>
    EuropeanNumber = 3,

    /// <summary>
    /// European Number Separator (ES).
    /// </summary>
    EuropeanSeparator = 4,

    /// <summary>
    /// European Number Terminator (ET).
    /// </summary>
    EuropeanTerminator = 5,

    /// <summary>
    /// Arabic Number (AN).
    /// </summary>
    ArabicNumber = 6,

    /// <summary>
    /// Common Number Separator (CS).
    /// </summary>
    CommonSeparator = 7,

    /// <summary>
    /// Nonspacing Mark (NSM).
    /// </summary>
    NonspacingMark = 8,

    /// <summary>
    /// Boundary Neutral (BN).
    /// </summary>
    BoundaryNeutral = 9,

    // Neutral Types

    /// <summary>
    /// Paragraph Separator (B).
    /// </summary>
    ParagraphSeparator = 10,

    /// <summary>
    /// Segment Separator (S).
    /// </summary>
    SegmentSeparator = 11,

    /// <summary>
    /// Whitespace (WS).
    /// </summary>
    Whitespace = 12,

    /// <summary>
    /// Other Neutral (ON).
    /// </summary>
    OtherNeutral = 13,

    // Explicit Formatting Types - Embed

    /// <summary>
    /// Left-to-Right Embedding (LRE).
    /// </summary>
    LeftToRightEmbedding = 14,

    /// <summary>
    /// Left-to-Right Override (LRO).
    /// </summary>
    LeftToRightOverride = 15,

    /// <summary>
    /// Right-to-Left Embedding (RLE).
    /// </summary>
    RightToLeftEmbedding = 16,

    /// <summary>
    /// Right-to-Left Override (RLO).
    /// </summary>
    RightToLeftOverride = 17,

    /// <summary>
    /// Pop Directional Format (PDF).
    /// </summary>
    PopDirectionalFormat = 18,

    // Explicit Formatting Types - Isolate

    /// <summary>
    /// Left-to-Right Isolate (LRI).
    /// </summary>
    LeftToRightIsolate = 19,

    /// <summary>
    /// Right-to-Left Isolate (RLI).
    /// </summary>
    RightToLeftIsolate = 20,

    /// <summary>
    /// First Strong Isolate (FSI).
    /// </summary>
    FirstStrongIsolate = 21,

    /// <summary>
    /// Pop Directional Isolate (PDI).
    /// </summary>
    PopDirectionalIsolate = 22,
}
