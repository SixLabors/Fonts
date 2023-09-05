// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests.Unicode;

internal static class UnicodeTypeMaps
{
    public static readonly Dictionary<string, BidiCharacterType> BidiCharacterTypeMap
        = new(StringComparer.OrdinalIgnoreCase)
    {
            { "L", BidiCharacterType.LeftToRight },
            { "R", BidiCharacterType.RightToLeft },
            { "AL", BidiCharacterType.ArabicLetter },
            { "EN", BidiCharacterType.EuropeanNumber },
            { "ES", BidiCharacterType.EuropeanSeparator },
            { "ET", BidiCharacterType.EuropeanTerminator },
            { "AN", BidiCharacterType.ArabicNumber },
            { "CS", BidiCharacterType.CommonSeparator },
            { "NSM", BidiCharacterType.NonspacingMark },
            { "BN", BidiCharacterType.BoundaryNeutral },
            { "B", BidiCharacterType.ParagraphSeparator },
            { "S", BidiCharacterType.SegmentSeparator },
            { "WS", BidiCharacterType.Whitespace },
            { "ON", BidiCharacterType.OtherNeutral },
            { "LRE", BidiCharacterType.LeftToRightEmbedding },
            { "LRO", BidiCharacterType.LeftToRightOverride },
            { "RLE", BidiCharacterType.RightToLeftEmbedding },
            { "RLO", BidiCharacterType.RightToLeftOverride },
            { "PDF", BidiCharacterType.PopDirectionalFormat },
            { "LRI", BidiCharacterType.LeftToRightIsolate },
            { "RLI", BidiCharacterType.RightToLeftIsolate },
            { "FSI", BidiCharacterType.FirstStrongIsolate },
            { "PDI", BidiCharacterType.PopDirectionalIsolate },
    };
}
