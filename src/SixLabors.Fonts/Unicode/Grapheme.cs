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
        public Grapheme(CodePoint firstCodePoint, int offset, ReadOnlyMemory<char> text)
        {
            this.FirstCodePoint = firstCodePoint;
            this.Offset = offset;
            this.Text = text;
        }

        /// <summary>
        /// Gets the first <see cref="CodePoint"/> of the grapheme cluster.
        /// </summary>
        public CodePoint FirstCodePoint { get; }

        /// <summary>
        /// Gets the index of the grapheme cluster withing the parent text block.
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// Gets the text that is represented by the grapheme cluster..
        /// </summary>
        public ReadOnlyMemory<char> Text { get; }
    }
}
