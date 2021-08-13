// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Contains extensions methods for memory types.
    /// </summary>
    public static class MemoryExtensions
    {
        /// <summary>
        /// Returns an enumeration of <see cref="CodePoint"/> from the provided span.
        /// </summary>
        /// <param name="span">The readonly span of char elements representing the text to enumerate.</param>
        /// <remarks>
        /// Invalid sequences will be represented in the enumeration by <see cref="CodePoint.ReplacementChar"/>.
        /// </remarks>
        /// <returns>The <see cref="SpanCodePointEnumerator"/>.</returns>
        public static SpanCodePointEnumerator EnumerateCodePoints(this ReadOnlySpan<char> span)
            => new SpanCodePointEnumerator(span);

        /// <summary>
        /// Returns an enumeration of <see cref="CodePoint"/> from the provided span.
        /// </summary>
        /// <param name="span">The span of char elements representing the text to enumerate.</param>
        /// <remarks>
        /// Invalid sequences will be represented in the enumeration by <see cref="CodePoint.ReplacementChar"/>.
        /// </remarks>
        /// <returns>The <see cref="SpanCodePointEnumerator"/>.</returns>
        public static SpanCodePointEnumerator EnumerateCodePoints(this Span<char> span)
            => new SpanCodePointEnumerator(span);

        /// <summary>
        /// Returns an enumeration of Grapheme instances from the provided span.
        /// </summary>
        /// <param name="span">The readonly span of char elements representing the text to enumerate.</param>
        /// <remarks>
        /// Invalid sequences will be represented in the enumeration by <see cref="GraphemeClusterClass.Any"/>.
        /// </remarks>
        /// <returns>The <see cref="SpanGraphemeEnumerator"/>.</returns>
        public static SpanGraphemeEnumerator EnumerateGraphemes(this ReadOnlySpan<char> span)
            => new SpanGraphemeEnumerator(span);

        /// <summary>
        /// Returns an enumeration of Grapheme instances from the provided span.
        /// </summary>
        /// <param name="span">The span of char elements representing the text to enumerate.</param>
        /// <remarks>
        /// Invalid sequences will be represented in the enumeration by <see cref="GraphemeClusterClass.Any"/>.
        /// </remarks>
        /// <returns>The <see cref="SpanGraphemeEnumerator"/>.</returns>
        public static SpanGraphemeEnumerator EnumerateGraphemes(this Span<char> span)
            => new SpanGraphemeEnumerator(span);
    }
}
