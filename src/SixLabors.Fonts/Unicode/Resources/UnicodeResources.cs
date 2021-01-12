// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Reflection;

namespace SixLabors.Fonts.Unicode.Resources
{
    internal static class UnicodeResources
    {
        private static readonly Lazy<UnicodeTrie> LazyBidiTrie = new Lazy<UnicodeTrie>(() => GetTrie("Bidi.trie"));
        private static readonly Lazy<UnicodeTrie> LazyLinebreakTrie = new Lazy<UnicodeTrie>(() => GetTrie("LineBreak.trie"));

        public static UnicodeTrie BidiTrie => LazyBidiTrie.Value;

        public static UnicodeTrie LineBreakTrie => LazyLinebreakTrie.Value;

        private static UnicodeTrie GetTrie(string name)
        {
            Stream stream = typeof(UnicodeResources)
                .GetTypeInfo()
                .Assembly
                .GetManifestResourceStream("SixLabors.Fonts.Unicode.Resources." + name);

            return new UnicodeTrie(stream);
        }
    }
}
