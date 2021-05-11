// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Represents the smallest unit of a writing system of any given language.
    /// </summary>
    internal readonly ref struct Grapheme
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Grapheme"/> struct.
        /// </summary>
        /// <param name="leadingCodePoint">The leading codepoint.</param>
        /// <param name="codePointCount">The total number of codepoints.</param>
        /// <param name="source">The buffer represented by the grapheme cluster.</param>
        public Grapheme(CodePoint leadingCodePoint, int codePointCount, ReadOnlySpan<char> source)
        {
            this.LeadingCodePoint = leadingCodePoint;
            this.CodePointCount = codePointCount;
            this.Text = source;
        }

        /// <summary>
        /// Gets the leading <see cref="CodePoint"/> of the grapheme cluster.
        /// </summary>
        public CodePoint LeadingCodePoint { get; }

        /// <summary>
        /// Gets the number of code points within the grapheme cluster.
        /// </summary>
        public int CodePointCount { get; }

        /// <summary>
        /// Gets the text that is represented by the grapheme cluster.
        /// </summary>
        public ReadOnlySpan<char> Text { get; }
    }
}
