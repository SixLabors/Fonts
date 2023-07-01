// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using SixLabors.Fonts.Unicode.Resources;

namespace SixLabors.Fonts.Unicode
{
    internal static class UnicodeData
    {
        private static readonly Lazy<UnicodeTrie> LazyBidiTrie = new(() => GetBidiTrie());
        private static readonly Lazy<UnicodeTrie> LazyBidiMirrorTrie = new(() => GetBidiMirrorTrie());
        private static readonly Lazy<UnicodeTrie> LazyGraphemeTrie = new(() => GetGraphemeTrie());
        private static readonly Lazy<UnicodeTrie> LazyLineBreakTrie = new(() => GetLineBreakTrie());
        private static readonly Lazy<UnicodeTrie> LazyScriptTrie = new(() => GetScriptTrie());
        private static readonly Lazy<UnicodeTrie> LazyCategoryTrie = new(() => GetCategoryTrie());
        private static readonly Lazy<UnicodeTrie> LazyArabicShapingTrie = new(() => GetArabicShapingTrie());
        private static readonly Lazy<UnicodeTrie> LazyIndicSyllabicCategoryTrie = new(() => GetIndicSyllabicCategoryTrie());
        private static readonly Lazy<UnicodeTrie> LazyIndicPositionalCategoryTrie = new(() => GetIndicPositionalCategoryTrie());
        private static readonly Lazy<UnicodeTrie> LazyVerticalOrientationTrie = new(() => GetVerticalOrientationTrie());

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
    }
}
