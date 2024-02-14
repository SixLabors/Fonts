// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using System.Runtime.CompilerServices;
using SixLabors.Fonts.Unicode.Resources;

namespace SixLabors.Fonts.Unicode;

internal static class UnicodeData
{
    private static readonly Lazy<UnicodeTrie> LazyBidiTrie = new(() => GetBidiTrie(), true);
    private static readonly Lazy<UnicodeTrie> LazyBidiMirrorTrie = new(() => GetBidiMirrorTrie(), true);
    private static readonly Lazy<UnicodeTrie> LazyGraphemeTrie = new(() => GetGraphemeTrie(), true);
    private static readonly Lazy<UnicodeTrie> LazyLineBreakTrie = new(() => GetLineBreakTrie(), true);
    private static readonly Lazy<UnicodeTrie> LazyScriptTrie = new(() => GetScriptTrie(), true);
    private static readonly Lazy<UnicodeTrie> LazyCategoryTrie = new(() => GetCategoryTrie(), true);
    private static readonly Lazy<UnicodeTrie> LazyArabicShapingTrie = new(() => GetArabicShapingTrie(), true);
    private static readonly Lazy<UnicodeTrie> LazyIndicSyllabicCategoryTrie = new(() => GetIndicSyllabicCategoryTrie(), true);
    private static readonly Lazy<UnicodeTrie> LazyIndicPositionalCategoryTrie = new(() => GetIndicPositionalCategoryTrie(), true);
    private static readonly Lazy<UnicodeTrie> LazyVerticalOrientationTrie = new(() => GetVerticalOrientationTrie(), true);
    private static readonly Lazy<UnicodeTrie> LazyUniversalShapingTrie = new(() => GetUniversalShapingTrie(), true);
    private static readonly Lazy<UnicodeTrie> LazyIndicShapingTrie = new(() => GetIndicShapingTrie(), true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetBidiData(uint codePoint) => LazyBidiTrie.Value.Get(codePoint);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetBidiMirror(uint codePoint) => LazyBidiMirrorTrie.Value.Get(codePoint);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GraphemeClusterClass GetGraphemeClusterClass(uint codePoint) => (GraphemeClusterClass)LazyGraphemeTrie.Value.Get(codePoint);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LineBreakClass GetLineBreakClass(uint codePoint) => (LineBreakClass)LazyLineBreakTrie.Value.Get(codePoint);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScriptClass GetScriptClass(uint codePoint) => (ScriptClass)LazyScriptTrie.Value.Get(codePoint);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetJoiningClass(uint codePoint) => LazyArabicShapingTrie.Value.Get(codePoint);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnicodeCategory GetUnicodeCategory(uint codePoint) => (UnicodeCategory)LazyCategoryTrie.Value.Get(codePoint);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndicSyllabicCategory GetIndicSyllabicCategory(uint codePoint) => (IndicSyllabicCategory)LazyIndicSyllabicCategoryTrie.Value.Get(codePoint);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndicPositionalCategory GetIndicPositionalCategory(uint codePoint) => (IndicPositionalCategory)LazyIndicPositionalCategoryTrie.Value.Get(codePoint);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VerticalOrientationType GetVerticalOrientation(uint codePoint) => (VerticalOrientationType)LazyVerticalOrientationTrie.Value.Get(codePoint);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetUniversalShapingSymbolCount(uint codePoint) => (int)LazyUniversalShapingTrie.Value.Get(codePoint);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetIndicShapingProperties(uint codePoint) => (int)LazyIndicShapingTrie.Value.Get(codePoint);

    private static UnicodeTrie GetBidiTrie() => new(BidiTrie.Data);

    private static UnicodeTrie GetBidiMirrorTrie() => new(BidiMirrorTrie.Data);

    private static UnicodeTrie GetGraphemeTrie() => new(GraphemeTrie.Data);

    private static UnicodeTrie GetLineBreakTrie() => new(LineBreakTrie.Data);

    private static UnicodeTrie GetScriptTrie() => new(ScriptTrie.Data);

    private static UnicodeTrie GetCategoryTrie() => new(UnicodeCategoryTrie.Data);

    private static UnicodeTrie GetArabicShapingTrie() => new(ArabicShapingTrie.Data);

    private static UnicodeTrie GetIndicSyllabicCategoryTrie() => new(IndicSyllabicCategoryTrie.Data);

    private static UnicodeTrie GetIndicPositionalCategoryTrie() => new(IndicPositionalCategoryTrie.Data);

    private static UnicodeTrie GetVerticalOrientationTrie() => new(VerticalOrientationTrie.Data);

    private static UnicodeTrie GetUniversalShapingTrie() => new(UniversalShapingTrie.Data);

    private static UnicodeTrie GetIndicShapingTrie() => new(IndicShapingTrie.Data);
}
