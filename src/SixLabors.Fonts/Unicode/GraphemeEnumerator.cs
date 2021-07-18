// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Supports a simple iteration over a grapheme collection.
    /// Implementsthe Unicode Grapheme Cluster Algorithm. UAX:29
    /// <see href="https://www.unicode.org/reports/tr29/tr29-37.html"/>
    /// Methods are pattern-matched by compiler to allow using foreach pattern.
    /// </summary>
    internal ref struct GraphemeEnumerator
    {
        private ReadOnlySpan<char> source;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphemeEnumerator"/> struct.
        /// </summary>
        /// <param name="source">The buffer to read from.</param>
        public GraphemeEnumerator(ReadOnlySpan<char> source)
        {
            this.source = source;
            this.Current = default;
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        public Grapheme Current { get; private set; }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that iterates through the collection.</returns>
        public GraphemeEnumerator GetEnumerator() => this;

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the enumerator was successfully advanced to the next element;
        /// <see langword="false"/> if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            if (this.source.IsEmpty)
            {
                return false;
            }

            // Algorithm given at https://www.unicode.org/reports/tr29/#Grapheme_Cluster_Boundary_Rules.
            var processor = new Processor(this.source);

            processor.MoveNext();

            CodePoint firstCodePoint = processor.CurrentCodePoint;

            // First, consume as many Prepend scalars as we can (rule GB9b).
            while (processor.CurrentType == GraphemeClusterClass.Prepend)
            {
                processor.MoveNext();
            }

            // Next, make sure we're not about to violate control character restrictions.
            // Essentially, if we saw Prepend data, we can't have Control | CR | LF data afterward (rule GB5).
            if (processor.CharsConsumed > 0)
            {
                if (processor.CurrentType == GraphemeClusterClass.Control
                    || processor.CurrentType == GraphemeClusterClass.CR
                    || processor.CurrentType == GraphemeClusterClass.LF)
                {
                    goto Return;
                }
            }

            // Now begin the main state machine.
            GraphemeClusterClass previousClusterBreakType = processor.CurrentType;
            processor.MoveNext();

            switch (previousClusterBreakType)
            {
                case GraphemeClusterClass.CR:
                    if (processor.CurrentType != GraphemeClusterClass.LF)
                    {
                        goto Return; // rules GB3 & GB4 (only <LF> can follow <CR>)
                    }

                    processor.MoveNext();
                    goto case GraphemeClusterClass.LF;

                case GraphemeClusterClass.Control:
                case GraphemeClusterClass.LF:
                    goto Return; // rule GB4 (no data after Control | LF)

                case GraphemeClusterClass.L:
                    if (processor.CurrentType == GraphemeClusterClass.L)
                    {
                        processor.MoveNext(); // rule GB6 (L x L)
                        goto case GraphemeClusterClass.L;
                    }
                    else if (processor.CurrentType == GraphemeClusterClass.V)
                    {
                        processor.MoveNext(); // rule GB6 (L x V)
                        goto case GraphemeClusterClass.V;
                    }
                    else if (processor.CurrentType == GraphemeClusterClass.LV)
                    {
                        processor.MoveNext(); // rule GB6 (L x LV)
                        goto case GraphemeClusterClass.LV;
                    }
                    else if (processor.CurrentType == GraphemeClusterClass.LVT)
                    {
                        processor.MoveNext(); // rule GB6 (L x LVT)
                        goto case GraphemeClusterClass.LVT;
                    }
                    else
                    {
                        break;
                    }

                case GraphemeClusterClass.LV:
                case GraphemeClusterClass.V:
                    if (processor.CurrentType == GraphemeClusterClass.V)
                    {
                        processor.MoveNext(); // rule GB7 (LV | V x V)
                        goto case GraphemeClusterClass.V;
                    }
                    else if (processor.CurrentType == GraphemeClusterClass.T)
                    {
                        processor.MoveNext(); // rule GB7 (LV | V x T)
                        goto case GraphemeClusterClass.T;
                    }
                    else
                    {
                        break;
                    }

                case GraphemeClusterClass.LVT:
                case GraphemeClusterClass.T:
                    if (processor.CurrentType == GraphemeClusterClass.T)
                    {
                        processor.MoveNext(); // rule GB8 (LVT | T x T)
                        goto case GraphemeClusterClass.T;
                    }
                    else
                    {
                        break;
                    }

                case GraphemeClusterClass.ExtPict:
                    // Attempt processing extended pictographic (rules GB11, GB9).
                    // First, drain any Extend scalars that might exist
                    while (processor.CurrentType == GraphemeClusterClass.Extend)
                    {
                        processor.MoveNext();
                    }

                    // Now see if there's a ZWJ + extended pictograph again.
                    if (processor.CurrentType != GraphemeClusterClass.ZWJ)
                    {
                        break;
                    }

                    processor.MoveNext();
                    if (processor.CurrentType != GraphemeClusterClass.ExtPict)
                    {
                        break;
                    }

                    processor.MoveNext();
                    goto case GraphemeClusterClass.ExtPict;

                case GraphemeClusterClass.RegionalIndicator:
                    // We've consumed a single RI scalar. Try to consume another (to make it a pair).
                    if (processor.CurrentType == GraphemeClusterClass.RegionalIndicator)
                    {
                        processor.MoveNext();
                    }

                    // Standlone RI scalars (or a single pair of RI scalars) can only be followed by trailers.
                    break; // nothing but trailers after the final RI

                default:
                    break;
            }

            // rules GB9, GB9a
            while (processor.CurrentType == GraphemeClusterClass.Extend
                || processor.CurrentType == GraphemeClusterClass.ZWJ
                || processor.CurrentType == GraphemeClusterClass.SpacingMark)
            {
                processor.MoveNext();
            }

            Return:

            ReadOnlySpan<char> text = this.source.Slice(0, processor.CharsConsumed);
            this.Current = new Grapheme(firstCodePoint, processor.CodePointsConsumed, text);
            this.source = this.source.Slice(processor.CharsConsumed);

            return true; // rules GB2, GB999
        }

        private ref struct Processor
        {
            private readonly ReadOnlySpan<char> source;
            private int charsConsumed;
            private int codePointsConsumed;

            public Processor(ReadOnlySpan<char> source)
            {
                this.source = source;
                this.CurrentCodePoint = CodePoint.ReplacementCodePoint;
                this.CurrentType = GraphemeClusterClass.Any;
                this.charsConsumed = 0;
                this.CharsConsumed = 0;
                this.codePointsConsumed = 0;
                this.CodePointsConsumed = 0;
            }

            public GraphemeClusterClass CurrentType { get; private set; }

            public CodePoint CurrentCodePoint { get; private set; }

            public int CodePointsConsumed { get; private set; }

            public int CharsConsumed { get; private set; }

            public void MoveNext()
            {
                this.CharsConsumed += this.charsConsumed;
                this.CodePointsConsumed += this.codePointsConsumed;
                this.CurrentCodePoint = CodePoint.DecodeFromUtf16At(this.source, this.CharsConsumed, out this.charsConsumed);
                this.CurrentType = CodePoint.GetGraphemeClusterClass(this.CurrentCodePoint);
                this.codePointsConsumed = this.charsConsumed > 0 ? 1 : 0;
            }
        }
    }
}
