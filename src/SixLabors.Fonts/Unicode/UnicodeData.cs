// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Unicode
{
    internal static class UnicodeData
    {
        private static readonly Lazy<UnicodeTrie> LazyBidiTrie = new Lazy<UnicodeTrie>(() => GetTrie("Bidi.trie"));
        private static readonly Lazy<UnicodeTrie> LazyGraphemeTrie = new Lazy<UnicodeTrie>(() => GetTrie("Grapheme.trie"));
        private static readonly Lazy<UnicodeTrie> LazyLinebreakTrie = new Lazy<UnicodeTrie>(() => GetTrie("LineBreak.trie"));
        private static readonly Lazy<UnicodeTrie> LazyCategoryTrie = new Lazy<UnicodeTrie>(() => GetTrie("UnicodeCategory.trie"));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetBidiData(int codePoint) => LazyBidiTrie.Value.Get(codePoint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GraphemeClusterClass GetGraphemeClusterClass(int codePoint) => (GraphemeClusterClass)LazyGraphemeTrie.Value.Get(codePoint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LineBreakClass GetLineBreakClass(int codePoint) => (LineBreakClass)LazyLinebreakTrie.Value.Get(codePoint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnicodeCategory GetUnicodeCategory(int codePoint) => (UnicodeCategory)LazyCategoryTrie.Value.Get(codePoint);

        private static UnicodeTrie GetTrie(string name)
        {
            Stream? stream = typeof(UnicodeData)
                .GetTypeInfo()
                .Assembly
                .GetManifestResourceStream("SixLabors.Fonts.Unicode.Resources." + name);

            return new UnicodeTrie(stream!);
        }
    }
}
