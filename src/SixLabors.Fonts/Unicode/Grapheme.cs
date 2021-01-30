// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Represents the smallest unit of a writing system of any given language.
    /// </summary>
    internal readonly struct Grapheme
    {
        public Grapheme(CodePoint codePoint, int index, int offset, ReadOnlyMemory<char> text)
        {
            this.LeadingCodePoint = codePoint;
            this.CodePointIndex = index;
            this.CharOffset = offset;
            this.Text = text;
        }

        /// <summary>
        /// Gets the first <see cref="CodePoint"/> of the grapheme cluster.
        /// </summary>
        public CodePoint LeadingCodePoint { get; }

        /// <summary>
        /// Gets the index of the grapheme cluster within the parent text block.
        /// </summary>
        public int CharOffset { get; }

        /// <summary>
        /// Gets the codepoint index within the parent text block.
        /// </summary>
        public int CodePointIndex { get; }

        /// <summary>
        /// Gets the text that is represented by the grapheme cluster..
        /// </summary>
        public ReadOnlyMemory<char> Text { get; }
    }
}
