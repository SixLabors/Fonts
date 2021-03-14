// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Supports a simple iteration over a codepoint collection.
    /// </summary>
    internal ref struct CodePointEnumerator
    {
        private ReadOnlySpan<char> source;

        public CodePointEnumerator(ReadOnlySpan<char> source)
        {
            this.source = source;
            this.Current = CodePoint.ReplacementCodePoint;
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        public CodePoint Current { get; private set; }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the enumerator was successfully advanced to the next element;
        /// <see langword="false"/> if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            this.Current = CodePoint.DecodeFromUtf16At(this.source, 0, out int consumed);
            this.source = this.source.Slice(consumed);
            return consumed > 0;
        }
    }
}
