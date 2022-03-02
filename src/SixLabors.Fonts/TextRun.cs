// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents a run of text spanning a series of code points within a string.
    /// </summary>
    public class TextRun
    {
        /// <summary>
        /// Gets or sets the inclusive start index of the first codepoint in this <see cref="TextRun"/>.
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// Gets or sets the exclusive end index of the last codepoint in this <see cref="TextRun"/>.
        /// </summary>
        public int End { get; set; }

        /// <summary>
        /// Gets or sets the font for this run.
        /// </summary>
        public Font? Font { get; set; }

        /// <summary>
        /// Gets or sets the text attributes applied to this run.
        /// </summary>
        public TextAttribute TextAttributes { get; set; }

        /// <summary>
        /// Returns the slice of the given text representing this <see cref="TextRun"/>.
        /// </summary>
        /// <param name="text">The text to slice.</param>
        /// <returns>The <see cref="ReadOnlySpan{Char}"/>.</returns>
        public ReadOnlySpan<char> Slice(ReadOnlySpan<char> text)
        {
            // Convert code point indices into char indices so we can slice
            int chars = 0;
            int count = 0;
            int start = 0;
            int length = 0;
            SpanCodePointEnumerator codePointEnumerator = new(text);
            while (codePointEnumerator.MoveNext())
            {
                if (count == this.Start)
                {
                    start = chars;
                }

                chars += codePointEnumerator.Current.Utf16SequenceLength;
                length = chars - start;
                if (++count == this.End)
                {
                    break;
                }
            }

            return text.Slice(start, length);
        }

        /// <inheritdoc/>
        public override string ToString()
            => $"[TextRun: Start={this.Start}, End={this.End}, TextAttributes={this.TextAttributes}]";
    }
}
